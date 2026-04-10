using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.Events;

/// <summary>
/// 슬라이더의 값을 표시하고, 하단에 단계별 가이드(Bar)를 자동으로 생성하고 배치하는 기능을 제공합니다.
/// </summary>
public class UISliderPack : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Slider m_slider;
    [SerializeField] private TextMeshProUGUI m_valueText;

    [Header("Step Bar Settings")]
    [SerializeField] private GameObject m_stepBarPrefab;    // 생성할 바 프리팹
    [SerializeField] private Transform m_stepBarContainer; // 바가 생성될 부모 오브젝트

    [Header("Settings")]
    [SerializeField, Range(0, 5)] private int m_decimalPlaces = 1;

    [Header("Step Settings")]
    [SerializeField] private bool m_useSteppedValue = true;
    [SerializeField, Min(0.001f)] private float m_stepInterval = 0.01f;

    [Header("SC Controller")]
    [SerializeField] UIAutoFadeWithImage cs_autoFade;

    /// <summary> 슬라이더 값 반환 </summary>
    public float GetValue => m_slider.value;
    
    private readonly UnityEvent<float> _onValueChanged = new UnityEvent<float>();
    private float _lastNotifiedValue = float.MinValue;

    /// <summary>
    /// 슬라이더의 값 변경 이벤트를 수신할 리스너를 등록합니다.
    /// </summary>
    public void AddOnValueChangedListener(UnityAction<float> listener)
    {
        _onValueChanged.AddListener(listener);
    }

    /// <summary>
    /// 슬라이더의 값 변경 이벤트를 수신하던 리스너를 해제합니다.
    /// </summary>
    public void RemoveOnValueChangedListener(UnityAction<float> listener)
    {
        _onValueChanged.RemoveListener(listener);
    }

    /// <summary> 슬라이더 값 설정 </summary>
    /// <param name="value"></param>
    public void SetValue(float value) => OnSliderValueChanged(value);

    private void Awake()
    {
        if (m_slider != null)
        {
            m_slider.onValueChanged.AddListener(OnSliderValueChanged);
        }
    }
    //private Start()
    //{
    //    yield return new WaitForEndOfFrame();

    //    if (m_slider != null)
    //    {
    //        m_slider.onValueChanged.AddListener(OnSliderValueChanged);

    //        // 단계별 값을 사용하는 경우 슬라이더 바 생성
    //        //if (m_useSteppedValue)
    //        //{
    //        //    CreateStepBars();
    //        //}

    //        //OnSliderValueChanged(m_slider.value);
    //    }
    //}

    /// <summary>
    /// 슬라이더의 최소/최대 값에 따라 슬라이더 바를 생성하여 배치합니다.
    /// </summary>
    private void CreateStepBars()
    {
        if (m_stepBarPrefab == null || m_stepBarContainer == null) return;

        RectTransform containerRect = m_stepBarContainer.GetComponent<RectTransform>();
        float min = m_slider.minValue;
        float max = m_slider.maxValue;

        // 일정 간격으로 바를 생성 (부동 소수점 오차를 고려하여 max 값에 작은 값을 더함)
        for (float i = min; i <= max + 0.001f; i += m_stepInterval)
        {
            GameObject bar = Instantiate(m_stepBarPrefab, m_stepBarContainer);
            bar.SetActive(true);
            RectTransform barRect = bar.GetComponent<RectTransform>();

            // 0~1 사이의 정규화된 값으로 위치를 계산합니다.
            float normalizedPos = Mathf.InverseLerp(min, max, i);

            // x축 앵커를 변경하여 위치를 설정하고, y축은 부모에 맞게 늘어납니다.
            barRect.anchorMin = new Vector2(normalizedPos, 0f);
            barRect.anchorMax = new Vector2(normalizedPos, 1f);
            barRect.anchoredPosition = Vector2.zero;
        }
    }

    /// <summary>
    /// 슬라이더 값 변경 시 호출되며, 단계별 값과 텍스트를 업데이트합니다.
    /// </summary>
    private void OnSliderValueChanged(float value)
    {
        if (m_slider == null) return;

        CheckCSOption();

        float finalValue = value;

        if (m_useSteppedValue)
        {
            finalValue = Mathf.Round(value / m_stepInterval) * m_stepInterval;
            finalValue = Mathf.Clamp(finalValue, m_slider.minValue, m_slider.maxValue);

            if (!Mathf.Approximately(m_slider.value, finalValue))
            {
                m_slider.value = finalValue;
                return; // m_slider.value가 변경되면 이 메서드가 다시 호출되므로 여기서 종료
            }
        }

        UpdateValueText(finalValue);
        
        // 최종 값이 이전과 다를 경우에만 이벤트 호출
        if (Mathf.Approximately(_lastNotifiedValue, finalValue)) return;
        
        _lastNotifiedValue = finalValue;
        _onValueChanged?.Invoke(finalValue);
    }

    /// <summary>
    /// 텍스트 UI의 값을 지정된 소수점 자리수에 맞춰 업데이트합니다.
    /// </summary>
    private void UpdateValueText(float value)
    {
        if (m_valueText != null)
        {
            //m_valueText.text = value.ToString($"F{m_decimalPlaces}");
            m_valueText.text = (value * 100.0f).ToString($"F{m_decimalPlaces}"); ;
        }
    }

    private void CheckCSOption()
    {
        if (cs_autoFade) cs_autoFade.ResetFade();
    }

    private void OnDestroy()
    {
        if (m_slider != null)
        {
            m_slider.onValueChanged.RemoveListener(OnSliderValueChanged);
        }
        _onValueChanged.RemoveAllListeners();
    }

}