using System;

public interface IAuthProvider
{
    void Initialize();
    // 성공 시 AuthCode를 반환하도록 액션 수정
    void SignIn(Action<string> onSuccess, Action<string> onFail);
}

public static class AuthProviderFactory
{
    public static IAuthProvider CreateProvider()
    {
#if UNITY_ANDROID
        return new GoogleAuthProvider();
#elif UNITY_IOS
        return new AppleAuthProvider();
#else
        // 에디터나 PC 환경에서는 테스트용 더미 또는 Null 반환
        return null; 
#endif
    }
}

public enum AuthStatus
{
    None,           // 초기 상태
    Processing,     // 로그인 또는 네트워크 연결 시도 중
    SuccessOnline,  // GPGS/Apple 로그인 성공 (온라인)
    SuccessOffline, // 로그인 실패로 인한 게스트 모드 (오프라인)
    Failed          // (사용 안 함) 게스트 모드 전환으로 인해 완전한 실패는 없음
}
