
using System;

public class GameSetting
{
    // 내부 필드를 통해 설정 값을 저장
    private static SettingValue _value = null;

    // 저장 필요하면 true
    static bool needSave = false;

    static SettingValue Value
    {
        get
        {
            // _value가 null이면 데이터 로드
            if (_value == null)
            {
                _value = SaveDataManager.LoadDataLocal("SettingValue", new SettingValue());
            }
            return _value;
        }
    }

    // 변경된 설정 값을 저장하는 메서드
    public static void Save()
    {
        if (needSave)
        {
            SaveDataManager.SaveDataLocalToJson("SettingValue", Value);
            needSave = false;
        }
    }

    public static void Load()
    {
        _value = SaveDataManager.LoadDataLocal("SettingValue", new SettingValue());
    }

    public static void Clear()
    {
        PlayerPrefs.DeleteKey("SettingValue");
    }

    // 사운드 크기
    public static float Volume
    {
        get => Value.Volume;
        set
        {
            Value.Volume = value;
            needSave = true;
        }
    }

    // 효과음 사운드 크기
    public static float Volume_Effect
    {
        get => Value.Volume_Effect;
        set
        {
            Value.Volume_Effect = value;
            needSave = true;
        }
    }


    // 진동 여부
    public static bool Vibration
    {
        get => Value.Vibration;
        set
        {
            Value.Vibration = value;
            needSave = true;
        }
    }


    // 낮 밤 모드 설정
    public static bool Night
    {
        get => Value.Night;
        set
        {
            Value.Night = value;
            needSave = true;
        }
    }

    // 타이머 온 오프
    public static bool Timer
    {
        get => Value.TimerOn;
        set
        {
            Value.TimerOn = value;
            needSave = true;
        }
    }

    // 튜토리얼 완료 시 True
    public static int TutorialStep
    {
        get => Value.TutorialStep;
        set
        {
            Value.TutorialStep = value;
            needSave = true;
        }
    }


    // 샘플 사이즈 크기
    public static float SampleSize
    {
        get => Value.SampleSize;
        set
        {
            Value.SampleSize = value;
            needSave = true;
        }
    }

    // 하단 여백 값
    public static float PuzzleBottomOffeset
    {
        get => Value.PuzzleBottomOffeset;
        set
        {
            Value.PuzzleBottomOffeset = value;
            needSave = true;
        }
    }
}

[Serializable]
public class SettingValue
{
    //사운드 크기
    public float Volume = 0.5f;
    //효과음 크기
    public float Volume_Effect = 0.5f;
    //진동
    public bool Vibration = false;
    //낮 밤 모드
    public bool Night = false;
    //샘플 이미지 사이즈
    public float SampleSize = 0.25f;
    // 게임 밑 여유 사이즈 공간
    public float PuzzleBottomOffeset = 0.0f;
    //타이머 표기 여부
    public bool TimerOn = false;
    //튜토리얼 종료 여부
    public int TutorialStep = -1;
}
