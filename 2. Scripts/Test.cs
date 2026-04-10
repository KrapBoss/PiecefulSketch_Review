using Managers;
using System;
using TMPro; // (이 using문은 더 이상 필요 없지만, 호환성을 위해 남겨둡니다.)
using UI.PopUp;
using UnityEngine;

public class Test : MonoBehaviour
{
    [Header("Reward Animation Test")]
    public RectTransform targetIconRect; // 아이콘이 날아갈 위치
    public Sprite itemIcon;
    public int amountToGain = 100;

    /// <summary>
    /// Method to test the reward animation.
    /// Press 'T' to trigger.
    /// </summary>
    public void PlayRewardAnimationTest()
    {
        if (targetIconRect != null && itemIcon != null && RewardAnimationManager.Instance != null)
        {
        }
        else
        {
            Debug.LogWarning("Reward Animation Test: Some fields are not assigned in the inspector.");
        }
    }

    /// <summary>
    /// Method to test early completion of the animation.
    /// Press 'Y' to trigger.
    /// </summary>
    public void CompleteRewardAnimationTest()
    {
        if (RewardAnimationManager.Instance != null)
        {
            RewardAnimationManager.Instance.CompleteAll();
        }
    }

    private void Update()
    {
        // Test key for playing the reward animation
        if (Input.GetKeyDown(KeyCode.T))
        {
            PlayerData.TryAddCoin(amountToGain);
        }
        
        // Test key for completing the reward animation early
        if (Input.GetKeyDown(KeyCode.Y))
        {
            Debug.Log(DateTime.UtcNow.Ticks.ToString());
        }

        if (Input.GetKeyDown(KeyCode.Q))
        {
            //AdsManager.Instance.ToggleBanner(!AdsManager.Instance.isBannerEnabled);

            FindAnyObjectByType<GeneratorSingle>().StateToEnd(1);
        }
        if (Input.GetKeyDown(KeyCode.D))
        {
            //PlayerData.TryAddCoin(100);
            //Notice.Message("Test");
            //FindAnyObjectByType<GeneratorSingle>().StateToEnd(1);

            SaveDataManager.ClearAllLocalData();
            GameSetting.Clear();
        }
        if (Input.GetKeyDown(KeyCode.F))
        {
            //PlayerData.TryAddCoin(100);
            //Notice.Message("Test");
            //FindAnyObjectByType<GeneratorSingle>().StateToEnd(1);
            CaptureScreen();
        }
    }
    
    // Screen Capture (1=original resolution, 2=2x, 4=4x)
    // At 4x, it's possible to save a 4K image on a FHD monitor
    [Range(1, 5)]
    public int superSize = 2;

    public void CaptureScreen()
    {
        // Set filename (using timestamp to prevent duplicates)
        string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string filename = $"Screenshot_{timestamp}.png";

        // 1. Capture
        // The superSize multiplier upscales the resolution.
        ScreenCapture.CaptureScreenshot(filename, superSize);

        Debug.Log($"[Screenshot] Saved: {filename} (Scale: {superSize}x)");
    }
    
    private void Awake()
    {
#if !UNITY_EDITOR
        // Destroy this component in the actual build to prevent test code from running.
        Destroy(this);
#endif
    }
    private void OnGUI()
    {
        // 제작 의도: 요청하신 해상도 비율(가로 25%, 세로 150px) 반영
        // Unity 효율성: GUI.Button의 반환값을 조건문으로 사용하여 클릭 이벤트를 즉시 처리
        //float buttonWidth = Screen.width * 0.25f;
        //float buttonHeight = 150f;

        //// 좌상단 (0, 0) 위치에 버튼 생성
        //if (GUI.Button(new Rect(0, 0, buttonWidth, buttonHeight), "TEST1"))
        //{
        //    AuthenticationManager.Clear();
        //    SaveDataManager.ClearAllLocalData();
        //}
    }
}