using UnityEngine;

// [제작 의도] 객체 생성 없이 어디서든 접근 가능한 정적(Static) 유틸리티 클래스
public static class NetworkMonitor
{
    // [기능 설명] 현재 네트워크 연결 여부 (True/False)
    public static bool IsConnected
    {
        get
        {
            return Application.internetReachability != NetworkReachability.NotReachable;
        }
    }

    // [기능 설명] 네트워크 타입 반환 (WiFi / Data / None)
    public static NetworkType CurrentNetworkType
    {
        get
        {
            switch (Application.internetReachability)
            {
                case NetworkReachability.ReachableViaLocalAreaNetwork:
                    return NetworkType.WiFi;
                case NetworkReachability.ReachableViaCarrierDataNetwork:
                    return NetworkType.Data;
                default:
                    return NetworkType.None;
            }
        }
    }
}

public enum NetworkType
{
    None,
    WiFi,
    Data
}
