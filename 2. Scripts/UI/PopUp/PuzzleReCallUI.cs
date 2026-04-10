using Custom;
using Managers;
using System.Security.Cryptography;
using TMPro;
using UI.Core;
using UI.PopUp;
using Unity.Services.Authentication;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 퍼즐 시작 전 정보를 표시하는 UI입니다.
/// </summary>
public class PuzzleReCallUI : UIBase
{
    [Header("퍼즐 샘플 이미지")]
    [SerializeField] private Image puzzleImage;

    [Header("퍼즐 텍스트")]
    [SerializeField] private TextMeshProUGUI puzzleNameText;    // 퍼즐 이름
    [SerializeField] private TextMeshProUGUI puzzleTimerText;   // 기록
    [SerializeField] private TextMeshProUGUI needStaminaPer;   // 1번당 소모 개수
    [SerializeField] private TextMeshProUGUI storyText;   // 기록

    [Header("타임 어택 보상 단계 UI")]
    [SerializeField] private RewardStepUI repeatReward;
    [SerializeField] private PuzzleTierItem cs_Tier;

    [SerializeField] private RectTransform rect_base;
    [SerializeField] private RectTransform rect_target;

    [Header("퍼즐 스테미너 UI ")]
    [SerializeField] TextMeshProUGUI text_Stamina;
    [SerializeField] TextMeshProUGUI text_maxStamina;

    [Header("퍼즐 오브젝트 UI 대기")]
    [SerializeField] UIObjectAssembler cs_UIObjectAssembler;

    int m_currentStamina;   // UI에 표시될 현재 설정된 소모 에너지 총량
    int m_maxStamina;       // 플레이어가 현재 보유한 최대 에너지
    int m_staminaPer;       // 1회 플레이 당 소모되는 기준 개수
    SecureInt repeatRewardAmount = new SecureInt(0); // 반복보상 개수

    PuzzleSO m_data;

    /// <summary>
    /// [기능] 객체 파괴 시 리소스 해제
    /// </summary>
    private void OnDestroy()
    {
        if (m_data != null && puzzleImage.sprite != null)
        {
            ResourceManager.Instance.ReleaseAsset(m_data.SampleImageName);
        }
    }

    /// <summary>
    /// [기능] UI 초기화 및 퍼즐 정보 설정
    /// [제작 의도] 퍼즐 진입 시 필요한 기본 데이터를 로드하고 스테미너 설정값을 초기화
    /// [Unity 효율성] 1회 소모량을 기준으로 초기값을 캐싱하여 이후 버튼 연산 최적화
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

        if (storyText) storyText.text = Localization.Localize($"{m_data.PuzzleName}_Story");
        if (needStaminaPer) needStaminaPer.text = $"x{m_data.Stamina}";

        cs_Tier.UpdateRewardTierIcons(progress);

        // 퍼즐 이름 설정
        puzzleNameText.text = Localization.Localize(puzzleData.PuzzleName);

        // 최대 스테미너 지정
        UpdateCoinInfo();

        // [수정] 소비할 스테미너 개수 캐싱 및 초기화 (최소 1회 플레이 기준)
        m_staminaPer = m_data.Stamina;
        m_currentStamina = m_staminaPer;
        UpdateStamina();

        if (puzzleTimerText)
        {   // 퍼즐 클리어 시간
            puzzleTimerText.text = CustomCalculator.FormatTime(progress.BestClearTime);
        }

        // 반복 보상 설정
        if (progress.IsCleared)
        {
            repeatReward.gameObject.SetActive(true);
            repeatRewardAmount = PuzzleRewardCalculator.CalculateRepeatReward(progress, m_data);
            repeatReward.Set(repeatRewardAmount, Localization.Localize("Recall Reward"));
        }
        else
        {
            repeatReward.gameObject.SetActive(false);
        }

        // 퍼즐 샘플 이미지 로드 및 설정
        puzzleImage.color = new Color(1, 1, 1, 0);
        var imageSprite = await ResourceManager.Instance.LoadAsset<Sprite>(puzzleData.SampleImageName);
        puzzleImage.color = new Color(1, 1, 1, 1);

        if (imageSprite != null)
        {
            rect_target.sizeDelta = CustomCalculator.GetSpriteSize(rect_base, (imageSprite.rect.width, imageSprite.rect.height));
            puzzleImage.sprite = imageSprite;
        }
        else
        {
            Debug.LogError($"Sample image not found at path: Puzzle/{puzzleData.SampleImageName}");
        }
    }

    void UpdateCoinInfo()
    {
        m_maxStamina = PlayerData.Stamina;
        text_maxStamina.text = string.Format(Localization.Localize("Max Stamina {0}"), m_maxStamina);
    }

    /// <summary>
    /// [기능] 현재 데이터 기반 UI 업데이트
    /// </summary>
    public void UpdataUI()
    {
        if (m_data != null) Init(m_data);
    }

    /// <summary>
    /// [기능] 회상 버튼 동작
    /// </summary>
    public void OnClickRecallButton()
    {
        UpdateCoinInfo();

        if (m_currentStamina > m_maxStamina)
        {
            //Notice.Message("Cannot set stamina over current max stamina");
            Notice.Message("Leak Stamina");
            return;
        }
        if(repeatRewardAmount.Value <= 0)
        {
            Notice.Message("There is no compensation available");
            return;
        }

        int count = m_currentStamina / m_staminaPer;
        int earnCoin = repeatRewardAmount * count;
        PopupConfirmUI.Show("Notice", string.Format(Localization.Localize("Would you like to reminisce{0}{1}?"), m_currentStamina, earnCoin), () => StartObjectAssembler(earnCoin));
    }

    /// <summary>
    /// [기능] 취소 버튼 동작
    /// </summary>
    public void OnClickCancleButton()
    {
        CloseUI();
    }

    /// <summary>
    /// [기능] 좌측 스테미너 버튼 클릭 처리 (감소)
    /// [제작 의도] 소모 스테미너를 1회 플레이 요구량만큼 감소 (최소 1회 요구량 보장)
    /// [Unity 효율성] 단순 뺄셈 연산으로 조건 처리
    /// </summary>
    public void OnClickLeftStaminaButton()
    {
        SoundManager.Instance.PlayClickUI();

        UpdateCoinInfo();

        // [수정] 현재 설정값이 1회 소모량 이하로 내려가지 않도록 방어
        if (m_currentStamina <= m_staminaPer)
        {
            Notice.Message("Cannot set stamina under base amount");
            return;
        }

        m_currentStamina -= m_staminaPer;
        UpdateStamina();
    }

    /// <summary>
    /// [기능] 우측 스테미너 버튼 클릭 처리 (증가)
    /// [제작 의도] 소모 스테미너를 1회 플레이 요구량만큼 증가 (보유 최대 스테미너 초과 방지)
    /// [Unity 효율성] 단순 덧셈 연산으로 조건 처리
    /// </summary>
    public void OnClickRightStaminaButton()
    {
        SoundManager.Instance.PlayClickUI();

        UpdateCoinInfo();

        // [수정] 1회 소모량을 더했을 때 현재 보유한 최대 스테미너를 초과하는지 검사
        if (m_currentStamina + m_staminaPer > m_maxStamina)
        {
            Notice.Message("Cannot set stamina over current max stamina");
            return;
        }

        m_currentStamina += m_staminaPer;
        UpdateStamina();
    }

    /// <summary>
    /// [기능] 스테미너 텍스트 갱신
    /// </summary>
    void UpdateStamina()
    {
        text_Stamina.text = $"x{m_currentStamina}";
    }


    #region 오브젝트 어셈블러

    /// <summary> 그림 모으기 시작 </summary>
    void StartObjectAssembler(int earnCoin)
    {
        if (!StaminaManager.Instance.TryUseStamina(m_currentStamina))
        {
            return;
        }

        UpdataUI();

        // 코인추가
        PlayerData.TryAddCoin(earnCoin, false);

        SoundManager.Instance.PlayEffect(SoundNames.BELL2, true);
        cs_UIObjectAssembler.StartAssembly("Progress Recall", string.Format(Localization.Localize("Complete Recall {0}"), earnCoin), ObjectAssemblerDone);
    }

    /// <summary> 그림 모으기 종료  </summary>
    void ObjectAssemblerDone()
    {
        //cs_UIObjectAssembler.gameObject.SetActive(false);

        HUDContainer.Instance?.UpdateCoin();

        //Notice.Message("I was rewarded with a picture of my memories");
    }
    #endregion
}