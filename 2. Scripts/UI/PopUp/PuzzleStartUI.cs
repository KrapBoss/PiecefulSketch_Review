using Custom;
using TMPro;
using UI.Core;
using UI.PopUp;
using Unity.Services.Authentication;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 퍼즐 시작 전 정보를 표시하는 UI입니다.
/// </summary>
public class PuzzleStartUI : UIBase
{
    [Header("퍼즐 샘플 이미지")]
    [SerializeField] private Image puzzleImage;

    [Header("퍼즐 텍스트")]
    [SerializeField] private TextMeshProUGUI puzzleNameText;
    [SerializeField] private TextMeshProUGUI puzzlePieceText;
    [SerializeField] private TextMeshProUGUI puzzleTimerText;
    [SerializeField] private GameObject puzzleTimerTextObject;

    [Header("퍼즐 보상 텍스트")]
    [SerializeField] private TextMeshProUGUI rewardText;
    [Header("스테미나 텍스트")]
    [SerializeField] private TextMeshProUGUI staminaText;

    [Header("타임 어택 보상 단계 UI")]
    [SerializeField] private RewardStepUI repeatReward; 
    [SerializeField] private RewardStepUI[] rewardSteps; // 인스펙터에서 3개 연결 (3, 2, 1단계 순서)

    [SerializeField] private RectTransform rect_base;
    [SerializeField] private RectTransform rect_target;

    string smapleImage = string.Empty;

    PuzzleSO m_data;

    /// <summary>
    /// UI를 초기화하고 퍼즐 정보를 설정합니다.
    /// </summary>
    /// <param name="puzzleData">표시할 퍼즐의 ScriptableObject 데이터</param>
    public async void Init(PuzzleSO puzzleData)
    {
        if (puzzleData == null)
        {
            Debug.LogError("PuzzleData is null.");
            CloseUI();
            return;
        }

        m_data = puzzleData;

        var progress = SaveDataManager.GetPuzzleProgress(m_data.Identity.Category, m_data.Identity.ID);
     

        // 퍼즐 이름 설정
        puzzleNameText.text = Localization.Localize(puzzleData.PuzzleName);


        if (puzzlePieceText) puzzlePieceText.text = $"{m_data.PieceCount}";

        if (staminaText) staminaText.text = $"x{m_data.Stamina}";

        if (puzzleTimerText)
        {   // 퍼즐 클리어 시간
            if(progress.BestClearTime <= 0)
            {
                if (puzzleTimerTextObject) puzzleTimerTextObject.SetActive(false);
                //puzzleTimerText.text = Localization.Localize("");
            }
            else
            {
                if (puzzleTimerTextObject) puzzleTimerTextObject.SetActive(true);
                puzzleTimerText.text = CustomCalculator.FormatTime(progress.BestClearTime);
            }
        }

        // 보상 정보 설정
        rewardText.text = $"{CustomCalculator.TFCoinString(m_data.RewardCoin)}";
        
        // 보상 단계 UI 설정
        if (rewardSteps != null && rewardSteps.Length == 4)
        {
            // 1. 초기 정보 설정 (Set)
            rewardSteps[0].Set(m_data.RewardCoin, "Default Reward");
            for (int i = 0; i < 3; i++)
            {
                int rank = i + 1;
                int rewardAmount = PuzzleRewardCalculator.GetRewardAmount(rank, m_data.RewardCoin);
                float targetTimeValue = PuzzleRewardCalculator.GetTargetTime(rank, m_data.PieceCount);
                string formattedTime = Custom.CustomCalculator.FormatTime(targetTimeValue);
                rewardSteps[i + 1].Set(rewardAmount, formattedTime);
            }

            // 2. 상태 변경 (TurnOn / TurnOff)
            // 기본 보상 상태
            if (progress.IsCleared)
                rewardSteps[0].TurnOff(); // 이미 획득
            else
                rewardSteps[0].TurnOn();  // 획득 가능

            // 타임어택 보상 상태
            for (int i = 0; i < 3; i++)
            {
                int rank = i + 1;
                int uiIndex = i + 1;
                bool isClaimed = progress.TimeAttackRewardsClaimed[rank - 1] && progress.IsCleared;

                if (isClaimed)
                {
                    rewardSteps[uiIndex].TurnOff(); // 이미 획득
                }
                else
                {
                    // 아직 획득 안한 보상 중, 받을 수 있는 것만 On
                    // (플레이 전이거나, 최고 기록이 목표 시간 이내인 경우)
                    //if (progress.IsCleared && progress.BestClearTime <= PuzzleRewardCalculator.GetTargetTime(rank, m_data.PieceCount))
                    //    rewardSteps[uiIndex].TurnOff();
                    //else
                        rewardSteps[uiIndex].TurnOn();
                }
            }
        }
        else 
        {
            Debug.LogError("RewardStepUI 배열이 4개로 설정되지 않았습니다.");
        }

        // 반복 보상 설정
        if (progress.IsCleared)
        {
            repeatReward.gameObject.SetActive(true);
            int repeatRewardAmount = PuzzleRewardCalculator.CalculateRepeatReward(progress, m_data);
            repeatReward.Set(repeatRewardAmount, Localization.Localize("Repeat Reward")); 
            repeatReward.TurnOn();
        }
        else
        {
            repeatReward.gameObject.SetActive(false);
        }

        // 퍼즐 샘플 이미지 로드 및 설정
        // PuzzleSO의 SampleImageName은 리소스 폴더 내의 파일 이름을 나타냅니다.
        // 예: "PuzzleSample_1-0"
        puzzleImage.color = new Color(1, 1, 1, 0);

        if (progress.IsCleared) smapleImage = puzzleData.SampleImageName;
        else smapleImage = puzzleData.SampleImageNameBack;

        var imageSprite = await ResourceManager.Instance.LoadAsset<Sprite>(smapleImage);
        puzzleImage.color = new Color(1, 1, 1, 1);
        if (imageSprite != null)
        {
            rect_target.sizeDelta = CustomCalculator.GetSpriteSize(rect_base, (imageSprite.rect.width, imageSprite.rect.height));

            puzzleImage.sprite = imageSprite;
        }
        else
        {
            Debug.LogError($"Sample image not found at path: Puzzle/{smapleImage}");
        }
    }


    public void OnClickStartButton()
    {
        if (!StaminaManager.Instance.TryUseStamina(m_data.Stamina))
        {
            PopupInfoUI.Show("Notice", "lack of stamina");
            return;
        }

        SoManager.Instance.ChoosePuzzle = m_data;
        SceneLoader.LoadLoaderScene(GameConfig.SingleSceneName);
    }

    public void OnClickCancleButton()
    {
        CloseUI();
    }

    private void OnDestroy()
    {
        if((m_data != null && puzzleImage.sprite != null) || !string.IsNullOrEmpty(smapleImage))
        {
            ResourceManager.Instance.ReleaseAsset(smapleImage);
        }
    }
}
