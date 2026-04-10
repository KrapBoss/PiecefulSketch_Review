using Fusion;
using Network;
using UnityEngine;
using System;
using Custom;

/// <summary>
/// 네트워크상 공유되는 플레이어 정보를 가지고 있습니다.
/// </summary>
public class PlayerDataNetwork : NetworkBehaviour
{
    [Networked]
    public NetworkString<_16>NickName { get; private set; }

    [Networked]
    public float Perfection { get; private set; }

    [Networked]
    public int Combo { get; private set; }

    [Networked]
    public string PlayerIcon {  get; private set; }

    //캐릭터 아이콘
    public SpriteRenderer spriteRenderer;

    NetworkGameController _NGC;

    StatePanel _statePanel;

    public override void Spawned()
    {
        //해당 캐릭터에 대한 권한 판단
        if (HasStateAuthority)
        {
            _NGC = NetworkGameController.Instance;

            spriteRenderer.enabled = false;

            CustomDebug.PrintE("플레이어 캐릭터 생성됨으로 인한 설정 시작");
            NickName = PlayerLocalData.NickName;
            Perfection = 0;
            Combo = 0;

            GameObject puzzle = Resources.Load<GameObject>(_NGC.GetPuzzlePath());
            CustomDebug.PrintW($"{_NGC.GetPuzzlePath()}");
            if (puzzle == null) 
                throw new Exception("플레이어 데이터 : 퍼즐 불러오기 실패");

            var _spawnedPuzzle=  Runner.Spawn(puzzle);

            PuzzleSet(_spawnedPuzzle);

            if (_spawnedPuzzle == null) throw new Exception("플레이어 데이터 : 퍼즐 생성 실패");
        }


        //All Client
        CustomDebug.PrintE($"플레이어 스폰 {NickName}");

        //모든 클라이언트에서 생성 시 상태바 업데이트를 위한 등록
        _statePanel = FindObjectOfType<StatePanel>();
        _statePanel.RegisterPlayer(this);

        NetworkGameController.Instance.RegistPlayer(this);
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        NetworkGameController.Instance.RemovePlayer(this);
        FindObjectOfType<StatePanel>()?.RemovePlayer(this);
    }


    //2명의 플레이어만 존재하기 때문에 상하로 위치시킨다.
     Vector2 GetPuzzleSpawnPoint(PuzzleContainer container, int _index)
    {
        Vector2 point = Vector2.zero;
        int multiple = (_index == 0) ? -1 : 1;
        point.y += container.BackGroundSprite.bounds.size.y * multiple;
        point.y += container.BackGroundSprite.bounds.size.y*0.2f* multiple; //오프셋
        return point;
    }

    //퍼즐 위치 지정
    void PuzzleSet(NetworkObject puzzle)
    {
        //퍼즐 초기화
        PuzzleContainer container = puzzle.GetComponent<PuzzleContainer>();
        container.Initialize();

        //이벤트 등록
        container.Action_Fit += ChangeData;

        //자신이 생성한 퍼즐을 등록한다.
        CameraController.Instance.Container = container;

        //현재 게임 컨트롤러로 부터 퍼즐을 생성한다.
        int index = Runner.LocalPlayer.PlayerId % 2;    //위치를 잡기 위한 것
        //CustomDebug.PrintE($"플레이어 네트워크 번호 {Runner.LocalPlayer.PlayerId}");

        container.transform.position = GetPuzzleSpawnPoint(container, index);
        //CustomDebug.PrintE($"{GetPuzzleSpawnPoint(container, index)}");
    }

    //데이터 변경 시 패널 업데이트를 위한 작업
    public void ChangeData(float perfection)
    {
        RpcChangeData(perfection);
    }

    //해당 오브젝트에 대한 권한을 가진 플레이어가 호출하고, 모든 클라이언트에 반영한다.
    //당연히 상태 패널에는 등록을 해야 된다.
    [Rpc(sources: RpcSources.StateAuthority, targets: RpcTargets.All)]
    private void RpcChangeData(float perfection)
    {
        if (_statePanel == null) return;

        //맞춘 정도를 변경한다.
        Perfection = perfection;

        _statePanel.UpdateState(this);
    }
}
