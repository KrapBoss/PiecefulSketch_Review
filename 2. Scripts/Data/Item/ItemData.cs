using System.Collections.Generic;
using UnityEngine;

public enum ItemType
{
    HINT,
}


/// <summary>
/// 아이템 정보 관리
/// </summary>
public class ItemData : MonoBehaviour
{
    private static ItemData m_instance;

    // 전역 인스턴스 접근 프로퍼티
    public static ItemData Instance
    {
        get
        {
            if (m_instance == null)
            {
                // 기존 인스턴스 확인
                m_instance = FindFirstObjectByType<ItemData>();

                // 없을 경우 새로운 게임 오브젝트 생성 후 컴포넌트 추가
                if (m_instance == null)
                {
                    GameObject go = new GameObject("ItemData_Global");
                    m_instance = go.AddComponent<ItemData>();
                }
            }
            return m_instance;
        }
    }

    private void OnDestroy()
    {
        ResetAllItems();
        m_instance = null;
    }

    // 아이템 타입별 수량 저장 (보안 변수 적용)
    private Dictionary<ItemType, SecureInt> m_itemInventory = new Dictionary<ItemType, SecureInt>();

    /// <summary>
    /// 특정 타입의 아이템 개수를 N개만큼 증가
    /// </summary>
    public void AddItemCount(ItemType type, int amount)
    {
        if (amount <= 0) return;

        SecureInt sInt = GetOrCreateSecureInt(type);
        sInt.Value += amount;
        m_itemInventory[type] = sInt;

        if (HUDContainer.Instance != null) HUDContainer.Instance.UpdateItem();

        Debug.Log($"[ItemData] {type} 증가: +{amount} (현재: {m_itemInventory[type].Value})");
    }

    /// <summary>
    /// 특정 타입의 아이템 개수를 감소 (부족할 경우 false 반환)
    /// </summary>
    public bool TrySubtractItemCount(ItemType type, int amount)
    {
        if (amount <= 0) return false;

        SecureInt sInt = GetOrCreateSecureInt(type);

        if (sInt.Value < amount)
        {
            Debug.LogWarning($"[ItemData] {type} 개수 부족!");
            return false;
        }

        sInt.Value -= amount;
        m_itemInventory[type] = sInt;

        if (HUDContainer.Instance != null) HUDContainer.Instance.UpdateItem();

        Debug.Log($"[ItemData] {type} 감소: -{amount} (현재: {m_itemInventory[type].Value})");
        return true;
    }

    /// <summary>
    /// 현재 아이템 보유량 확인
    /// </summary>
    public int GetItemCount(ItemType type)
    {
        return GetOrCreateSecureInt(type).Value;
    }

    /// <summary>
    /// 모든 아이템의 개수를 0으로 초기화
    /// </summary>
    public void ResetAllItems()
    {
        m_itemInventory.Clear();
        Debug.Log("[ItemData] 모든 아이템 초기화 완료");
    }

    /// <summary>
    /// 딕셔너리에서 SecureInt를 가져오거나 없으면 생성
    /// </summary>
    private SecureInt GetOrCreateSecureInt(ItemType type)
    {
        if (!m_itemInventory.ContainsKey(type))
        {
            m_itemInventory[type] = new SecureInt(0);
        }
        return m_itemInventory[type];
    }

    /// <summary>
    /// 아이템 가격을 반환합니다.
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static int GetItemBuyPrice(ItemType type)
    {
        return type switch
        {
            ItemType.HINT => 150,
            _ => 1000
        };
    }

    /// <summary>
    /// 아이템 구매 시 아이템 구매 개수를 반환합니다.
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static int GetItemBuyCount(ItemType type)
    {
        return type switch
        {
            ItemType.HINT => 3,
            _ => 0
        };
    }
}
