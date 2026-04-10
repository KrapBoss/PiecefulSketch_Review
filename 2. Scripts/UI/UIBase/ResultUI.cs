using Custom;
using Managers;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UI.Core;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 결과를 표시하는 UI
/// </summary>
public class ResultUI : UIBase, HUDContainer
{
    public Image image_Result;                // 결과 이미지를 표시할 이미지
    public TextMeshProUGUI text_coin;           // 획득 코인을 표시할 텍스트
    public TextMeshProUGUI text_description;    // 퍼즐 설명을 추가할 텍스트
    public TextMeshProUGUI text_timer;    //    // 현재 클리어 시간을 입력할 부분
    public GameObject go_coin;                  // 코인 게임오브젝트
    public GameObject go_storyPanel;                  // 코인 게임오브젝트

    //이미지를 디바이스 화면에 맞출 때 사용할 비율
    public float ratio = 0.7f;


    /// <summary> 추가 코인 받기 버튼 </summary>
    public GameObject go_additive;
    public TextMeshProUGUI txt_adText;  // 광고 텍스트
    public TextMeshProUGUI txt_name;  // 광고 텍스트
    public TextMeshProUGUI txt_coin;  // 코인 보여주는 텍스트

    [Header("샘플 이미지 값")]
    public RectTransform rect_base;     // 배경 사이즈
    public RectTransform rect_target;   // 타켓 사이즈
    public float pictureSize = 800;

    Capture2D capture2D;
    Texture2D texture = null;
    bool isFirstClear = false;
    bool IsGetAdditiveCoin = false;
    SecureInt totalEarnedCoin = new(0);

    [Header("타임 어택 보상")]
    [SerializeField] private Slider timeSlider;
    [SerializeField] private RewardStepUI[] rewardSteps;
    [SerializeField] private RectTransform[] rewardStepMarkers;
    [SerializeField] private GameObject timeAttackRewardGroup;

    [Header("반복 보상")]
    [SerializeField] private RewardStepUI repeatRewardUI;

    public override void Init()
    {
        base.Init();

        gameObject.SetActive(true);
        capture2D = FindObjectOfType<Capture2D>();
        Vibrator.Vibrate(250);

        Vector2 size = CustomCalculator.GetSpriteSize(rect_base, (capture2D.Picture.rect.width, capture2D.Picture.rect.height));
        rect_target.sizeDelta = size;
        image_Result.sprite = capture2D.Picture;
        image_Result.preserveAspect = true;

        PuzzleSO data = SoManager.Instance.GetChoosePuzzle();
        SecureFloat clearTime = TimerUI.Timer;
        float floatClearTime = (float)clearTime;

        // 애니메이션에 이전 상태 정보가 필요하므로, 원본을 복사해서 사용합니다.
        var originalProgress = SaveDataManager.GetPuzzleProgress(data.Identity.Category, data.Identity.ID);
        var progressToUpdate = new PuzzleProgress
        {
            Id = originalProgress.Id,
            IsCleared = originalProgress.IsCleared,
            BestClearTime = originalProgress.BestClearTime,
            TimeAttackRewardsClaimed = (bool[])originalProgress.TimeAttackRewardsClaimed.Clone()
        };

        if (string.IsNullOrEmpty(progressToUpdate.Id))
        {
            progressToUpdate.Id = SaveDataManager.GetPuzzleKey(data.Identity.Category, data.Identity.ID);
        }

        // --- 반복 보상 계산 (신규 로직) ---
        int repeatRewardAmount = PuzzleRewardCalculator.CalculateRepeatReward(originalProgress, data);
        bool isRepeatClear = originalProgress.IsCleared;

        // 반복 보상 UI는 기본적으로 비활성화
        if (repeatRewardUI != null)
        {
            repeatRewardUI.gameObject.SetActive(false);
        }

        // --- 총 획득 코인 계산 (로직 변경) ---

        // 1. 신규 달성 랭크부터 확인
        var newlyAchievedRanks = new List<int>();
        for (int rank = 1; rank <= 3; rank++)
        {
            // 아직 획득하지 않은 보상이고, 목표 시간을 달성했다면
            if (!originalProgress.TimeAttackRewardsClaimed[rank - 1] && floatClearTime <= PuzzleRewardCalculator.GetTargetTime(rank, data.PieceCount))
            {
                newlyAchievedRanks.Add(rank);
            }
        }

        // 2. 총 코인 합산
        totalEarnedCoin = 0;
        bool isFirstClear = !originalProgress.IsCleared;

        if (isFirstClear)
        {
            totalEarnedCoin += data.RewardCoin; // 최초 클리어 보상
        }

        // 신규 달성 랭크 보상 추가
        foreach (var rank in newlyAchievedRanks)
        {
            totalEarnedCoin += PuzzleRewardCalculator.GetRewardAmount(rank, data.RewardCoin);
        }

        // "아무것도 새로 얻지 못한 반복 클리어"일 경우에만 반복 보상 추가 및 UI 표시
        if (isRepeatClear && newlyAchievedRanks.Count == 0)
        {
            totalEarnedCoin += repeatRewardAmount;

            if (repeatRewardUI != null)
            {
                repeatRewardUI.gameObject.SetActive(true);
                repeatRewardUI.Set(repeatRewardAmount, Localization.Localize("Repeat Reward"));
                repeatRewardUI.TurnOff(); // 정보 표시용이므로 Off 상태로
            }
        }

        // 데이터 업데이트 및 저장
        progressToUpdate.IsCleared = true;
        foreach (var rank in newlyAchievedRanks)
        {
            progressToUpdate.TimeAttackRewardsClaimed[rank - 1] = true;
        }
        if (progressToUpdate.BestClearTime < 0 || floatClearTime < progressToUpdate.BestClearTime)
        {
            progressToUpdate.BestClearTime = floatClearTime;
        }
        SaveDataManager.SavePuzzleProgress(progressToUpdate);

        text_description.text = Localization.Localize($"{data.PuzzleName}_Story");
        txt_name.text = Localization.Localize(data.PuzzleName);
        txt_coin.text = CustomCalculator.TFCoinString(totalEarnedCoin);
        txt_adText.text = PlayerData.BlockAD == 1 ? Localization.Localize("Free! Get extra coins right now! x3") : Localization.Localize("Watch an ad for extra rewards! x3");
        text_timer.text = CustomCalculator.FormatTime(floatClearTime);

        SoundManager.Instance.PlayEffect(SoundNames.SHUTTER);
        HUDContainer.Instance = this;

        // 사용자가 수정한 로직 보존: 코인 획득 처리 및 UI 업데이트를 먼저 호출
        UpdateCoin();
        //코인 먼저 저장
        EarnCoin();

        go_additive.SetActive(false);
        go_coin.SetActive(false);
        if (timeAttackRewardGroup != null)
        {
            timeAttackRewardGroup.SetActive(true);
            // 컴파일 오류 수정: 코루틴에 필요한 모든 정보를 전달
            StartCoroutine(AnimateRewards(data, originalProgress, isFirstClear, newlyAchievedRanks));
        }
        else if (totalEarnedCoin > 0)
        {
            UpdateCoin();
            go_additive.SetActive(true);
            go_coin.SetActive(true);
        }
    }

    private IEnumerator AnimateRewards(PuzzleSO puzzle, PuzzleProgress oldProgress, bool isFirstClear, List<int> newlyAchievedRanks)
    {
        if (timeSlider == null || rewardSteps == null || rewardStepMarkers == null || rewardSteps.Length != 4 || rewardStepMarkers.Length != 4)
        {
            Debug.LogError("Reward UI 요소가 4개로 설정되지 않았습니다.");
            yield break;
        }

        // 목표 정보 텍스트 설정
        rewardSteps[0].Set(puzzle.RewardCoin, "Default Reward");
        for (int i = 0; i < 3; i++)
        {
            int rank = i + 1;
            int rewardAmount = PuzzleRewardCalculator.GetRewardAmount(rank, puzzle.RewardCoin);
            float targetTimeValue = PuzzleRewardCalculator.GetTargetTime(rank, puzzle.PieceCount);
            string formattedTime = Custom.CustomCalculator.FormatTime(targetTimeValue);
            rewardSteps[i + 1].Set(rewardAmount, formattedTime);
        }

        // 2. 애니메이션 시작점 및 초기 상태 설정
        int startSliderValue = 0;
        if (!isFirstClear)
        {
            if (oldProgress.IsCleared) startSliderValue = 1;
            for (int i = 0; i < 3; i++)
            {
                if (oldProgress.TimeAttackRewardsClaimed[i])
                {
                    startSliderValue = i + 2;
                }
                else
                {
                    break;
                }
            }
        }

        for (int i = 0; i < 4; i++)
        {
            if (i < startSliderValue) rewardSteps[i]?.TurnOff();
            else rewardSteps[i]?.TurnOn();
        }

        timeSlider.minValue = 0;
        timeSlider.maxValue = 4;
        timeSlider.value = startSliderValue;

        RectTransform sliderRect = timeSlider.GetComponent<RectTransform>();
        if (sliderRect != null)
        {
            float sliderWidth = sliderRect.rect.width;
            for (int i = 0; i < rewardStepMarkers.Length; i++)
            {
                float markerRatio = (float)(i + 1) / 4.0f;
                rewardStepMarkers[i].anchoredPosition = new Vector2(sliderWidth * markerRatio, rewardStepMarkers[i].anchoredPosition.y);
            }
        }

        yield return new WaitForSeconds(0.5f);

        // 3. 신규 달성 랭크 애니메이션
        float currentSliderValue = startSliderValue;
        var ranksToAnimate = new List<int>();
        if (isFirstClear) ranksToAnimate.Add(0); // 기본보상
        ranksToAnimate.AddRange(newlyAchievedRanks);
        ranksToAnimate.Sort();

        SoundManager.Instance.CashCoinGet();

        foreach (var rank in ranksToAnimate)
        {
            int uiIndex = rank == 0 ? 0 : rank;
            float targetSliderValue = uiIndex + 1;

            if (targetSliderValue > currentSliderValue)
            {
                yield return AnimateSliderTo(currentSliderValue, targetSliderValue, 0.5f);
                currentSliderValue = targetSliderValue;

                rewardSteps[uiIndex]?.TurnOff();

                SoundManager.Instance.PlayCoinGet();
                SoundManager.Instance.PlayEffect(SoundNames.STAMP);

                yield return new WaitForSeconds(0.2f);
            }
        }

        // 4. 코인 획득 처리 (사용자 로직 보존: 애니메이션 후에는 UI 업데이트만)
        if (totalEarnedCoin > 0)
        {
            UpdateCoin();
            go_additive.SetActive(true);
            go_coin.SetActive(true);
        }
    }

    // ... 나머지 코드는 이전과 동일하게 유지 ...
    private void OnDestroy() { HUDContainer.Instance = null; if (texture != null) Destroy(texture); }
    private void Start() { EventManager.Instance.SetNight(); }
    private IEnumerator AnimateSliderTo(float start, float end, float duration) { float elapsedTime = 0f; while (elapsedTime < duration) { elapsedTime += Time.deltaTime; timeSlider.value = Mathf.Lerp(start, end, elapsedTime / duration); yield return null; } timeSlider.value = end; }
    void SetRectImage(Vector2 size)
    {
        float ratio = size.x / size.y;
        if (ratio > 1)
            rect_base.sizeDelta = new Vector2(pictureSize, pictureSize / ratio);
        else
            rect_base.sizeDelta = new Vector2(pictureSize * ratio, pictureSize);
    }
    public void OnClickNoThankYou() { SceneLoader.LoadLoaderScene(GameConfig.TitleSceneName); }
    public void OnClickGetAdditiveCoin() { if ((totalEarnedCoin > 0) && !IsGetAdditiveCoin) { if (PlayerData.BlockAD == 0) { AdsManager.Instance.ShowRewardedAd(() => { PlayerData.TryAddCoin(totalEarnedCoin); txt_coin.text = $"{CustomCalculator.TFCoinString(totalEarnedCoin)} + {CustomCalculator.TFCoinString(totalEarnedCoin)}"; SoundManager.Instance.PlayEffect(SoundNames.COIN_PICK); IsGetAdditiveCoin = true; }); } else { PlayerData.TryAddCoin(totalEarnedCoin); txt_coin.text = $"{CustomCalculator.TFCoinString(totalEarnedCoin)} + {CustomCalculator.TFCoinString(totalEarnedCoin)}"; SoundManager.Instance.PlayEffect(SoundNames.COIN_PICK); IsGetAdditiveCoin = true; } } else { Notice.Message("Available only once"); } }
    public void OnClickDownloadImage() { SaveImageToGallery(); }
    private void SaveImageToGallery() { if (capture2D == null || capture2D.Picture == null) return; texture = ToTexture2D(capture2D.Picture); if (texture == null) return; if (!NativeGallery.CheckPermission(NativeGallery.PermissionType.Write, NativeGallery.MediaType.Image)) { NativeGallery.OpenSettings(); } NativeGallery.SaveImageToGallery(texture, "PiecefulSketch", Localization.Localize(SoManager.Instance.GetChoosePuzzle().PuzzleName), (success, path) => { Notice.Message(success ? "Saved to Gallery!" : "Save failed"); SoundManager.Instance.PlayEffect(SoundNames.THIRING); }); }
    private Texture2D ToTexture2D(Sprite sprite) { try { var tex = new Texture2D((int)sprite.rect.width, (int)sprite.rect.height, sprite.texture.format, false); tex.SetPixels(sprite.texture.GetPixels((int)sprite.textureRect.x, (int)sprite.textureRect.y, (int)sprite.textureRect.width, (int)sprite.textureRect.height)); tex.Apply(); return tex; } catch { return null; } }
    public void EarnCoin()
    {
        if (totalEarnedCoin > 0)
            PlayerData.TryAddCoin(totalEarnedCoin, false);
    }

    public void OnClickShowStory() { go_storyPanel.SetActive(!go_storyPanel.activeSelf); SoundManager.Instance.PlayClickUI(); EventManager.Instance.SetNight(); }
    [SerializeField] TextMeshProUGUI text_Coin;
    [SerializeField] int _cashCoin = -1;
    public void UpdateCoin()
    {
        if (_cashCoin < 0)
        {
            _cashCoin = PlayerData.Coin;
            text_Coin.text = CustomCalculator.TFCoinString(PlayerData.Coin);
        }
        else
        {
            int currentCoin = PlayerData.Coin;
            int subCoin = currentCoin - _cashCoin;
            int cashCoin = _cashCoin; _cashCoin = PlayerData.Coin;
            if (subCoin <= 0) { text_Coin.text = CustomCalculator.TFCoinString(PlayerData.Coin); }
            else
            {
                SoundManager.Instance.CashCoinGet();
                int itemCoin = RewardAnimationManager.Instance.GetOneValue(subCoin);

                SoundManager.Instance.PlayEffect(SoundNames.COIN_EARN);

                int counting = 1; RewardAnimationManager.Instance.StopAll(); RewardAnimationManager.Instance.Play(text_Coin.GetComponent<RectTransform>(), subCoin, ResourceNames.ATLAS_ITEM_ICON, ItemNames.COIN, objectComplete: () =>
                {
                    int addtiveCoin = (itemCoin * (counting++));
                    if (addtiveCoin > subCoin) { addtiveCoin = subCoin; }
                    text_Coin.text = CustomCalculator.TFCoinString(cashCoin + addtiveCoin);
                    SoundManager.Instance.PlayCoinGet();
                }, onComplete: () => { text_Coin.text = CustomCalculator.TFCoinString(currentCoin); });
            }
        }
    }
    public void UpdateItem() { }
    public void OnDestroyMethod() { }
    public void UpdateTimer() { }

    public void UpdateStamina()
    {

    }
}
