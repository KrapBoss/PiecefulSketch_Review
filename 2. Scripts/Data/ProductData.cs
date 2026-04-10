using Custom; // SecureInt가 포함된 네임스페이스
using Managers;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 상품 구매 시 지급되는 보상의 타입을 정의합니다.
/// </summary>
public enum GrantedRewardType
{
    Coin,           // 게임 내 재화 (코인)
    Stamina,        // 에너지
    RemoveAds       // 광고 제거 기능
}

/// <summary>
/// 지급되는 단일 보상 항목의 정보를 정의합니다.
/// </summary>
public class GrantedItem
{
    public string ItemName { get; private set; }
    public SecureInt Amount { get; private set; } = new SecureInt(0);

    // 타입별로 생성자를 분리하여 편의성 및 안정성 확보
    private GrantedItem(string itemName, int amount)
    {
        ItemName = itemName;
        Amount = new SecureInt(amount);
    }

    public static GrantedItem Coin(int amount) => new GrantedItem(ItemNames.COIN, amount);
    public static GrantedItem Stamina(int amount) => new GrantedItem(ItemNames.STAIMINA, amount);
    public static GrantedItem AdsRemoval() => new GrantedItem(ItemNames.AD_BLOCK, 1);
}


/// <summary>
/// 단일 인게임 상품의 모든 정보를 담는 데이터 클래스입니다.
/// </summary>
public class ProductInfo
{
    public string ProductId { get; private set; }
    public string NameLocalizationKey { get; private set; }
    public string DescLocalizationKey { get; private set; }
    public SecureInt Price { get; private set; }
    public List<GrantedItem> Rewards { get; private set; }

    public ProductInfo(string productId, string nameKey, string descKey, int price, List<GrantedItem> rewards)
    {
        ProductId = productId;
        NameLocalizationKey = nameKey;
        DescLocalizationKey = descKey;
        Price = new SecureInt(price);
        Rewards = rewards ?? new List<GrantedItem>();
    }
}

/// <summary>
/// 인게임 상품(IAP) 정보를 하드코딩하여 관리하는 정적 클래스입니다.
/// ProductSO 대신 사용됩니다.
/// </summary>
public class ProductData
{
    private static readonly Dictionary<string, ProductInfo> _products;

    /// <summary>
    /// 클래스가 처음 로드될 때 상품 목록을 초기화합니다.
    /// </summary>
    static ProductData()
    {
        _products = new Dictionary<string, ProductInfo>();

        // --- 여기에 하드코딩할 상품들을 추가합니다 ---

        // 광고 제거 상품 추가
        AddProduct(new ProductInfo(
            productId: IAPNames.REMOVE_ADS,
            nameKey: "shop_remove_ads_name", // "광고 제거"
            descKey: "AdsBlockDescText", // "모든 강제 삽입 광고를 영구적으로 제거합니다."
            price: 5000,
            rewards: new List<GrantedItem> { GrantedItem.AdsRemoval(), GrantedItem.Coin(1000) }
        ));

        // 코인 상품 500
        AddProduct(new ProductInfo(
            productId: IAPNames.COIN_500,
            nameKey: "shop_coin_500_name",
            descKey: "shop_coin_500_desc",
            price: 999999,
            rewards: new List<GrantedItem> { GrantedItem.Coin(500) }
        ));
        
        // 코인 상품 1750
        AddProduct(new ProductInfo(
            productId: IAPNames.COIN_1750,
            nameKey: "shop_coin_1750_name",
            descKey: "shop_coin_1750_desc",
            price: 999999,
            rewards: new List<GrantedItem> { GrantedItem.Coin(1750) }
        ));

        // 코인 상품 3150
        AddProduct(new ProductInfo(
            productId: IAPNames.COIN_3150,
            nameKey: "shop_coin_3150_name",
            descKey: "shop_coin_3150_desc",
            price: 999999,
            rewards: new List<GrantedItem> { GrantedItem.Coin(3150) }
        ));

        // 코인 상품 6750
        AddProduct(new ProductInfo(
            productId: IAPNames.COIN_6750,
            nameKey: "shop_coin_6750_name",
            descKey: "shop_coin_6750_desc",
            price: 999999,
            rewards: new List<GrantedItem> { GrantedItem.Coin(6750) }
        ));

        // 코인 상품 10800
        AddProduct(new ProductInfo(
            productId: IAPNames.COIN_10800,
            nameKey: "shop_coin_10800_name",
            descKey: "shop_coin_10800_desc",
            price: 999999,
            rewards: new List<GrantedItem> { GrantedItem.Coin(10800) }
        ));

        // 코인 상품 15000
        AddProduct(new ProductInfo(
            productId: IAPNames.COIN_15000,
            nameKey: "shop_coin_15000_name",
            descKey: "shop_coin_15000_desc",
            price: 999999,
            rewards: new List<GrantedItem> { GrantedItem.Coin(15000) }
        ));

        // - 스테미나 1
        AddProduct(new ProductInfo(
            productId: "Reward Stamina1",
            nameKey: "Reward Stamina1_name",
            descKey: "Reward Stamina1_desc",
            price: 200,     // 코인 결제 금액
            rewards: new List<GrantedItem> { GrantedItem.Stamina(10) }
        ));

        // - 스테미나 5
        AddProduct(new ProductInfo(
            productId: "Reward Stamina5",
            nameKey: "Reward Stamina5_name",
            descKey: "Reward Stamina5_desc",
            price: 500,     // 코인 결제 금액
            rewards: new List<GrantedItem> { GrantedItem.Stamina(30) }
        ));

        // 무료 광고 상품 - 코인
        AddProduct(new ProductInfo(
            productId: "Free Reward Coin",
            nameKey: "Free Reward Coin_name",
            descKey: "AdsDescText",
            price: 0,
            rewards: new List<GrantedItem> { GrantedItem.Coin(50) }
        ));

        // 무료 광고 상품 - 스테미나
        AddProduct(new ProductInfo(
            productId: "Free Reward Stamina",
            nameKey: "Free Reward Stamina_name",
            descKey: "Free Reward Stamina_desc",
            price: 0,
            rewards: new List<GrantedItem> { GrantedItem.Stamina(10) }
        ));
        
    }

    /// <summary>
    /// 상품 목록에 상품을 추가합니다.
    /// </summary>
    private static void AddProduct(ProductInfo productInfo)
    {
        if (productInfo == null || string.IsNullOrEmpty(productInfo.ProductId)) return;
        if (_products.ContainsKey(productInfo.ProductId)) return;
        
        _products.Add(productInfo.ProductId, productInfo);
    }

    /// <summary>
    /// ProductId를 사용하여 특정 상품 정보를 가져옵니다.
    /// </summary>
    /// <param name="productId">가져올 상품의 고유 ID</param>
    /// <returns>상품 정보 클래스. 없으면 null을 반환합니다.</returns>
    public static ProductInfo GetProductInfo(string productId)
    {
        if(_products.TryGetValue(productId, out var productInfo))
        {
            return productInfo;
        }
        return null;
    }

    /// <summary>
    /// 하드코딩된 모든 상품의 목록을 가져옵니다.
    /// </summary>
    /// <returns>상품 ID를 키로 사용하는 상품 정보 딕셔너리</returns>
    public static IReadOnlyDictionary<string, ProductInfo> GetAllProducts()
    {
        return _products;
    }

}
