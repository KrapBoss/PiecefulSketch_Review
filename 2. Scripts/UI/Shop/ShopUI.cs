using Custom;
using Global;
using Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UI.Core;
using UnityEngine;

/// <summary>
/// 상점 UI를 관리하는 메인 클래스입니다. UIBase를 상속받습니다.
/// </summary>
public class ShopUI : UIBase, HUDContainer
{
    [SerializeField] private List<ProductUI> productUIList; // 인스펙터에서 직접 할당할 상품 UI 리스트

    //코인 정보
    [SerializeField] TextMeshProUGUI text_Coin;
    [SerializeField] TextMeshProUGUI text_AddCoinInfo; // 획득 코인 정보 텍스트
    [SerializeField] float coinInfoDisplayDuration = 2f; // 코인 정보 표시 시간
    [SerializeField] int _cashCoin = -1;

    private Coroutine _coinSummaryCoroutine;

    public override void Init()
    {
        base.Init();
        UpdateAllProducts();

        //HUD 변경
        HUDContainer.Instance = this;
        HUDContainer.Instance.UpdateCoin();

        SoundManager.Instance.PlayEffect(SoundNames.ENTER_SHOP);

        if (text_AddCoinInfo != null) text_AddCoinInfo.gameObject.SetActive(false);
    }

    public override void OnClose(Action destroyAction, bool immediately)
    {
        //코인 정보를 업데이트 하기 위한 사전 작업
        var screen = UIScreenManager.Instance.GetUI(UIName.LOBBY_BASE_UI);
        LobbyBaseUI lobbyBaseUI = screen as LobbyBaseUI;
        if (lobbyBaseUI != null) lobbyBaseUI.ClearCash();
        if (screen != null)
        {//HUD 컨테이너 지정
            HUDContainer.Instance = screen.GetComponent<HUDContainer>();
            HUDContainer.Instance.UpdateCoin();
            HUDContainer.Instance.UpdateItem();
        }

        OnDestroyMethod();

        base.OnClose(destroyAction, immediately);
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

    /// <summary>
    /// 리스트에 있는 모든 상품 UI를 다시 초기화합니다.
    /// </summary>
    public void UpdateAllProducts()
    {
        if (productUIList == null) return;

        foreach (var productUI in productUIList)
        {
            if(productUI != null)
            {
                productUI.Initialize();
            }
        }
    }

    //코인 정보 업데이트
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
            int cashCoin = _cashCoin;
            _cashCoin = PlayerData.Coin;

            if (subCoin <= 0)
            {   // 바로 적용
                text_Coin.text = CustomCalculator.TFCoinString(PlayerData.Coin);
            }
            else
            {// 시각적 애니메이션만 재생하고,
             // 애니메이션이 모두 끝나거나 스킵되면 콜백 함수를 실행하여 실제 데이터를 업데이트합니다.
                SoundManager.Instance.CashCoinGet();

                int itemCoin = RewardAnimationManager.Instance.GetOneValue(subCoin);

                int counting = 1;

                RewardAnimationManager.Instance.StopAll();

                SoundManager.Instance.PlayEffect(SoundNames.COIN_EARN);

                RewardAnimationManager.Instance.Play(text_Coin.GetComponent<RectTransform>(), subCoin, ResourceNames.ATLAS_ITEM_ICON, ItemNames.COIN,
                    objectComplete: () =>
                    {
                        text_Coin.text = CustomCalculator.TFCoinString(cashCoin + (itemCoin * (counting++)));
                        SoundManager.Instance.PlayCoinGet();
                    },
                    onComplete: () =>
                    {
                        text_Coin.text = CustomCalculator.TFCoinString(currentCoin);
                        
                        // 코인 획득 정보 표시 코루틴 실행
                        if (_coinSummaryCoroutine != null)
                        {
                            StopCoroutine(_coinSummaryCoroutine);
                        }
                        _coinSummaryCoroutine = StartCoroutine(Co_ShowCoinGainSummary(cashCoin, subCoin));
                    });
            }

        }
    }

    /// <summary>
    /// 코인 획득 정보를 잠시 보여주고 페이드아웃 시키는 코루틴
    /// </summary>
    private System.Collections.IEnumerator Co_ShowCoinGainSummary(int previousCoin, int addedCoin)
    {
        if (text_AddCoinInfo == null) yield break;

        // 초기화
        text_AddCoinInfo.gameObject.SetActive(true);
        Color originalColor = text_AddCoinInfo.color;
        originalColor.a = 1f;
        text_AddCoinInfo.color = originalColor;
        text_AddCoinInfo.text = $"{CustomCalculator.TFCoinString(previousCoin)}<color=red>+{CustomCalculator.TFCoinString(addedCoin)}</color>";

        // N초 대기
        yield return new WaitForSeconds(coinInfoDisplayDuration);

        // 페이드아웃
        float fadeDuration = 0.5f;
        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, timer / fadeDuration);
            text_AddCoinInfo.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
            yield return null;
        }

        text_AddCoinInfo.gameObject.SetActive(false);
        _coinSummaryCoroutine = null;
    }

    public void UpdateItem()
    {

    }

    public void UpdateTimer()
    {

    }

    public void UpdateStamina()
    {

    }

}
