using NearClientUnity.KeyStores;
using NearClientUnity.Utilities;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net.Http;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;

namespace NearClientUnity
{
    public class WalletAccount
    {
        private const string LocalStorageKeySuffix = "_wallet_auth_key";
        private const string LoginWalletUrlSuffix = "/login/";
        private const string PendingAccessKeyPrefix = "pending_key";

        private AuthData _authData = new AuthData();
        private string _authDataKey;
        private IExternalAuthService _authService;
        private IExternalAuthStorage _authStorage;
        private KeyStore _keyStore;        
        private string _networkId;
        private string _walletBaseUrl;

        private bool _keySet;

        public WalletAccount(Near near, string appKeyPrefix, IExternalAuthService authService, IExternalAuthStorage authStorage)
        {
            _networkId = near.Config.NetworkId;
            _walletBaseUrl = near.Config.WalletUrl;
            appKeyPrefix = string.IsNullOrEmpty(appKeyPrefix) || string.IsNullOrWhiteSpace(appKeyPrefix)
                ? "default"
                : appKeyPrefix;
            _authDataKey = $"{appKeyPrefix}{LocalStorageKeySuffix}";
            _keyStore = (near.Connection.Signer as InMemorySigner).KeyStore;
            _authService = authService;
            _authStorage = authStorage;

           
            if (_authStorage.HasKey(_authDataKey))
            {
                _authData.AccountId = _authStorage.GetValue(_authDataKey);
            }
            else
            {
                _authData.AccountId = null;
            }
        }

        public IExternalAuthStorage NearAuthStorage => _authStorage;

        public async Task CompleteSignIn(string url)
        {
            Uri uri = new Uri(url);

            if (uri.AbsoluteUri.Contains("/fail"))
            {
                return;
            }

            string publicKey = null;
            string accountId = null;
            if (!string.IsNullOrWhiteSpace(uri.Query))
            {
                var queryParams = uri.Query.TrimStart('?').Split('&');
                foreach (var queryParam in queryParams)
                {
                    var parts = queryParam.Split('=');
                    if (parts.Length >= 2)
                    {
                        var key = Uri.UnescapeDataString(parts[0]);
                        var value = Uri.UnescapeDataString(parts[1]);

                        if (key == "public_key")
                        {
                            publicKey = value;
                        }
                        else if (key == "account_id")
                        {
                            accountId = value;
                        }
                    }
                }
            }

            if (publicKey == null || accountId == null)
                return;

            await SetData(accountId, publicKey);
        }
        
        private async Task SetData(string accountId, string publicKey)
        {
            _authData.AccountId = accountId;

            try
            {
                if (_authStorage.HasKey(accountId) == false && _authStorage.HasKey("tempNearAccessKey"))
                    _authStorage.Add(accountId, _authStorage.GetValue("tempNearAccessKey"));
                _authStorage.Add(_authDataKey, accountId);         
                await MoveKeyFromTempToPermanent(accountId, publicKey);
                _keySet = true;
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        public string GetAccountId()
        {
            return _authData.AccountId ?? "";
        }

        public bool IsSignedIn()
        {
            if (_authData.AccountId == null) return false;
            return true;
        }
        
        public bool IsKeySet()
        {
            return _keySet;
        }

        public async Task<bool>  RequestSignIn(string contractId, string title, Uri successUrl, Uri failureUrl, Uri appUrl)
        {
            // if (!string.IsNullOrWhiteSpace(GetAccountId())) return true;
            // if (await _keyStore.GetKeyAsync(_networkId, GetAccountId()) != null) return true;
            
            var accessKey = _authStorage.HasKey(GetAccountId()) ? KeyPair.FromString(Helper.Decrypt(_authStorage.GetValue(GetAccountId()))) : 
                            _authStorage.HasKey("tempNearAccessKey") ? KeyPair.FromString(Helper.Decrypt(_authStorage.GetValue("tempNearAccessKey"))) : KeyPair.FromRandom("ed25519");

            var url = new UriBuilder(_walletBaseUrl + LoginWalletUrlSuffix);

            url.Query = new FormUrlEncodedContent(new Dictionary<string, string>()
            {
                { "title", title },
                //{ "contract_id", contractId },
                { "success_url", successUrl.AbsoluteUri },
                { "failure_url", failureUrl.AbsoluteUri },
                { "app_url", appUrl.AbsoluteUri},
                { "public_key", accessKey.GetPublicKey().ToString() },
            }).ReadAsStringAsync().Result;

            await _keyStore.SetKeyAsync(_networkId, PendingAccessKeyPrefix + accessKey.GetPublicKey(), accessKey);
            if (IsSignedIn() && _authStorage.HasKey(GetAccountId()))
            {
                await SetData(GetAccountId(), accessKey.GetPublicKey().ToString());
                return true;
            }
            else
            {
                _authStorage.Add("tempNearAccessKey", Helper.Encrypt(accessKey.ToString()));
                return _authService.OpenUrl(url.Uri.AbsoluteUri);
            }
        }

        public void SignOut()
        {
            _authData = new AuthData();
            _authStorage.DeleteKey("tempNearAccessKey");
            _authStorage.DeleteKey(GetAccountId());
            _authStorage.DeleteKey(_authDataKey);
            _authData.AccountId = null;
        }

        private async Task MoveKeyFromTempToPermanent(string accountId, string publicKey)
        {
            var pendingAccountId = PendingAccessKeyPrefix + publicKey;
            KeyPair keyPair;
            try
            {
                keyPair = await _keyStore.GetKeyAsync(_networkId, pendingAccountId);
            }
            catch (Exception)
            {
                throw new Exception("Wallet account error: no KeyPair");
            }

            try
            {
                await _keyStore.SetKeyAsync(_networkId, accountId, keyPair);
            }
            catch (Exception e)
            {
                throw e;
            }

            try
            {
                await _keyStore.RemoveKeyAsync(_networkId, PendingAccessKeyPrefix + publicKey);
            }
            catch (Exception e)
            {
                throw e;
            }
        }
        
        private class AuthData
        {
            public string AccountId { get; set; }
        }
    }
}