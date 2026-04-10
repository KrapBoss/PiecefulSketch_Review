using Custom;
using Fusion;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using Random = UnityEngine.Random;

namespace Network
{
    //네트워크 내에 게임의 전체적인 상태 관리
    public class NetworkGameController : NetworkBehaviour
    {
        enum GameState // 각 게임 상태
        {
            Starting, Running, Ending
        }

        public static NetworkGameController Instance { get; private set; }

        List<PlayerDataNetwork> _players = new List<PlayerDataNetwork>();

        [Networked]
        NetworkString<_32> PuzzleName { get; set; } //생성할 퍼즐 이름

        [Networked]
        GameState State { get; set; }   //현재 게임의 상태 판단

        [Networked]
        TickTimer tickTimer { get; set; }   // 네트워크 시간 개념

        TimerUI timerUI;
        [Networked] bool delay { get; set; } // 시작 시 딜레이 시작을 알림

        //기본 네트워크 프리팹 경로
        string PrefabPath = "Network/Puzzle";

        private void Awake()
        {
            CustomDebug.PrintW("NetworkGameController 생성");

            Instance = this;

            //해당 오브젝트의 소유는 마스터 클라이언트임을 나타냄.
            GetComponent<NetworkObject>().Flags |= NetworkObjectFlags.MasterClientObject;
        }

        private void OnDestroy()
        {
            if (Instance == this) { Instance = null; }
            else { throw new InvalidOperationException(); }
        }

        public override void Spawned()
        {
            base.Spawned();

            //마스터 클라이언트의 퍼즐 랜덤 지정 생성
            if (HasStateAuthority)
            {
                CustomDebug.PrintW("랜덤한 퍼즐을 지정합니다.");

                State = GameState.Starting;

                PuzzleName = GetRandomPuzzleName();
                Resources.UnloadUnusedAssets();
                CustomDebug.PrintW($"{PuzzleName}");

                //플레이어 생성
                FindObjectOfType<PlayerChracterSpawner>().ReadyGame();

                tickTimer = TickTimer.CreateFromSeconds(Runner, 60 * 3);
            }

            timerUI = FindObjectOfType<TimerUI>();

            TopCanvasInMulti.Instance.Blind.gameObject.SetActive(true);

            FindObjectOfType<TitleUI>().SetTitle($"▶{PuzzleName}◀");
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            CustomDebug.PrintE("디스폰으로 인한 서버 종료");
            ShutDown();
        }

        //네트워크에서 주기적으로 그려지는 명령
        public override void Render()
        {
            //각 단계에 따른 게임 진행 상태 업데이트
            switch (State)
            {
                case GameState.Starting:
                    StartingGame();
                    break;
                case GameState.Running:
                    RunningGame();
                    break;
                case GameState.Ending:
                    EndingGame();
                    break;

            }
        }

        void StartingGame()
        {

            //-----------------Master----------------
            if (HasStateAuthority)
            {
                //게임 시작 대기 시간 부여
                if (_players.Count >= 2 && !delay)
                {
                    tickTimer = TickTimer.CreateFromSeconds(Runner, 3);
                    delay = true;
                }

                //대기시작 종료 시 게임을 시작합니다.
                if (tickTimer.Expired(Runner))
                {
                    //다음 단계로 진입
                    State = GameState.Running;
                    tickTimer = TickTimer.CreateFromSeconds(Runner, 60 * 4);
                }
            }


            //-----------------------Local---------------

            CustomDebug.PrintE("네트워크 게임 Starting.");

            float remain = tickTimer.RemainingTime(Runner) ?? 0;
            timerUI.SetTime(remain);

            if (delay)
            {
                TopCanvasInMulti.Instance.Blind.SetText($"Ready! {remain: 0.#}");

                //시작 시 초기화 설정
                if(remain <= 1.0f)
                {
                    //로컬에서 각 상태에 대해 정의된 
                    StateManager.Instance.GameStateChage(LocalGameState.Playing);
                }
            }
        }

        void RunningGame()
        {
            CustomDebug.PrintE("네트워크 게임 Running.");

            //-----------------------Local---------------

            TopCanvasInMulti.Instance.Blind.gameObject.SetActive(false);

            float remain = tickTimer.RemainingTime(Runner) ?? 0;
            timerUI.SetTime(remain);


            //-----------------Master----------------

            if (HasStateAuthority)
            {
                //게임 종료 조건 판단
                CheckEnd();
            }
        }

        bool CheckEnd()
        {
            if (GameHasEnd())
            {
                tickTimer = TickTimer.CreateFromSeconds(Runner, 15.0f);
                return true;
            }

            return false;
        }

        bool GameHasEnd()
        {
            //-----------------------Local---------------

            //조건 1 : 시간 초과
            if (tickTimer.Expired(Runner))
            {
                State = GameState.Ending;
                return true;
            }

            //조건 2 : 퍼즐의 완성
            foreach (var player in _players)
            {
                if (player.Perfection == 1.0f)
                {
                    State = GameState.Ending;
                    return true;
                }
            }

            //조건 3 : 플레이어의 떠남
            if (_players.Count < 2)
            {
                State = GameState.Ending;
                return true;
            }

            return false;
        }

        void EndingGame()
        {
            CustomDebug.PrintE("네트워크 게임을 종료합니다.");
            TopCanvasInMulti.Instance.Result.ShowResult(_players.ToArray());
            TopCanvasInMulti.Instance.Blind.gameObject.SetActive(false);

            //제한 시간 종료 후 게임 종료
            if (tickTimer.Expired(Runner))
            {
                ShutDown();
            }
        }

        

        public void RegistPlayer(PlayerDataNetwork player)
        {
            //게임이 끝남
            if (State == GameState.Ending) return;

            if (!_players.Contains(player)) _players.Add(player);
            CustomDebug.Print("플레이어가 추가되었습니다.");
        }
        public void RemovePlayer(PlayerDataNetwork player)
        {
            if (_players.Contains(player)) _players.Remove(player);
            CustomDebug.Print("플레이어가 제거되었습니다.");
        }

        //현재 네트워크 퍼즐 중 랜던한 한개를 가져옵니다.
        string GetRandomPuzzleName()
        {
            // Resources 폴더 내의 특정 경로에서 모든 GameObject를 로드
            var objects = Resources.LoadAll<GameObject>(PrefabPath);
            //foreach (GameObject go in objects) CustomDebug.PrintW(go.name);

            // 각 GameObject의 이름을 배열로 반환
            string[] puzzleNames = objects.Select(obj => obj.name).ToArray();
            foreach (string go in puzzleNames) CustomDebug.PrintW(go);

            if (puzzleNames.Length < 1) throw new Exception("리소스에서 네트워크 오브젝트를 찾을 수 없습니다.");

            return puzzleNames[Random.Range(0, puzzleNames.Length)];
        }

        public string GetPuzzlePath()
        {
            return $"{PrefabPath}/{PuzzleName}";
        }

        //서버 종료
        void ShutDown()
        {
            CustomDebug.PrintE("NetworkGAmeController :: ShutDown");
            if (Runner.IsRunning)
            {
                Runner.Shutdown();
            }
        }
    }
}
