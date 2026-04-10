using Custom;
using Global;
using Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UI.Core;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;

/// <summary>
/// 상점 UI를 관리하는 메인 클래스입니다. UIBase를 상속받습니다.
/// </summary>
public class ShopUIController : TapMenuPanel, PuzzleItemContainerParent
{
    [SerializeField] private List<ProductUI> productUIList; // 인스펙터에서 직접 할당할 상품 UI 리스트
    [SerializeField] private RectTransform refreshRect;

    public override void Active()
    {
        base.Active();

        UpdateAllProducts();

        SoundManager.Instance.PlayEffect(SoundNames.ENTER_SHOP);
    }

    /// <summary>
    /// 리스트에 있는 모든 상품 UI를 다시 초기화합니다.
    /// </summary>
    public void UpdateAllProducts()
    {
        if (productUIList == null) return;

        foreach (var productUI in productUIList)
        {
            if (productUI != null)
            {
                productUI.Initialize();
            }
        }
    }

    public void UpdateState()
    {
        if (refreshRect)
        {
            // 1. 캔버스 데이터 강제 동기화
            Canvas.ForceUpdateCanvases();
            // 최상위 캔버스까지 올라가며 갱신하는 대신, 
            // 영향을 받는 가장 인접한 레이아웃 루트를 찾아 갱신하는 것이 효율적임
            LayoutRebuilder.ForceRebuildLayoutImmediate(refreshRect);

            // Content Size Fitter가 부모에 있을 경우 부모도 명시적으로 호출
            //if (refreshRect.parent is RectTransform parentRT)
            //{
            //    LayoutRebuilder.ForceRebuildLayoutImmediate(parentRT);
            //}
        }
     }


    /// <summary>
    /// [기능] 구매 복구 및 미결제 내역 스토어 일괄 확정 처리
    /// [제작 의도] 누락된 보상을 모아 한 번에 지급한 뒤, 처리된 트랜잭션들을 IAPManager에 전달하여 스토어 Confirm 일괄 처리
    /// [Unity 효율성] 이전 스텝에서 제작한 Dictionary<TransactionID, PendingOrder> 구조에 맞춰 데이터를 안전하게 추출하며, 불필요한 스토어 개별 통신 대신 배치를 활용
    /// </summary>
    public async void OnClickRestorePurchases()
    {
        SoundManager.Instance.PlayClickUI();

        try
        {
            if (!SaveDataManager.CanUpload || !IAPManager.Instance.IsReady)
            {   // 업로드가 불가능한 상태라면 구매 복구가 불가능합니다.
                Notice.Message("Need Connect Network");
                return;
            }
        }
        catch(Exception e)
        {
            Notice.Message("Reconnect after network connection");
            return;
        }

        await LoadingUI.Show("Restoring Purchases...");

        // 1. Get purchase histories (Key: transactionId, Value: PendingOrder)
        var iapHistory = IAPManager.Instance.GetPendingOrders();
        var savedLogs = SaveDataManager.GetAllPurchaseLogs();

        // 2. Find unrewarded purchases
        var productIdsToRestore = new List<string>();
        var newPurchaseLogs = new List<SaveDataManager.PurchaseLog>();
        var transactionIdsToConfirm = new List<string>(); // [추가] 스토어에 확정(Confirm) 보낼 ID 목록

        foreach (var iapPurchase in iapHistory)
        {
            string transactionId = iapPurchase.Key;
            var pendingOrder = iapPurchase.Value;

            // PendingOrder에서 productId 안전하게 추출
            string productId = pendingOrder?.Info?.PurchasedProductInfo?.FirstOrDefault()?.productId;

            Debug.LogWarning($"[IAP] Restore {transactionId} : {productId}");

            if (string.IsNullOrEmpty(transactionId) || string.IsNullOrEmpty(productId)) continue;

            // 이미 처리되었든 안 되었든 펜딩 상태인 주문은 모두 확정(Confirm) 리스트에 추가하여 스토어 큐에서 제거
            transactionIdsToConfirm.Add(transactionId);

            Debug.LogWarning($"[IAP] Restore Check {savedLogs.Any(log => log.TransactionId == transactionId)}");

            // 고유 ID 체크 후 지급 안된 보상 확인
            if (!savedLogs.Any(log => log.TransactionId == transactionId))
            {
                Debug.LogWarning($"[IAP] Restore SEt {transactionId} : {productId}");
                productIdsToRestore.Add(productId);

                // Create a new log entry for saving later
                var newLog = new SaveDataManager.PurchaseLog
                {
                    ProductId = productId,
                    TransactionId = transactionId,
                    Platform = Application.platform.ToString(),
                    PurchaseTime = SaveDataManager.GetKstString(),
                    Language = Application.systemLanguage.ToString(),
                    CurrentCoin = PlayerData.Coin,
                    UserId = AuthenticationManager.GetPlayerId(),
                    DeviceModel = SystemInfo.deviceModel + "_SP" // Special flag for restored purchases
                };
                newPurchaseLogs.Add(newLog);
            }
        }

        // 3. 누락된 보상이 있다면 일괄 지급 및 로그 저장
        if (productIdsToRestore.Count > 0)
        {
            // 보상 종합 후 보상 지급
            GrantAggregatedRewards(productIdsToRestore);

            // 4. Save the restored purchase logs
            await SaveDataManager.SaveRestoredPurchases(newPurchaseLogs);

            Notice.Message($"purchases have been restored.");
        }
        else
        {
            Notice.Message("No new purchases to restore.");
        }

        // 5. [추가] 보상 처리 완료 후, 펜딩 내역들을 스토어에 일괄 확정(Confirm) 처리
        if (transactionIdsToConfirm.Count > 0)
        {
            IAPManager.Instance.ConfirmPendingPurchases(transactionIdsToConfirm);
        }

        LoadingUI.Close();
    }


    /// <summary>
    /// 제공된 상품 ID 목록에 대한 보상을 합산하여 지급합니다.
    /// </summary>
    private static void GrantAggregatedRewards(List<string> productIds)
    {
        int totalCoinToAdd = 0;
        int totalStaminaToAdd = 0;

        foreach (var productId in productIds)
        {
            var productInfo = ProductData.GetProductInfo(productId);
            if (productInfo == null)
            {
                Debug.LogWarning($"[SaveDataManager] GrantRewards: ProductInfo not found for ID: {productId}");
                continue;
            }

            foreach (var reward in productInfo.Rewards)
            {
                switch (reward.ItemName)
                {
                    case string name when name.Equals(ItemNames.COIN):
                        totalCoinToAdd += reward.Amount;
                        break;
                    case string name when name.Equals(ItemNames.STAIMINA):
                        totalStaminaToAdd += reward.Amount;
                        break;
                    case string name when name.Equals(ItemNames.AD_BLOCK):
                        if (PlayerData.BlockAD != 1)
                        {
                            PlayerData.BlockAD = 1;
                        }
                        break;
                    default:
                        Debug.LogWarning($"[SaveDataManager] Unknown reward item name: {reward.ItemName}");
                        break;
                }
            }
        }

        // 합산된 보상 지급
        if (totalCoinToAdd > 0)
        {
            PlayerData.TryAddCoin(totalCoinToAdd);
        }
        if (totalStaminaToAdd > 0)
        {
            StaminaManager.Instance.AddStamina(totalStaminaToAdd);
        }
    }
}
