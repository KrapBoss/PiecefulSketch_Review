using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// UI 슬라이더를 사용하여 On/Off 상태를 제어하고 부드러운 시각적 전환 효과를 제공하는 재사용 가능한 스크립트입니다.
/// 외부에서 이벤트를 등록하여 상태 변경에 반응할 수 있습니다.
/// </summary>
[RequireComponent(typeof(Slider))]
public class SliderOnOff : MonoBehaviour, IOnOff
{
    [SerializeField] TextMeshProUGUI text_OnOff;

    [Header("Settings")]
    [Tooltip("슬라이더가 On 상태일 때의 값")]
    [SerializeField] private float onValue = 1f;

    [Tooltip("슬라이더가 Off 상태일 때의 값")]
    [SerializeField] private float offValue = 0f;

    [Tooltip("On/Off 전환에 걸리는 시간 (초)")]
    [SerializeField] private float transitionDuration = 0.3f;

    [Header("Events")]
    [Tooltip("슬라이더의 On/Off 상태가 변경될 때 호출되는 이벤트 (외부에서 AddListener/RemoveListener를 통해 등록).")]
    private UnityEvent<bool> _onStateChanged = new UnityEvent<bool>();

    private Slider _slider;
    private Coroutine _transitionCoroutine;
    private bool _isOn;

    /// <summary>
    /// 현재 슬라이더의 On/Off 상태를 가져옵니다.
    /// </summary>
    public bool IsOn => _isOn;

    public bool Active { get; set; }

    /// <summary>
    /// 슬라이더의 상태 변경 이벤트를 수신할 리스너를 등록합니다.
    /// </summary>
    /// <param name="listener">등록할 메서드</param>
    public void AddOnStateChangedListener(UnityAction<bool> listener)
    {
        _onStateChanged.AddListener(listener);
    }

    /// <summary>
    /// 슬라이더의 상태 변경 이벤트를 수신하던 리스너를 해제합니다.
    /// </summary>
    /// <param name="listener">해제할 메서드</param>
    public void RemoveOnStateChangedListener(UnityAction<bool> listener)
    {
        _onStateChanged.RemoveListener(listener);
    }

    private void Awake()
    {
        _slider = GetComponent<Slider>();
    }

    /// <summary>
    /// 슬라이더의 상태를 설정합니다.
    /// </summary>
    /// <param name="isOn">새로운 On/Off 상태</param>
    /// <param name="immediate">true일 경우 즉시 상태를 변경합니다.</param>
    public void SetState(bool isOn, bool immediate = false)
    {
        // 상태가 실제로 변경되지 않으면 아무 작업도 수행하지 않습니다.
        if (_isOn == isOn && !immediate)
        {
            return;
        }

        StopAllCoroutines();

        _isOn = isOn;
        float targetValue = _isOn ? onValue : offValue;

        if (_transitionCoroutine != null)
        {
            StopCoroutine(_transitionCoroutine);
        }

        if (immediate || !gameObject.activeInHierarchy)
        {
            _slider.value = targetValue;
            CheckAction(_isOn);
        }
        else
        {
            _transitionCoroutine = StartCoroutine(TransitionCoroutine(targetValue));
        }
    }

    /// <summary>
    /// 슬라이더의 값을 부드럽게 목표 값으로 변경하는 코루틴입니다.
    /// </summary>
    private IEnumerator TransitionCoroutine(float targetValue)
    {
        float startValue = _slider.value;
        float time = 0;

        while (time < transitionDuration)
        {
            time += Time.deltaTime;
            float progress = Mathf.Clamp01(time / transitionDuration);
            _slider.value = Mathf.Lerp(startValue, targetValue, progress);
            yield return null;
        }

        _slider.value = targetValue; // 전환 완료 후 정확한 값 설정

        // 상태 변경 이벤트 호출
        CheckAction(_isOn);
    }

    void CheckAction(bool onoff)
    {
        _onStateChanged?.Invoke(onoff);

        if(text_OnOff) text_OnOff.text = Localization.Localize(onoff ? "On" : "Off");
    }

    public bool On()
    {
        Active = true;
        SetState(true);
        return Active;
    }

    public bool Off()
    {
        Active = false;
        SetState(false);
        return Active;
    }
}
