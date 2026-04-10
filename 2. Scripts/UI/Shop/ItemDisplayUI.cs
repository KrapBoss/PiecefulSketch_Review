using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Threading;
using System;
using Custom;

/// <summary>
/// 상품에 포함된 단일 아이템의 UI를 표시하는 클래스입니다.
/// 비동기 아이콘 로딩 및 취소, 리소스 해제를 지원합니다.
/// </summary>
public class ItemDisplayUI : MonoBehaviour
{
    [SerializeField] private Image itemImage; // 아이템 이미지
    [SerializeField] private TextMeshProUGUI itemNameText; // 아이템 이름
    [SerializeField] private TextMeshProUGUI itemCountText; // 아이템 개수

    private CancellationTokenSource _cancellationTokenSource;
    private string _loadedAtlasName; // 리소스 해제를 위해 로드한 아틀라스 이름을 저장

    private void OnDestroy()
    {
        // 컴포넌트 파괴 시 진행 중인 비동기 작업을 취소합니다.
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource = null;

        // 로드했던 아틀라스 리소스를 해제합니다.
        if (!string.IsNullOrEmpty(_loadedAtlasName))
        {
            ResourceManager.Instance.ReleaseAsset(_loadedAtlasName);
            _loadedAtlasName = null;
        }
    }

    /// <summary>
    /// 아이템 정보를 기반으로 UI를 설정합니다. 아이콘은 비동기로 로드됩니다.
    /// </summary>
    /// <param name="spriteName">아이템 스프라이트 이름</param>
    /// <param name="count">아이템 개수</param>
    public void SetItem(string spriteName, int count)
    {
        // 기존에 실행 중이던 로딩 작업이 있다면 취소합니다.
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource = new CancellationTokenSource();

        // 텍스트 정보 우선 설정
        if (itemNameText != null)
        {
            // TODO: Localization.Localize(spriteName) 와 같은 지역화 처리 필요
            itemNameText.text = Localization.Localize(spriteName);
        }
        if (itemCountText != null)
        {
            itemCountText.text = $"x{CustomCalculator.TFCoinString(count)}";
        }
        
        // 아이콘을 비동기로 로드합니다.
        // 아이콘 로딩 전, 기본 이미지를 null로 설정하여 이전 이미지가 남지 않도록 합니다.
        if(itemImage != null)
        {
            itemImage.sprite = null;
            LoadIconAsync(spriteName, _cancellationTokenSource.Token);
        }
    }

    /// <summary>
    /// 스프라이트를 비동기로 로드하고 이미지에 적용합니다.
    /// </summary>
    private async void LoadIconAsync(string spriteName, CancellationToken token)
    {
        try
        {
            // 로드했던 아틀라스 리소스를 해제합니다.
            if (!string.IsNullOrEmpty(_loadedAtlasName))
            {
                ResourceManager.Instance.ReleaseAsset(_loadedAtlasName);
                _loadedAtlasName = null;
            }

            // TODO: 아틀라스 이름을 외부에서 받아오거나, 데이터에 포함시키는 것이 좋습니다.
            // ResourceNames.ATLAS_ITEM_ICON 와 같은 형태로 정의된 상수를 사용합니다.
            _loadedAtlasName = ResourceNames.ATLAS_ITEM_ICON;

            Sprite sprite = await ResourceManager.Instance.LoadSpriteFromAtlas(_loadedAtlasName, spriteName, true);

            // 작업이 취소되었는지 확인합니다.
            if (token.IsCancellationRequested)
            {
                // 로드가 완료되었지만 CancellationToken에 의해 취소된 경우,
                // 참조 카운트가 증가했으므로 여기서 한 번 해제해줘야 합니다.
                ResourceManager.Instance.ReleaseAsset(_loadedAtlasName);
                return;
            }

            // 스프라이트 적용
            if (itemImage != null)
            {
                itemImage.sprite = sprite;
            }
        }
        catch (Exception ex)
        {
            // 로딩 중 에러 발생 (e.g., 스프라이트를 찾을 수 없음)
            Debug.LogWarning($"Icon loading failed for {spriteName}: {ex.Message}");
            if (itemImage != null)
            {
                itemImage.sprite = null; // 기본 이미지나 빈 이미지로 설정
            }
        }
    }
}
