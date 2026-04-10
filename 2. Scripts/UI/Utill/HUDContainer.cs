using UnityEngine;

/// <summary>
/// 즉각적인 변경이 필요한 UI에 대한 형을 지정
/// 관련 UI에서 상속 받아 사용합니다.
/// </summary>
public interface HUDContainer
{
    public static HUDContainer Instance;

    /// <summary> 코인 정보를 업데이트 합니다. </summary>
    public abstract void UpdateCoin();

    /// <summary> 아이템 개수 정보 업데이트 </summary>
    public abstract void UpdateItem();

    /// <summary> 상단 정보 표기 업데이트 </summary>
    public abstract void UpdateTimer();

    /// <summary> 스테미나 정보 업데이트 </summary>
    public abstract void UpdateStamina();

    public abstract void OnDestroyMethod();

}
