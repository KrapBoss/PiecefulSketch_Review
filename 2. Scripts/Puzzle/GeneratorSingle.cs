using Custom;
using Global;
using Managers;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;

/// <summary>
/// 싱글플레이 퍼즐 생성 및 게임오버 로직을 담당합니다.
/// </summary>
public class GeneratorSingle : MonoBehaviour
{
    // From SinglePuzzleLoader
    PuzzleSO _so;

    private void Awake()
    {
        // From SingleGameEnd
        CustomDebug.Print($"GeneratorSingle :: Name {transform.name}");
        StateManager.Instance.InsertGameStateAction(LocalGameState.Starting, GameStateStart);
        StateManager.Instance.InsertGameStateAction(LocalGameState.Playing, GameStatePlaying);
        StateManager.Instance.InsertGameStateAction(LocalGameState.End, GameStateEnd);
        // From SinglePuzzleLoader
        StateManager.Instance.InsertGameStateAction(LocalGameState.Starting, GameStart);
    }

    private async void Start()
    {
        // From SinglePuzzleLoader
        await StartInit();  // 퍼즐 로더

        StateManager.Instance.GameStateChage(LocalGameState.Starting);
        StateManager.Instance.GameStateChage(LocalGameState.Playing);
    }

    private void OnDestroy()
    {
        if (StateManager.Instance != null)
        {
            // From SinglePuzzleLoader
            StateManager.Instance.DeleteGameStateAction(LocalGameState.Starting, GameStart);
            // From SingleGameEnd
            StateManager.Instance.DeleteGameStateAction(LocalGameState.Starting, GameStateStart);
            StateManager.Instance.DeleteGameStateAction(LocalGameState.End, GameStateEnd);
            StateManager.Instance.DeleteGameStateAction(LocalGameState.Playing, GameStatePlaying);
        }
        
        if (PuzzleContainer.Container != null)
        {
            PuzzleContainer.Container.Action_Fit -= StateToEnd;
        }
    }

    // --- Methods from SinglePuzzleLoader ---

    private async Task StartInit()
    {
        CustomDebug.PrintW("GeneratorSingle :: 퍼즐을 로드합니다");

        _so = SoManager.Instance.GetChoosePuzzle();
        GameObject puzzle = await ResourceManager.Instance.LoadAsset<GameObject>(_so.PuzzleName, true);

        var container = Instantiate(puzzle).GetComponent<PuzzleContainer>();
        container.Initialize();

        CustomDebug.PrintW("GeneratorSingle :: 퍼즐 로드완료. 게임을 시작합니다");
    }

    //게임 준비 완료되면 페이드 효과를 제거합니다.
    public void GameStart()
    {
#if UNITY_EDITOR
        // 기본 힌트 제공 개수
        ItemData.Instance.AddItemCount(ItemType.HINT, 999);
#endif

        //기존 코루틴 제거
        coroutine = null;

        TimerUI.IsStop = false;

        EventManager.Instance.SetNight();

        //Fade.FadeSetting(false, 1.0f, GameSetting.Night ? Color.black : Color.white);

        //if(_so.PuzzleBGM != null)
        //{
        //    //사운드 BGM 이름과 클립을 넘김.
        //    SoundManager.Instance.PlayBGM(_so.PuzzleBGM.name, _so.PuzzleBGM);
        //}
    }

    // --- Methods from SingleGameEnd ---

    //게임이 시작되면 조각 맞추기 액션을 연결
    public void GameStateStart()
    {
        PuzzleContainer.Container.Action_Fit += StateToEnd;
    }

    //게임이 시작되면 조각 맞추기 액션을 연결
    public void GameStatePlaying()
    {
        //EventManager.Instance.SetNight();
    }

    //게임이 끝나면 결과 화면을 불러옵니다.
    public async void GameStateEnd()
    {
        CustomDebug.Print($"GeneratorSingle :: Name {transform.name}");
        CustomDebug.Print("GeneratorSingle :: 게임이 끝나 결과창을 호출합니다.");

        // 타이머 정지
        TimerUI.IsStop = true;

        //결과 UI 표시
        await UIScreenManager.Instance.ShowUI(UIName.RESULT_UI);
    }


    Coroutine coroutine;
    //퍼즐을 다 맞추면 게임 상태를 변경합니다.
    public void StateToEnd(float perfection)
    {
        if (coroutine != null) return;
        if (perfection == 1)
        {
            SingleGameBaseUI screen = UIScreenManager.Instance.GetUI(UIName.SINGLE_BASE_UI) as SingleGameBaseUI;
            if (screen) screen.Block(false);

            coroutine = StartCoroutine(StateToEndCroutine());
        }
    }

    IEnumerator StateToEndCroutine()
    {
        SoundManager.Instance.PlayEffect(SoundNames.PUZZLE_SUCCESS);

        StateManager.Instance.GameStateChage(LocalGameState.Pause);

        if (CameraController.Instance != null)
        {
            CameraController.Instance.ResizeCamera(1.0f);
        }

        yield return new WaitForSeconds(1.5f);

        StateManager.Instance.GameStateChage(LocalGameState.End);
    }
}