using Managers;
using UnityEngine;

namespace UI.Utill
{
    /// <summary>
    /// 이 컴포넌트가 부착된 RectTransform을 기기의 안전 영역(Safe Area)에 맞게 조절합니다.
    /// 노치 디자인이나 하단 제스처 영역이 있는 기기에서 UI가 잘리는 것을 방지합니다.
    /// 추가로 AdsManager의 배너 광고 활성화 여부에 따라 하단 패딩을 추가로 적용합니다.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class SafeArea : MonoBehaviour
    {
        private RectTransform _panel;
        private Rect _lastSafeArea = new Rect(0, 0, 0, 0);
        
        // 배너 높이 (DPI에 따라 계산하거나 고정값 사용 가능)
        // AdMob 표준 배너 높이는 약 50dp입니다.
        // 여기서는 안전하게 픽셀 단위로 변환하거나 비율로 계산해야 하지만,
        // 편의상 화면 높이의 일정 비율 혹은 고정 픽셀을 적용할 수 있습니다.
        // 더 정확한 계산을 위해선 GoogleMobileAds의 AdSize.Banner.HeightInPixels 등을 참조해야 하지만,
        // 여기서는 직접 계산하거나 AdsManager에서 받아오는 구조가 좋습니다.
        // 일단 표준 높이(대략 150~200px 또는 화면 비율의 8~10%)를 가정합니다.
        
        private bool _lastBannerStatus = false;
        private float _lastScreenHeight = 0;

        private void Awake()
        {
            _panel = GetComponent<RectTransform>();
            ApplySafeArea();
        }

        private void Update()
        {
            // 1. 화면 해상도나 안전 영역 변경 감지
            bool safeAreaChanged = (Screen.safeArea != _lastSafeArea);
            bool screenHeightChanged = (Screen.height != _lastScreenHeight);
            
            // 2. 배너 상태 변경 감지
            //bool isBannerActive = AdsManager.Instance != null && AdsManager.Instance.isBannerEnabled;
            //bool bannerStatusChanged = (isBannerActive != _lastBannerStatus);

            // 변경사항이 있을 때만 갱신
            if (safeAreaChanged || screenHeightChanged)
            {
                ApplySafeArea();
            }
        }

        /// <summary>
        /// Screen.safeArea 값을 RectTransform의 앵커에 적용하고 배너 영역만큼 추가 패딩을 줍니다.
        /// </summary>
        private void ApplySafeArea(bool isBannerActive = false)
        {
            Rect safeArea = Screen.safeArea;
            
            // 상태 갱신
            _lastSafeArea = safeArea;
            _lastBannerStatus = isBannerActive;
            _lastScreenHeight = Screen.height;

            // 픽셀 단위의 안전 영역을 정규화된 앵커 값(0-1)으로 변환합니다.
            Vector2 anchorMin = safeArea.position;
            Vector2 anchorMax = safeArea.position + safeArea.size;
            
            // [배너 영역 추가 로직]
            // 배너가 켜져 있다면, 안전 영역의 바닥(yMin)을 배너 높이만큼 위로 올립니다.
            if (isBannerActive)
            {
                // 표준 배너 높이는 50dp.
                // Unity에서 dp -> pixel 변환: 50 * (Screen.dpi / 160f)
                // 만약 DPI를 못 가져오면 기본값 사용
                float dpi = Screen.dpi;
                if (dpi <= 0) dpi = 160f; // 기본값
                
                float bannerHeightInPixels = 50f * (dpi / 160f);

                // 배너가 화면을 너무 많이 가리지 않도록 최대치 제한 (예: 화면의 15%)
                if (bannerHeightInPixels > Screen.height * 0.15f)
                {
                    bannerHeightInPixels = Screen.height * 0.15f;
                }

                // safeArea.y (하단 시작점)이 이미 제스처바 등으로 올라와 있을 수 있습니다.
                // 배너는 화면 최하단(0)부터 시작하므로, 
                // anchorMin.y가 배너 높이보다 작다면 배너 높이만큼 맞춰줘야 가려지지 않습니다.
                // 즉, (안전 영역 하단)과 (배너 상단) 중 더 높은 곳을 컨텐츠 시작점으로 잡습니다.
                
                if (anchorMin.y < bannerHeightInPixels)
                {
                    anchorMin.y = bannerHeightInPixels;
                }
            }

            // 정규화 (0.0 ~ 1.0)
            anchorMin.x /= Screen.width;
            anchorMin.y /= Screen.height;
            anchorMax.x /= Screen.width;
            anchorMax.y /= Screen.height;

            // RectTransform의 앵커에 적용
            _panel.anchorMin = anchorMin;
            _panel.anchorMax = anchorMax;
        }
    }
}