using UnityEngine;

/// <summary>
/// 모바일 기기 진동 제어 유틸리티
/// - Android: 밀리초(ms) 단위 제어 가능
/// - iOS: 기본 Handheld.Vibrate 사용 (OS 제약)
/// - Editor: 로그 출력
/// </summary>
public static class Vibrator
{
#if UNITY_ANDROID && !UNITY_EDITOR
    private static AndroidJavaClass unityPlayer;
    private static AndroidJavaObject currentActivity;
    private static AndroidJavaObject vibrator;
#endif

    /// <summary>
    /// 지정된 시간(ms)만큼 진동을 울립니다. (Android 전용)
    /// </summary>
    /// <param name="milliseconds">진동 지속 시간 (기본값: 250ms)</param>
    public static void Vibrate(long milliseconds = 250)
    {
        if (!GameSetting.Vibration)
        {
            return;
        }

        // 에디터에서는 진동 대신 로그로 확인
        if (Application.isEditor)
        {
            Debug.Log($"[Vibration] 징~ ({milliseconds}ms)");
            return;
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        // 안드로이드 진동 서비스 호출 초기화 (최초 1회만 수행)
        if (vibrator == null)
        {
            unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            vibrator = currentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator");
        }

        // 진동 기능이 있는 경우 실행
        if (vibrator != null)
        {
            // deprecated된 vibrate(long) 함수지만 하위 호환성을 위해 사용
            vibrator.Call("vibrate", milliseconds);
        }
#elif UNITY_IOS
        // iOS는 시간 제어 API를 네이티브 플러그인 없이 사용 불가하므로 내장 함수 사용
        Handheld.Vibrate();
#endif
    }

    /// <summary>
    /// 진동 기능을 취소합니다.
    /// </summary>
    public static void Cancel()
    {
        if (Application.isEditor) return;

#if UNITY_ANDROID && !UNITY_EDITOR
        if (vibrator != null) vibrator.Call("cancel");
#endif
    }
}
