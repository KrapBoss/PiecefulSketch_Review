using Custom;
using Managers;
using UI.PopUp;
using UnityEngine;

/// <summary>
/// 아이템 구매를 위한 매니저
/// </summary>
public class ShopManager
{
    /// <summary>
    /// 아이템을 구매합니다.
    /// 가격이 맞을 경우에만 아이템을 추가하여 줍니다.
    /// </summary>
    /// <param name="type"></param>
    public static void BuyItem(ItemType type)
    {
        int itemPrice = ItemData.GetItemBuyPrice(type);

        if(PlayerData.Coin >= itemPrice)
        {
            PlayerData.TryAddCoin(-itemPrice);
            ItemData.Instance.AddItemCount(type, ItemData.GetItemBuyCount(type));
            CustomDebug.Print($"{type} 아이템 구매에 성공했습니다");
        }
        //else
        //{   // 돈이 없을 경우 광고를 봅니다.

        //    CustomDebug.Print($"{type} 아이템을 구매할 수 없습니다.");

        //    PopupConfirmUI.Show(
        //        "Notice",
        //        $"You don't have enough coins to use the item.\n Would you like to receive coins after watching an ad?"
        //        );
        //}
    }

    /// <summary>
    /// 아이템 구매
    /// </summary>
    public static void ItemPurChase()
    {
    }
}
