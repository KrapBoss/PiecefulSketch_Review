using Custom;
using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public enum LocalGameState
{
    None ,Starting, Playing, Pause, End
}

/// <summary>
/// 각 클라이언트에서 실행되는 게임 순서별 진행되는 이벤트에 관한 저장이다
/// </summary>
public class StateManager : MonoBehaviour
{
    static StateManager instance;

    public static StateManager Instance
    {
        get
        {
            if (instance == null)
                instance = FindObjectOfType<StateManager>();
            return instance;
        }
    }

    public LocalGameState State { get; private set; }

    //해당 상태로 변경될 때에 호출되어야 되는 이벤트
    Dictionary<LocalGameState, Action> GameStateObservor = new Dictionary<LocalGameState, Action>();


    public void GameStateChage(LocalGameState state)
    {
        if (State == state) return;

        switch (state)
        {
            case LocalGameState.Starting:
                Debug.Log("게임이 시작 상태로 변경");
                PlayEvent(state);
                break;
            case LocalGameState.Playing:
                Debug.Log("게임이 진행으로 변경.");
                PlayEvent(state);
                break;
            case LocalGameState.Pause:
                Debug.Log("게임이 정지상태로 변경");
                PlayEvent(state);
                break;
            case LocalGameState.End:
                Debug.Log("게임이 종료 상태");
                PlayEvent(state);
                break;
        }
        
        State = state;
    }

    //각 상태에 따른 이벤트 함수를 저장하는 것
    public void InsertGameStateAction(LocalGameState state, Action action)
    {
        if (!GameStateObservor.ContainsKey(state)) GameStateObservor[state] = action;
        else GameStateObservor[state] += action;
        CustomDebug.Print($"[StateManager] 이벤트 등록 {action.Target.ToString()}");
    }

    public void DeleteGameStateAction(LocalGameState state, Action action)
    {
        if (GameStateObservor.ContainsKey(state))
        {
            GameStateObservor[state] -= action;
        }
    }

    //지정된 이벤트를 실행시킵니다.
    void PlayEvent(LocalGameState state)
    {
        if (GameStateObservor.ContainsKey(state))
        {
            GameStateObservor[state].Invoke();
        }
        else
        {
            CustomDebug.PrintE($"{state} 키가 없습니다.");
        }
    }

    private void OnDestroy()
    {
        instance = null;
    }

    public void Clear()
    {
        GameStateObservor.Clear();
    }
}
