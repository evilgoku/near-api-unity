using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SignInButtonHandler : MonoBehaviour
{
    public async void RequestSignIn()
    {
        await NearPersistentManager.Instance.WalletAccount.RequestSignIn(
            "dev-1678015185015-40254331682784",
            "Near Unity Client",
            new Uri("nearclientunity://testnet.mynearwallet.com/success"),
            new Uri("nearclientunity://testnet.mynearwallet.com/fail"),
            new Uri("nearclientios://testnet.mynearwallet.com")
            );
    }
}
