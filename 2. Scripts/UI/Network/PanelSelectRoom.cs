using Custom;
using Fusion;
using System.Diagnostics;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Network
{
    /// <summary>
    /// 선택된 룸에 대한 시작을 담당합니다.
    /// 네트워크를 통한 게임 생성이 기반됩니다.
    /// </summary>
    public class PanelSelectRoom : MonoBehaviour
    {
        //네트워크 동작을 위한 프리팹 객체
        [SerializeField] private NetworkRunner _networkRunnerPrefab = null;

        //닉네임
        [SerializeField] private TextMeshProUGUI _nickName = null;

        // The Placeholder Text is not accessible through the TMP_InputField component so need a direct reference
        [SerializeField] private TextMeshProUGUI _nickNamePlaceholder = null;

        [SerializeField] private TMP_InputField _roomName = null;

        //게임 씬 연동을 위한 경로
        [SerializeField] private string _gameScenePath = null;

        //실제로 생성된 네트워크 러너
        private NetworkRunner _runnerInstance = null;

        // 새로운 게임 세션의 접속을 시도한다.
        public void StartShared()
        {
            SetPlayerData();
            StartGame(GameMode.Shared, _roomName.text, _gameScenePath);
        }

        //플레이어 데이터 지정
        private void SetPlayerData()
        {
            //실제 닉네임 필드가 비어있다면, PlaceHolder에 있는 닉네임이 지정된다.
            if (string.IsNullOrWhiteSpace(_nickName.text))
            {
                PlayerLocalData.NickName = _nickNamePlaceholder.text;
            }
            else
            {
                PlayerLocalData.NickName = _nickName.text;
            }
        }

        //async 예약어는 항상 Promise를 반환한다. // 대기 명령 같은 것
        private async void StartGame(GameMode mode, string roomName, string sceneName)
        {
            _runnerInstance = FindObjectOfType<NetworkRunner>();
            if (_runnerInstance == null) // 자동으로 Dontdestroy로 지정된다.
            {
                _runnerInstance = Instantiate(_networkRunnerPrefab);
            }

            // 퓨전 러너에게 사용자 입력을 제공할 것인지 알림
            _runnerInstance.ProvideInput = true;

            StartGameArgs startGameArgs = new StartGameArgs()
            {
                //현재 게임 모드
                GameMode = mode,

                //방 이름으로 찾기
                SessionName = roomName,

                PlayerCount = 2,

                //경로로부터 씬 인덱스를 가져와 활성화하기
                Scene = SceneRef.FromIndex(SceneUtility.GetBuildIndexByScenePath(_gameScenePath)),

                //게임 시작 시 네트워크 오브젝트 풀 사용
                //ObjectProvider = _runnerInstance.GetComponent<NetworkObjectPoolDefault>(),
            };

            // GameMode.Host = Start a session with a specific name
            // GameMode.Client = Join a session with a specific name

            //게임 시작을 위한 설정을 진행한다.
            //룸에 연결
            var result = await _runnerInstance.StartGame(startGameArgs);

            CustomDebug.Print($"_runnerInstance.IsServer :: {_runnerInstance.IsServer}");

            ////서버[클라우드]인 경우
            //if (_runnerInstance.IsServer)
            //{
            //    CustomDebug.Print("서버 연결 성공 접속 시도");

            //    //서버에서 씬을 호출하도록 합니다.
            //    await _runnerInstance.LoadScene(sceneName);
            //}
        }

        private void OnEnable()
        {
            // _nickNamePlaceholder.text = PlayerLocalData.NickName;
        }
        private void OnDisable()
        {
            _roomName.text = "";
        }
    }
}