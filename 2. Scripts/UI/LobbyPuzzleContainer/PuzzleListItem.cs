using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Managers;
using Global;
using Custom;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// LOBBY 내 퍼즐 슬롯에 대한 각 정보
/// </summary>
public class PuzzleListItem : MonoBehaviour
{
    [SerializeField] private Image thumbnailImage;
    [SerializeField] private TextMeshProUGUI infoText;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI descText;
    [SerializeField] private Button playButton;
    [SerializeField] UIAnimationParent cs_parent;

    [Header("스토리 텍스트")]
    [SerializeField] private GameObject go_desc;

    [SerializeField] private GameObject go_lock;

    [Header("구매를 위한 텍스트")]
    [SerializeField] GameObject go_price;
    [SerializeField] TextMeshProUGUI txt_price;

    [Header("보상 등급 아이콘")]
    [SerializeField] GameObject rewardIcon;
    [SerializeField] private GameObject[] rewardTierIcons = new GameObject[4];

    [Header("갤러리 UI 인가?")]
    [SerializeField] bool isGallery = false;

    // 현재 로드된 이미지의 키 (이미지 존재 여부 판단 기준)
    private string _currentLoadedSpriteKey = string.Empty;
    private CancellationTokenSource _cts;

    PuzzleSO m_data;
    PuzzleProgress m_progress;
    bool m_unlocked;

    /// <summary> 현재 데이터 로딩 중 </summary>
    bool nowLoading = false;

    /// <summary>
    /// 퍼즐 슬롯 데이터 설정 및 리소스 로드 (통합 최적화)
    /// </summary>
    public async void Setup(PuzzleSO data, bool isUnlocked, PuzzleProgress progress, Vector2 cellSize)
    {
        // 1. 데이터 설정
        bool sameData = m_data?.Equals(data) ?? false;
        m_data = data;
        m_progress = progress;
        m_unlocked = isUnlocked;

        if(BuildVersion.IsLocal) m_unlocked = true;  // 로컬은 퍼즐 전부 해제

        // 2. UI 정보 갱신 (데이터가 같든 다르든 텍스트/잠금 상태 등은 항상 최신화)
        Refresh();

        // 3. 로드 필요 여부 판단
        // 조건: 데이터가 변경되었거나 OR 현재 로드된 이미지 키가 없는 경우(이미지 유실/초기화 상태)
        bool needLoad = !sameData || string.IsNullOrEmpty(_currentLoadedSpriteKey);

        if (needLoad)
        {
            // [로딩 프로세스 시작]

            // 기존 작업 취소 및 토큰 생성
            if (_cts != null) { _cts.Cancel(); _cts.Dispose(); }
            _cts = new CancellationTokenSource();
            CancellationToken token = _cts.Token;

            // 이전 리소스 무조건 해제
            ReleasePreviousResource(_currentLoadedSpriteKey);

            if (m_progress.IsCleared)
            {// 클리어 된 경우 완성형 이미지
                _currentLoadedSpriteKey = data.SampleImageName;
            }
            else
            {// 클리어 안된 경우 샘플 이미지
                _currentLoadedSpriteKey = data.SampleImageNameBack;
            }
            CustomDebug.Print($"{(m_progress.IsCleared?"Clear" : "not Clear")}{_currentLoadedSpriteKey}");

            // Task 생성 (즉시 await 하지 않음)
            Task<Sprite> loadTask = ResourceManager.Instance.LoadAsset<Sprite>(_currentLoadedSpriteKey, true);

            nowLoading = true;
            // A. 캐시 등으로 즉시 완료된 경우 -> 깜빡임 없이 바로 적용
            if (loadTask.IsCompleted)
            {
                if (loadTask.Status == TaskStatus.RanToCompletion)
                {
                    ApplySpriteSettings(loadTask.Result, cellSize);
                    AlphaOne();
                }
                else
                {
                    thumbnailImage.sprite = null; // 로드 실패
                    cs_parent?.Hide();
                }
            }
            // B. 실제 다운로드/로딩 대기가 필요한 경우 -> Hide 연출 후 적용
            else
            {
                cs_parent?.Hide(); // 로딩 중임을 알리기 위해 숨김

                try
                {
                    Sprite sprite = await loadTask;
                    if (token.IsCancellationRequested) return;

                    ApplySpriteSettings(sprite, cellSize);
                    cs_parent?.Show(); // 로드 완료 후 노출

                    CheckAfterTask();
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[PuzzleListItem] Load Failed: {e.Message}");
                }
            }
            nowLoading = false;
        }
        else
        {
            // [로드가 필요 없는 경우] (데이터 동일 && 이미지 존재)
            if(!nowLoading) AlphaOne();

            // 기존 이미지 사이즈 재계산 (셀 크기가 변했을 수 있으므로)
            if (thumbnailImage != null && thumbnailImage.sprite != null)
            {
                ApplySpriteSettings(thumbnailImage.sprite, cellSize);
            }

            //cs_parent?.Show();
        }
    }

    void CheckAfterTask()
    {
        if (!gameObject.activeInHierarchy)
        {
            CustomDebug.Print("비활성화로 인해 메모리 반환");
            Off();
        }
    }

    void AlphaOne()
    {
        // UI 상태 보정 (숨겨져 있었다면 다시 보이게 처리)
        if (cs_parent is UIAlphaAnimator animator && animator.Group != null)
        {
            animator.Group.alpha = 1;
        }
    }

    /// <summary>
    /// 로드된 스프라이트를 이미지 컴포넌트에 적용하고 사이즈를 조정합니다.
    /// </summary>
    private void ApplySpriteSettings(Sprite sprite, Vector2 cellSize)
    {
        if (thumbnailImage != null && sprite != null)
        {
            thumbnailImage.sprite = sprite;
            // 원본 비율 유지를 위한 사이즈 계산
            thumbnailImage.GetComponent<RectTransform>().sizeDelta =
                CustomCalculator.GetSpriteSize((cellSize.x, cellSize.y), (sprite.rect.width, sprite.rect.height));
        }
    }

    /// <summary>
    /// UI 텍스트 및 버튼 상태 갱신 (리소스 로드와 무관한 가벼운 작업)
    /// </summary>
    void Refresh()
    {
        playButton.onClick.RemoveAllListeners();
        bool isPurchased = (m_data.PuzzleType == PuzzleType.Free) || PlayerData.IsPuzzlePurchased(m_data.PuzzleName) || !m_unlocked;

        // 초기화
        if (go_lock) go_lock.SetActive(false);
        if (go_price) go_price.SetActive(false);
        if (go_desc) go_desc.SetActive(false);

        // 구매 여부에 따른 상태 설정
        if (isPurchased)
        {
            playButton.interactable = m_unlocked;

            if (m_unlocked)
            {
                thumbnailImage.color = Color.white;
            }
            else
            {
                thumbnailImage.color = new Color(0.4f, 0.4f, 0.4f);
                if (go_lock) go_lock.SetActive(true);
            }
        }
        else
        {
            playButton.interactable = true; // 구매 가능 상태

            if (go_price) go_price.SetActive(true);
            if (go_lock) go_lock.SetActive(true);

            thumbnailImage.color = new Color(0.3f, 0.3f, 0.3f);
            if (txt_price) txt_price.text = CustomCalculator.TFCoinString(m_data.PuzzlePrice);
        }

        playButton.onClick.AddListener(OnClickPuzzleStart);

        if (infoText) infoText.text = $"{m_data.PieceCount}";
        if (timerText != null) timerText.text = CustomCalculator.FormatTime(m_progress.BestClearTime);
        if (descText != null) descText.text = Localization.Localize($"{m_data.PuzzleName}_Story");

        UpdateRewardTierIcons(m_progress);
    }

    private void UpdateRewardTierIcons(PuzzleProgress progress)
    {
        if (rewardTierIcons == null || rewardTierIcons.Length != 4) return;

        foreach (var icon in rewardTierIcons)
        {
            if (icon != null) icon.SetActive(false);
        }

        if (progress == null || !progress.IsCleared)
        {
            if (rewardIcon) rewardIcon.SetActive(false);
            return;
        }

        if (rewardIcon) rewardIcon.SetActive(true);

        int highestTierIndex = 0;
        if (progress.TimeAttackRewardsClaimed.Length > 2 && progress.TimeAttackRewardsClaimed[2]) highestTierIndex = 3;
        else if (progress.TimeAttackRewardsClaimed.Length > 1 && progress.TimeAttackRewardsClaimed[1]) highestTierIndex = 2;
        else if (progress.TimeAttackRewardsClaimed.Length > 0 && progress.TimeAttackRewardsClaimed[0]) highestTierIndex = 1;

        if (rewardTierIcons[highestTierIndex] != null)
        {
            rewardTierIcons[highestTierIndex].SetActive(true);
        }
    }

    private async void OnClickPuzzleStart()
    {
        SoundManager.Instance.PlayClickUI();

        if (isGallery)
        {
            CustomDebug.Print("[Puzzle] 회상용 UI 를 불러옵니다.");

            await UIScreenManager.Instance.ShowUI(UIName.PUZZLE_RECALL_UI);
            var ui = UIScreenManager.Instance.GetUI(UIName.PUZZLE_RECALL_UI) as PuzzleReCallUI;
            if (ui != null) ui.Init(m_data);

            return;
        }

        bool isPurchased = (m_data.PuzzleType == PuzzleType.Free) || PlayerData.IsPuzzlePurchased(m_data.PuzzleName);

        if (!isPurchased)
        {
            UI.PopUp.PopupConfirmUI.Show("Unlock Puzzle", string.Format(Localization.Localize("Spend {0} coins to purchase this puzzle?"), m_data.PuzzlePrice),
            () => {
                bool success = PlayerData.TryPurchasePuzzle(m_data);
                if (success)
                {
                    SoundManager.Instance.PlayEffect(SoundNames.COIN_PICK);
                    Notice.Message(string.Format(Localization.Localize("purchase success {0}"), Localization.Localize(m_data.PuzzleName)));
                    Refresh();
                }
                else
                {
                    Notice.Message("Not enough coins");
                }
            });
        }
        else
        {
            if (m_unlocked)
            {
                await UIScreenManager.Instance.ShowUI(UIName.PUZZLE_START_UI);
                var ui = UIScreenManager.Instance.GetUI(UIName.PUZZLE_START_UI) as PuzzleStartUI;
                if (ui != null) ui.Init(m_data);
            }
            else
            {
                Notice.Message("Need UnLock");
            }
        }
    }

    /// <summary>
    /// 리소스 해제
    /// </summary>
    /// <param name="name"></param>
    /// <returns> 해제 가능 리소스가 있을 경우 true </returns>
    private bool ReleasePreviousResource(string name)
    {
        if (!string.IsNullOrEmpty(name))
        {
            thumbnailImage.sprite = null;
            return ResourceManager.Instance.ReleaseAsset(name);
        }
        return false;
    }

    private void OnDestroy()
    {
        if (_cts != null)
        {
            _cts.Cancel();
            _cts.Dispose();
        }
        if (!string.IsNullOrEmpty(_currentLoadedSpriteKey)) ReleasePreviousResource(_currentLoadedSpriteKey);
    }

    private void OnDisable()
    {
        Off();
    }

    /// <summary>
    /// 아이템이 화면에서 사라질 때 리소스를 해제합니다.
    /// </summary>
    public void Off()
    {
        //if (_cts != null) { _cts.Cancel(); _cts.Dispose(); }    // 진행 중인 Task 비활성화
        playButton?.onClick.RemoveAllListeners();
        if(ReleasePreviousResource(_currentLoadedSpriteKey)) _currentLoadedSpriteKey = string.Empty;
        cs_parent?.Hide(); 
    }

    /// <summary>
    /// 아이템이 화면에 나타날 때 데이터를 기반으로 다시 설정합니다.
    /// </summary>
    public void On(Vector2 cellSize)
    {
        if (m_data != null)
        {
            Setup(m_data, m_unlocked, m_progress, cellSize);
        }
    }


    public void OnClickDescription()
    {
        if (go_desc) go_desc.SetActive(!go_desc.activeSelf);
    }
}