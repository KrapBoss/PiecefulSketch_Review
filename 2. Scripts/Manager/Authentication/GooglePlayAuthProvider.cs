using GooglePlayGames;
using GooglePlayGames.BasicApi;
using UnityEngine;
using System;

public class GoogleAuthProvider : IAuthProvider
{
    public void Initialize()
    {
        // Android GPGS 초기화
        PlayGamesPlatform.Activate();
    }

    public void SignIn(Action<string> onSuccess, Action<string> onFail)
    {
        Debug.Log("[Authentication] : Google SignIn");
        // 이미 로그인 되어 있는지 확인
        if (PlayGamesPlatform.Instance.IsAuthenticated())
        {
            RequestAuthCode(onSuccess, onFail);
            return;
        }

        // 1. GPGS 로그인 시도
        PlayGamesPlatform.Instance.Authenticate((SignInStatus status) =>
        {
            if (status == SignInStatus.Success)
            {
                RequestAuthCode(onSuccess, onFail);
            }
            else
            {
                onFail?.Invoke($"Google SignIn Failed: {status}");
            }
        });
    }

    private void RequestAuthCode(Action<string> onSuccess, Action<string> onFail)
    {
        Debug.Log("[Authentication] : Google Request Code");

        // 2. UGS 연동을 위한 Server Auth Code 요청 (GPGS v11+ 표준)
        PlayGamesPlatform.Instance.RequestServerSideAccess(false, (authCode) =>
        {
            if (!string.IsNullOrEmpty(authCode))
            {
                onSuccess?.Invoke(authCode);
            }
            else
            {
                onFail?.Invoke("Google AuthCode is Null. Check Web Client ID.");
            }
        });
    }
}
