using UnityEngine;

/// <summary>
/// 즉각적인 변경이 필요한 UI에 대한 형을 지정
/// 관련 UI에서 상속 받아 사용합니다.
/// </summary>
public interface HUDContainer_InGame
{
    public static HUDContainer_InGame Instance;

    /// <summary> 하단 바텀을 업데이트 합니다. </summary>
    public abstract void UpdatePieceInventory();
}
