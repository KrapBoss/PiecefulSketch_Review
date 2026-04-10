using Global;
using Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UI.Core;
using Unity.Services.Authentication;
using UnityEngine;

/// <summary>
/// 게임 설정 UI의 동작을 관리합니다.
/// 사운드, 진동 등의 설정을 초기화하고, 사용자 입력을 처리하며, 변경 사항을 저장합니다.
/// </summary>
public class SettingUI : UIBase
{
    [Header("UI Components")]
    [SerializeField] private UISliderPack soundSlider; // 사운드용 슬라이더
    [SerializeField] private UISliderPack soundSlider_Effect; // 사운드용 슬라이더
    [SerializeField] private SliderOnOff vibration;     // 진동용 On/Off 슬라이더
    [SerializeField] private SliderOnOff timer;         // 타이머용 UI
    [SerializeField] private UISliderPack soundSlider_Offset; // 보관소 하단 오프셋
    [SerializeField] private TextMeshProUGUI userID;    // 유저 ID 표시용


    [Space, Header("Setting Value")]


    // UI가 열릴 때의 초기 설정 값을 저장하기 위한 변수
    private float _initialVolume;
    private float _initialVolume_Effect;
    private float _initialBottomOffset;
    private bool _initialVibrationState;
    private bool _initialTimer;

    bool first1 = false;
    bool first1_effect = false;
    bool first2 = false;

    /// <summary>
    /// UI가 비활성화될 때 호출됩니다.
    /// 이벤트 리스너를 해제합니다.
    /// </summary>
    private void OnDisable()
    {
        // 메모리 누수 방지를 위해 이벤트 리스너 해제
        if (soundSlider != null)
        {
            soundSlider.RemoveOnValueChangedListener(HandleSoundChange);
        }
        if (soundSlider_Effect != null)
        {
            soundSlider_Effect.RemoveOnValueChangedListener(HandleSoundChange_Effect);
        }
        if (vibration != null)
        {
            vibration.RemoveOnStateChangedListener(HandleVibrationChange);
        }
        if (timer != null)
        {
            timer.RemoveOnStateChangedListener(HandleTimer);
        }

        if (soundSlider_Offset != null)
        {
            soundSlider_Offset.RemoveOnValueChangedListener(HandleOffsetChange);
        }
    }

    /// <summary>
    /// 설정 UI를 초기화합니다.
    /// GameSetting에서 현재 값을 불러와 UI에 적용하고, 초기 값을 저장합니다.
    /// </summary>
    public override void Init()
    {
        // 1. GameSetting에서 현재 설정 값을 가져옵니다.
        _initialVolume = GameSetting.Volume;
        _initialVolume_Effect = GameSetting.Volume_Effect;
        _initialBottomOffset = GameSetting.PuzzleBottomOffeset;
        _initialVibrationState = GameSetting.Vibration;
        _initialTimer = GameSetting.Timer;

        // 2. 가져온 값으로 UI 컨트롤을 초기화합니다.
        if (soundSlider != null)
        {
            soundSlider.SetValue(_initialVolume);
        }
        if (soundSlider_Effect != null)
        {
            soundSlider_Effect.SetValue(_initialVolume_Effect);
        }
        if (soundSlider_Offset != null)
        {
            soundSlider_Offset.SetValue(_initialBottomOffset);
        }
        if (vibration != null)
        {
            vibration.SetState(_initialVibrationState, true); // 즉시 상태 변경
        }
        if (timer != null)
        {
            timer.SetState(_initialTimer, true); // 즉시 상태 변경
        }

        // 3. 값 변경을 감지하기 위해 이벤트 리스너를 등록합니다.
        //    (OnDisable에서 해제하므로 중복 등록 방지를 위해 먼저 제거)
        if (soundSlider != null)
        {
            soundSlider.RemoveOnValueChangedListener(HandleSoundChange); // 중복 등록 방지
            soundSlider.AddOnValueChangedListener(HandleSoundChange);
        }
        if (soundSlider_Effect != null)
        {
            soundSlider_Effect.RemoveOnValueChangedListener(HandleSoundChange_Effect); // 중복 등록 방지
            soundSlider_Effect.AddOnValueChangedListener(HandleSoundChange_Effect);
        }
        if (soundSlider_Offset != null)
        {
            soundSlider_Offset.RemoveOnValueChangedListener(HandleOffsetChange); // 중복 등록 방지
            soundSlider_Offset.AddOnValueChangedListener(HandleOffsetChange);
        }
        if (vibration != null)
        {
            vibration.RemoveOnStateChangedListener(HandleVibrationChange); // 중복 등록 방지
            vibration.AddOnStateChangedListener(HandleVibrationChange);
        }
        if (timer != null)
        {
            timer.RemoveOnStateChangedListener(HandleTimer); // 중복 등록 방지
            timer.AddOnStateChangedListener(HandleTimer);
        }

        //4. 유저 ID 지정
        userID.text = $"{AuthenticationManager.GetPlayerId()}";
    }

    /// <summary>
    /// 사운드 슬라이더의 값이 변경될 때 호출될 이벤트 핸들러입니다.
    /// </summary>
    private void HandleSoundChange(float newValue)
    {
        GameSetting.Volume = newValue;
        /*if(first1)*/ SoundManager.Instance.SettingVolume();
        first1 = true;
    }

    /// <summary>
    /// 사운드 슬라이더의 값이 변경될 때 호출될 이벤트 핸들러입니다.
    /// </summary>
    private void HandleSoundChange_Effect(float newValue)
    {
        GameSetting.Volume_Effect = newValue;
        if(first1_effect) SoundManager.Instance.PlayClickUI();
        SoundManager.Instance.SettingVolume();
        first1_effect = true;
    }

    /// <summary>
    /// 하단 여백 값이 변경될 때 적용할 값
    /// </summary>
    private void HandleOffsetChange(float newValue)
    {
        GameSetting.PuzzleBottomOffeset = newValue;
        SoundManager.Instance.PlayClickUI();
    }

    /// <summary>
    /// 진동 슬라이더(On/Off)의 상태가 변경될 때 호출될 이벤트 핸들러입니다.
    /// </summary>
    private void HandleVibrationChange(bool isOn)
    {
        GameSetting.Vibration = isOn;
        SoundManager.Instance.PlayClickUI();
        first2 = true;
    }
    

    /// <summary>
    /// 진동 슬라이더(On/Off)의 상태가 변경될 때 호출될 이벤트 핸들러입니다.
    /// </summary>
    private void HandleTimer(bool isOn)
    {
        GameSetting.Timer = isOn;
        SoundManager.Instance.PlayClickUI();
    }

    /// <summary>
    /// "닫기" 버튼 클릭 시 호출됩니다.
    /// UI가 열렸을 때의 값과 현재 값을 비교하여 변경 사항이 있으면 로그를 출력합니다.
    /// </summary>
    public void OnClickCloseUI()
    {
        SoundManager.Instance.PlayClickUI();

        bool soundChanged = !Mathf.Approximately(_initialVolume, GameSetting.Volume);
        bool vibrationChanged = _initialVibrationState != GameSetting.Vibration;
        bool timerChanged = _initialTimer != GameSetting.Timer;
        bool offsetChanged = _initialBottomOffset != GameSetting.PuzzleBottomOffeset;

        if (soundChanged || vibrationChanged || timerChanged || offsetChanged)
        {
            Debug.Log("SettingUI: 설정이 변경되었습니다.");
            if (soundChanged)
            {
                Debug.Log($" - 효과음 볼륨 변경: {_initialVolume} -> {GameSetting.Volume}");
                SoundManager.Instance?.SettingVolume();
            }
            if (vibrationChanged)
            {
                Debug.Log($" - 진동 설정 변경: {_initialVibrationState} -> {GameSetting.Vibration}");
            }
            if (timerChanged)
            {
                Debug.Log($" - 타이머 설정 변경: {_initialTimer} -> {GameSetting.Timer}");
                HUDContainer.Instance?.UpdateTimer();
            }
            if (offsetChanged)
            {
                Debug.Log($" - 하단 오프셋 설정 변경: {_initialBottomOffset} -> {GameSetting.PuzzleBottomOffeset}");
                HUDContainer_InGame.Instance?.UpdatePieceInventory();
            }

            // 변경된 내용을 최종 저장
            GameSetting.Save();
        }
        else
        {
            Debug.Log("SettingUI: 변경된 설정 없음.");
        }

        CloseUI();

        // UI를 닫는 로직 (예: UIManager를 통해)
        // UIManager.Instance.CloseUI(this);
    }

    /// <summary> 카피보드 복사 </summary>
    public void OnClickCopyUUID()
    {
        string playerId = AuthenticationManager.GetPlayerId();
        
        SoundManager.Instance.PlayClickUI();

        // 유니티 내장 시스템 클립보드 버퍼에 문자열 대입
        GUIUtility.systemCopyBuffer = playerId;

        Notice.Message("Copied to Clipboard");
    }
}