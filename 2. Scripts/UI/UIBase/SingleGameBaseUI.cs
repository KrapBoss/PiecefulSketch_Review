using Custom;
using Global;
using Managers;
using System.Collections;
using System.Threading.Tasks;
using TMPro;
using UI.Core;
using UnityEngine;

public class SingleGameBaseUI : UIBase, HUDContainer
{
    private int m_currentCoin;
    public TextMeshProUGUI text_Hint;
    public GameObject go_hintAd;          //힌트를 보기 위한 광고 표기

    [SerializeField] UIAnimationParent cs_coinUpdate;   // 코인정보 업데이트

    [Space, Header("Important")]
    [SerializeField] PuzzlePieceScrollView cs_scrollView;

    [SerializeField] TextMeshProUGUI txt_timer;
    [SerializeField] GameObject go_timer;

    private void Update()
    {
        if (GameSetting.Timer) txt_timer.text = CustomCalculator.FormatTime(TimerUI.Timer);
    }

    private void OnDestroy()
    {
        HUDContainer.Instance = null;
        OnDestroyMethod();
    }

    public void OnDestroyMethod()
    {
        try
        {
            if (RewardAnimationManager.Instance != null)
            {
                RewardAnimationManager.Instance.StopAll();
            }
        }
        catch (System.Exception e)
        {
            CustomDebug.PrintW($"[{gameObject.name}] : {e.InnerException.Message}");
        }
    }

    public override void Init()
    {
        base.Init();
        HUDContainer.Instance = this;

        // 초기화 시엔 애니메이션 없이 즉시 적용
        m_currentCoin = 0;
        HUDContainer.Instance.UpdateCoin();
        HUDContainer.Instance.UpdateItem();
        HUDContainer.Instance.UpdateTimer();

        //퍼즐 데이터를 불러와 표기합니다.
        cs_scrollView.RegistPiece();
    }

    public async void OnClickLeaveGame()
    {
        if (TutorialGuide.Tutorialing)
        {
            TutorialGuide.Instance.OnClickActiveSkip();
            return;
        }

        await UIScreenManager.Instance.ShowUI(UIName.LEAVE_GAME_UI);
    }

    public void UpdateCoin()
    {
        cs_coinUpdate?.Show();

        go_hintAd.SetActive(ItemData.GetItemBuyPrice(ItemType.HINT) > PlayerData.Coin);
    }



    public void UpdateItem()
    {
        if(ItemData.Instance.GetItemCount(ItemType.HINT) <= 0)
        {
            text_Hint.text = string.Empty;
        }
        else
        {
            text_Hint.text = $"x {ItemData.Instance.GetItemCount(ItemType.HINT)}";
        }
    }

    public void Block(bool active)
    {
        if(CanvasGroup)
        {
            CanvasGroup.interactable = active;
            if (active)
            {
                CanvasGroup.alpha = 1;
            }
            else
            {
                CanvasGroup.alpha = 0;
            }
        }
    }

    public void UpdateTimer()
    {
        go_timer.SetActive(GameSetting.Timer);
    }

    public void UpdateStamina()
    {

    }
}