using UnityEngine;

/// <summary>
/// 퍼즐 인벤토리 업데이트용 사이즈 조절기
/// </summary>
public class PuzzleInventoryUpdater : MonoBehaviour, HUDContainer_InGame
{
    [SerializeField] RectTransform inventory;
    float baseY;

    private void Awake()
    {
        HUDContainer_InGame.Instance = this;

        baseY = inventory.rect.height;
        HUDContainer_InGame.Instance?.UpdatePieceInventory();
    }

    private void OnDestroy()
    {
        HUDContainer_InGame.Instance = null;
    }

    public void UpdatePieceInventory()
    {
        float y = GameValueConfig.BottomOffest * GameSetting.PuzzleBottomOffeset;
        inventory.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, baseY + y);
    }
}
