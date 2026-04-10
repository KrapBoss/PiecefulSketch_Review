using Custom;
using Fusion;
using UnityEngine;

//플레이어 캐릭터를 스폰합니다.
public class PlayerChracterSpawner : NetworkBehaviour
{
    [Networked] private bool gameIsReady { get; set; }

    [SerializeField] NetworkPrefabRef character;

    // 각 로컬에서 플레이어의 스폰된 상태를 나타냄.
    bool spawned;

    //각 로컬에서 호출합니다.
    public override void Spawned()
    {
        //게임 준비가 되어있으면 캐릭터를 바로 생성합니다.
        if (gameIsReady)
        {
            CreateCharacter();
        }
    }

    //네트워크 연결에 따른 준비 상태를 전달받는다.
    public void ReadyGame()
    {
        gameIsReady = true;
        RPCCreateChracter();
    }


    //Host의 생성용
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPCCreateChracter()
    {
        CreateCharacter();
    }

    public void CreateCharacter()
    {
        if (spawned) return;

        CustomDebug.Print("플레이어 캐릭터 생성");
        NetworkObject go = Runner.Spawn(character, Vector3.zero, Quaternion.identity);
        //해당 객체의 권한을 플레이어에게 부여하기 위한 것
        Runner.SetPlayerObject(Runner.LocalPlayer, go);

        spawned = true;
    }
}
