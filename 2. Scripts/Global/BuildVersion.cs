#define LOCAL_DEV

using UnityEngine;

/// <summary>
/// 빌드 버전 및 환경 설정을 관리하는 클래스
/// </summary>
public static class BuildVersion
{
#if LOCAL_DEV
    public static bool IsLocal = true;
#else
    public static bool IsLocal = false;
#endif

    /// <summary>
    /// 현재 환경에 맞는 Addressable 서버 주소를 반환합니다.
    /// </summary>
    /// <returns>Addressable URL</returns>
    public static string GetAddressableURL()
    {
        if (IsLocal)
        {
            // 로컬 테스트 서버 주소
            return "";
        }
        else
        {
            // 라이브 운영 서버 주소 (실제 URL로 대체 필요)
            return "";
        }
    }
}
