using Managers;
using UI.PopUp;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 힌트 버튼을 눌렀을 경우
/// </summary>
public class OnClickHintButton : MonoBehaviour
{
    private const int limit = 2;    // 최대 2번 구매 가능
    private int currentCount = 0;

    private void Awake()
    {
        Button btn = GetComponent<Button>();
        if (btn)
        {
            btn.onClick.AddListener(OnClickBuyHintItem);
        }
    }

    void OnClickBuyHintItem()
    {
        if(currentCount >= limit)
        {
            Notice.Message(string.Format(Localization.Localize("No more {0} available"), Localization.Localize(ItemType.HINT.ToString())));
            return;
        }

        int itemPrice = ItemData.GetItemBuyPrice(ItemType.HINT);

        if (itemPrice <= PlayerData.Coin)
        {   // 코인 충분
            PopupConfirmUI.Show(
                "Notice",
                string.Format(Localization.Localize("Would you like to purchase a 3 hint item for {0} coins? Usable only in the current puzzle."), itemPrice),
                () => { 
                    ShopManager.BuyItem(ItemType.HINT);
                    SoundManager.Instance.PlayEffect(SoundNames.BELL2);
                    NoticeCount();
                }
                );
        }
        else
        {
            AdsManager.Instance.ShowRewardedAd(() =>
            {
                Debug.LogWarning("코인이 부족해서 광고 시청 후 아이템을 얻었습니다");
                SoundManager.Instance.PlayEffect(SoundNames.BELL2);
                ItemData.Instance.AddItemCount(ItemType.HINT, ItemData.GetItemBuyCount(ItemType.HINT));
                NoticeCount();
            });
        }
    }

    private void NoticeCount()
    {
        currentCount++;
        Notice.Message(string.Format(Localization.Localize("{1} {0} claims remaining"), limit - currentCount, Localization.Localize(ItemType.HINT.ToString())));
    }
}