using UnityEngine;
using System.Text;
using System; // Convert 사용을 위해 추가

/// <summary>
/// 데이터를 XOR 연산 후 Base64로 변환하여 저장하는 보안 유틸리티
/// </summary>
public static class CryptoUtil
{
    private static readonly string Key = "Pieceful_Sketch_Key_2026";

    /// <summary>
    /// 평문 -> 암호화 -> Base64 (저장용)
    /// </summary>
    public static string Encrypt(string data)
    {
        if (string.IsNullOrEmpty(data)) return "";

        byte[] bytes = Encoding.UTF8.GetBytes(data);
        byte[] keyBytes = Encoding.UTF8.GetBytes(Key);

        for (int i = 0; i < bytes.Length; i++)
        {
            bytes[i] = (byte)(bytes[i] ^ keyBytes[i % keyBytes.Length]);
        }

        // 널 문자(\0) 문제를 피하기 위해 Base64로 변환하여 반환
        return Convert.ToBase64String(bytes);
    }

    /// <summary>
    /// Base64 -> 복호화 -> 평문 (로드용)
    /// </summary>
    public static string Decrypt(string data)
    {
        if (string.IsNullOrEmpty(data)) return "";

        try
        {
            byte[] bytes = Convert.FromBase64String(data);
            byte[] keyBytes = Encoding.UTF8.GetBytes(Key);

            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = (byte)(bytes[i] ^ keyBytes[i % keyBytes.Length]);
            }

            return Encoding.UTF8.GetString(bytes);
        }
        catch
        {
            return ""; // 데이터 형식이 안 맞으면 빈 값 반환
        }
    }
}

[System.Serializable]
public class ProductItemData
{
    public string encryptedItemName;
    public string encryptedRewardAmount;
}

[CreateAssetMenu(fileName = "ProductList", menuName = "IAP/ProductItem")]
public class ProductSO : ScriptableObject
{
    public string encryptedProductId;
    public string encryptedDescription;
    public string encryptedPrice; // 가격 정보 추가
    public ProductItemData[] itemDatas;

    // 읽을 때는 Decrypt 사용
    public string GetProductId() => CryptoUtil.Decrypt(encryptedProductId);

    // 읽을 때는 Decrypt 사용
    public string GetDescription() => CryptoUtil.Decrypt(encryptedDescription);
    
    // 가격 정보 읽기
    public int GetPrice()
    {
        if (int.TryParse(CryptoUtil.Decrypt(encryptedPrice), out int price))
        {
            return price;
        }
        return 0;
    }

    public (string name, int amount) GetItemData(int index)
    {
        if (itemDatas == null || index >= itemDatas.Length) return ("", 0);
        string name = CryptoUtil.Decrypt(itemDatas[index].encryptedItemName);
        string amountStr = CryptoUtil.Decrypt(itemDatas[index].encryptedRewardAmount);
        int.TryParse(amountStr, out int amount);
        return (name, amount);
    }
}