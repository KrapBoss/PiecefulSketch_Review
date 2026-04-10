using Custom;
using UnityEngine;

/// <summary>
/// 네크워크 상 플레이어 의 정보를 표시해줍니다.
/// </summary>
public class StatePanel : MonoBehaviour
{
    [SerializeField] StatePenelEntity[] entities;

    private void Awake()
    {
        entities = GetComponentsInChildren<StatePenelEntity>();
    }

    //플레이어를 등록함
    public bool RegisterPlayer(PlayerDataNetwork player)
    {
        CustomDebug.PrintE($"플레이어 정보 등록 {player.NickName}");
        foreach (var entity in entities)
        {
            if (entity.Register(player))
            {
                return true;
            }
        }

        return false;
    }

    //플레이어 상태 변화에 따른 업데이트를 진행
    public void UpdateState(PlayerDataNetwork player)
    {
        foreach (var entity in entities)
        {
            if (entity.UpdateState(player)) break;
        }
    }

    //플레이어를 등록함
    public bool RemovePlayer(PlayerDataNetwork player)
    {
        CustomDebug.PrintE($"플레이어 정보 제거 {player.NickName}");
        foreach (var entity in entities)
        {
            if (entity.Remove(player))
            {
                return true;
            }
        }

        return false;
    }
}
