using Managers;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 배경 색이 바뀌는 동작을 수행하기 위함
/// </summary>
public class NightSlider : MonoBehaviour, IOnOff
{
    public Slider slider;
    public float slider_value;

    public bool Active { get; set; }

    public bool Off()
    {
        Active = false;
        SetNight(false);
        return Active;
    }

    public bool On()
    {
        Active = true;
        SetNight(true);
        return Active;
    }

    private void Awake()
    {
        EventManager.Instance.SetNight();
        slider.value = GameSetting.Night ? 1 : 0;
    }

    void SetNight(bool night)
    {
        GameSetting.Night = night;

        if (night)
        {
            StartCoroutine(SetNightCroutine(1));
        }
        else
        {
            StartCoroutine(SetNightCroutine(0));
        }
    }

    IEnumerator SetNightCroutine(float target)
    {
        if(slider != null)
        {
            float value = slider.value;

            float t = 0;
            float ab = 1.0f / slider_value;

            while (t <1.0f)
            {
                t += Time.deltaTime * ab;
                value = Mathf.Lerp(value, target, t);
                slider.value = value;

                yield return null;
            }

            slider.value = target;
        }

        GameSetting.Night = target == 1.0f;
        EventManager.Instance.SetNight();
        SoundManager.Instance.PlayClickUI();
    }
}
