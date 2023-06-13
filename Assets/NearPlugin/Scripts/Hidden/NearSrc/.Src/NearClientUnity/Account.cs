using NearClientUnity.Providers;
using NearClientUnity.Utilities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace NearClientUnity
{
    public class Account
    {
        // Default amount of tokens to be send with the function calls. Used to pay for the fees
        // incurred while running the contract execution. The unused amount will be refunded back to
        // the originator.
        public const int DefaultFuncCallAmount = 2000000;

        private const int TxStatusRetryNumber = 10;
        private const int TxStatusRetryWait = 500;
        private readonly string _accountId;
        private readonly Connection _connection;
        private AccessKey _accessKey;
        private bool _ready;
        private AccountState _state;

        public Account(Connection connection, string accountId)
        {
            _connection = connection;
            _accountId = accountId;
        }

        public string AccountId => _accountId;
        public Connection Connection => _connection;

        public async Task<FinalExecutionOutcome> AddKeyAsync(string publicKey, UInt128? amount,
            string methodName = "", string contractId = "")
        {
            AccessKey accessKey;
            if (string.IsNullOrEmpty(contractId) || string.IsNullOrWhiteSpace(contractId))
            {
                accessKey = AccessKey.FullAccessKey();
            }
            else
            {
                accessKey = AccessKey.FunctionCallAccessKey(contractId, (string.IsNullOrWhiteSpace(methodName) || string.IsNullOrEmpty(methodName)) ? Array.Empty<string>() : new[] { methodName }, amount);
            }
            var result = await SignAndSendTransactionAsync(_accountId, new[] { Action.AddKey(new PublicKey(publicKey), accessKey) });
            return (FinalExecutionOutcome)result;
        }

        public async Task<FinalExecutionOutcome> AddKeyAsync(string publicKey,
            string methodName = "", string contractId = "")
        {
            AccessKey accessKey;
            if (string.IsNullOrEmpty(contractId) || string.IsNullOrWhiteSpace(contractId))
            {
                accessKey = AccessKey.FullAccessKey();
            }
            else
            {
                accessKey = AccessKey.FunctionCallAccessKey(contractId, (string.IsNullOrWhiteSpace(methodName) || string.IsNullOrEmpty(methodName)) ? Array.Empty<string>() : new[] { methodName });
            }
            var result = await SignAndSendTransactionAsync(_accountId, new[] { Action.AddKey(new PublicKey(publicKey), accessKey) });
            return (FinalExecutionOutcome)result;
        }

        public async Task<FinalExecutionOutcome> CreateAccountAsync(string newAccountId, string publicKey,
            UInt128 amount)
        {
            var accessKey = AccessKey.FullAccessKey();
            var actions = new[]
                {Action.CreateAccount(), Action.Transfer(amount), Action.AddKey(new PublicKey(publicKey), accessKey)};
            var result = await SignAndSendTransactionAsync(newAccountId, actions);
            return (FinalExecutionOutcome)result;
        }

        public async Task<FinalExecutionOutcome> CreateAccountAsync(string newAccountId, PublicKey publicKey,
            UInt128 amount)
        {
            var accessKey = AccessKey.FullAccessKey();
            var actions = new[]
                {Action.CreateAccount(), Action.Transfer(amount), Action.AddKey(publicKey, accessKey)};
            var result = await SignAndSendTransactionAsync(newAccountId, actions);
            return (FinalExecutionOutcome)result;
        }

        public async Task<Account> CreateAndDeployContractAsync(string contractId, string publicKey, byte[] data,
            UInt128 amount)
        {
            var accessKey = AccessKey.FullAccessKey();
            var actions = new[]
            {
                Action.CreateAccount(), Action.Transfer(amount), Action.AddKey(new PublicKey(publicKey), accessKey),
                Action.DeployContract(data)
            };

            await SignAndSendTransactionAsync(contractId, actions);

            var contractAccount = new Account(_connection, contractId);
            return contractAccount;
        }

        public async Task<Account> CreateAndDeployContractAsync(string contractId, PublicKey publicKey, byte[] data,
            UInt128 amount)
        {
            var accessKey = AccessKey.FullAccessKey();
            var actions = new[]
            {
                Action.CreateAccount(), Action.Transfer(amount), Action.AddKey(publicKey, accessKey),
                Action.DeployContract(data)
            };

            await SignAndSendTransactionAsync(contractId, actions);

            var contractAccount = new Account(_connection, contractId);
            return contractAccount;
        }

        public async Task<FinalExecutionOutcome> DeleteAccountAsync(string beneficiaryId)
        {
            var result =
                await SignAndSendTransactionAsync(_accountId, new[] { Action.DeleteAccount(beneficiaryId) });
            return (FinalExecutionOutcome)result;
        }

        public async Task<FinalExecutionOutcome> DeleteKeyAsync(string publicKey)
        {
            var result = await SignAndSendTransactionAsync(_accountId, new[] { Action.DeleteKey(new PublicKey(publicKey)) });
            return (FinalExecutionOutcome)result;
        }

        public async Task<FinalExecutionOutcome> DeleteKeyAsync(PublicKey publicKey)
        {
            var result = await SignAndSendTransactionAsync(_accountId, new[] { Action.DeleteKey(publicKey) });
            return (FinalExecutionOutcome)result;
        }

        public async Task<FinalExecutionOutcome> DeployContractAsync(byte[] data)
        {
            var result = await SignAndSendTransactionAsync(_accountId, new[] { Action.DeployContract(data) });
            return (FinalExecutionOutcome)result;
        }

        public async Task FetchStateAsync()
        {
            _accessKey = null;

                try
                {
                    var rawState = await _connection.Provider.QueryAsync<AccountState>($"account/{_accountId}", "");
                    if (rawState == null)
                    {                    
                        return;
                    }
                    _state = new AccountState()
                    {
                        AccountId = rawState.AccountId ?? null,
                        Staked = rawState.Staked ?? null,
                        Locked = rawState.Locked,
                        Amount = rawState.Amount,
                        CodeHash = rawState.CodeHash,
                        StoragePaidAt = rawState.StoragePaidAt,
                        StorageUsage = rawState.StorageUsage
                    };
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                    throw new Exception($"Failed to fetch state for '{_accountId}'");
                }

            var publicKey = await _connection.Signer.GetPublicKeyAsync(_accountId, _connection.NetworkId);
            if (publicKey == null) return;

            try
            {
                var rawAccessKey =
                    await _connection.Provider.QueryAsync<object>($"access_key/{_accountId}/{publicKey.ToString()}", "");
                _accessKey = AccessKey.FromDynamicJsonObject((JObject)rawAccessKey);
            }
            catch (Exception)
            {
                throw new Exception(
                    $"Failed to fetch access key for '{_accountId}' with public key {publicKey.ToString()}");
            }
        }

        public async Task<object> FunctionCallAsync(string contractId, string methodName, object args, ulong? gas = null, Nullable<UInt128> amount = null)
        {
            if (args == null)
            {
                args = new Dictionary<string, object>();
            }

            var methodArgs = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(args));
            var gasValue = gas ?? DefaultFuncCallAmount;
            var amountValue = amount ?? DefaultFuncCallAmount;
            var result = await SignAndSendTransactionAsync(contractId, new Action[] { Action.FunctionCall(methodName, methodArgs, gasValue, amountValue) });
            return result;
        }


        /// Returns array of {access_key: AccessKey, public_key: PublicKey} items.
        public async Task<object> GetAccessKeysAsync()
        {
            var response = await _connection.Provider.QueryAsync<object>($"access_key/{_accountId}", "");
            return response;
        }

        public async Task<object> GetAccountDetailsAsync()
        {
            // TODO: update the response value to return all the different keys, not just app keys.
            // Also if we need this function, or getAccessKeys is good enough.
            var accessKeys = await GetAccessKeysAsync();
            var result = new Dictionary<string, object>();
            var authorizedApps = new List<Dictionary<string, object>>();

            foreach (var key in (IEnumerable<object>)accessKeys)
            {
                var rawPermission = ((JObject)key)["access_key"]["permission"];
                var isFullAccess = rawPermission.Type == JTokenType.String && rawPermission.Value<string>() == "FullAccess";
                if (isFullAccess) continue;
                var perm = ((JObject)key)["access_key"]["permission"]["FunctionCall"];
                var authorizedApp = new Dictionary<string, object>();
                authorizedApp["ContractId"] = perm["receiver_id"].Value<string>();
                authorizedApp["Amount"] = perm["allowance"].Value<string>();
                authorizedApp["PublicKey"] = ((JObject)key)["public_key"].Value<string>();
                authorizedApps.Add(authorizedApp);
            }

            result["AuthorizedApps"] = authorizedApps.ToArray();
            result["Transactions"] = Array.Empty<object>();
            return result;
        }




        public async Task<AccountState> GetStateAsync()
        {
            await GetReadyStatusAsync();
            return _state;
        }

        public async Task<FinalExecutionOutcome> SendMoneyAsync(string receiverId, UInt128 amount)
        {
            var result = await SignAndSendTransactionAsync(receiverId, new[] { Action.Transfer(amount) });
            return (FinalExecutionOutcome)result;
        }

        public async Task<FinalExecutionOutcome> StakeAsync(string publicKey, UInt128 amount)
        {
            var result = await SignAndSendTransactionAsync(_accountId, new[] { Action.Stake(amount, new PublicKey(publicKey)) });
            return (FinalExecutionOutcome)result;
        }

        public async Task<FinalExecutionOutcome> StakeAsync(PublicKey publicKey, UInt128 amount)
        {
            var result = await SignAndSendTransactionAsync(_accountId, new[] { Action.Stake(amount, publicKey) });
            return (FinalExecutionOutcome)result;
        }

        public async Task<object> ViewFunctionAsync(string contractId, string methodName, object args)
        {
            if (args == null)
            {
                args = new Dictionary<string, object>();
            }

            var methodArgs = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(args));
            var response = await _connection.Provider.QueryAsync<object>($"call/{contractId}/{methodName}", Base58.Encode(methodArgs));

            var result = (JObject)response;

            if (result["logs"] != null && result["logs"].GetType() is ArraySegment<string> && result["logs"].Count() > 0)
            {
                var logs = result["logs"].ToObject<string[]>();
                PrintLogs(contractId, logs);
            }

            return result;
        }


        
        protected async Task<bool> GetReadyStatusAsync()
        {
            if (_ready) return _ready;
            try
            {
                await FetchStateAsync();
                _ready = true;
            }
            catch (Exception e)
            {
                _ready = false;
                throw e;
            }

            return _ready;
        }

        private void PrintLogs(string contractId, string[] logs)
        {
            foreach (var log in logs)
            {
                Console.WriteLine($"[{contractId}]: {log}");
            }
        }

        private async Task<FinalExecutionOutcome> RetryTxResultAsync(byte[] txHash, string accountId)
        {
            var waitTime = TxStatusRetryWait;
            for (var i = 0; i < TxStatusRetryNumber; i++)
            {
                try
                {
                    var result = await _connection.Provider.GetTxStatusAsync(txHash, accountId);
                    return result;
                }
                catch
                {
                    await Task.Delay(waitTime);
                    waitTime *= TxStatusRetryWait;
                    i++;
                }
            }

            throw new Exception(
                $"Exceeded {TxStatusRetryNumber} status check attempts for transaction ${Base58.Encode(txHash)}");
        }

        private async Task<object> SignAndSendTransactionAsync(string receiverId, Action[] actions)
        {
            if (!await GetReadyStatusAsync())
            {
                throw new Exception($"Cannot sign transactions, no matching key pair found in Signer.");
            }

            var status = await _connection.Provider.GetStatusAsync();
            var signTransaction = await SignedTransaction.SignTransactionAsync(receiverId, (ulong)++_accessKey.Nonce, actions,
                new ByteArray32() { Buffer = Base58.Decode(status.SyncInfo.LatestBlockHash) }, _connection.Signer, _accountId, _connection.NetworkId);

            object result;

            try
            {
                result = await _connection.Provider.SendTransactionAsync<object>(signTransaction.Item2);
            }
            catch (Exception e)
            {
                var parts = e.Message.Split(':');
                if (parts.Length > 1 && parts[1] == " Request timed out.")
                {
                    result = await RetryTxResultAsync(signTransaction.Item1, _accountId);
                }
                else
                {
                    throw e;
                }
            }

            return result;
        }

    }
}