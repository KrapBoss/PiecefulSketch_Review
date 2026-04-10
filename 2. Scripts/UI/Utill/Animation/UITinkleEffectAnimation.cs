using System.Collections;
using UnityEngine;

/// <summary>
/// 일정 간격(interval)으로 대기한 후, 짧은 시간(activeTime) 동안 빠르게 Z축 회전 애니메이션(a->b->c->b->a)을 1회 수행하고 다시 대기 상태로 돌아가는 것을 반복합니다.
/// </summary>
public class UITinkleEffectAnimation : UIAnimationParent
{
    // 인스펙터에서 할당할 RectTransform
    [SerializeField]
    private RectTransform rectTransform;

    // 애니메이션 실행 전 대기 시간 (n초)
    [SerializeField]
    private float interval = 3f;

    // 회전 애니메이션이 1회 진행되는 총 시간 (n2초)
    [SerializeField]
    private float activeTime = 0.4f;

    // 목표 회전값 A (Euler angles)
    [SerializeField]
    private float rotA = 0f;

    // 목표 회전값 B (Euler angles)
    [SerializeField]
    private float rotB = -15f;

    // 목표 회전값 C (Euler angles)
    [SerializeField]
    private float rotC = 15f;

    private Coroutine _animationCoroutine;
    private Quaternion _originalRotation;

    /// <summary>
    /// 초기화 시 RectTransform을 가져오고 초기 회전값을 저장합니다.
    /// </summary>
    private void Awake()
    {
        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
        }
        _originalRotation = rectTransform.localRotation;
    }

    /// <summary>
    /// 애니메이션 코루틴을 시작합니다.
    /// </summary>
    public override void Show()
    {
        if (_animationCoroutine != null)
        {
            StopCoroutine(_animationCoroutine);
        }
        _animationCoroutine = StartCoroutine(AnimationRoutine());
    }

    /// <summary>
    /// 애니메이션을 중지하고 RectTransform의 회전을 원래대로 복원합니다.
    /// </summary>
    public override void Hide()
    {
        if (_animationCoroutine != null)
        {
            StopCoroutine(_animationCoroutine);
            _animationCoroutine = null;
        }
        rectTransform.localRotation = _originalRotation;
    }

    /// <summary>
    /// 실제 애니메이션 로직을 처리하는 코루틴입니다.
    /// </summary>
    private IEnumerator AnimationRoutine()
    {
        // 초기 회전값을 rotA로 설정
        rectTransform.localRotation = Quaternion.Euler(0, 0, rotA);

        while (true)
        {
            // n초 동안 대기
            yield return new WaitForSeconds(interval);

            // 4단계 회전에 걸리는 시간 계산
            float stepDuration = activeTime / 4f;

            // n2초 동안 a -> b -> c -> b -> a 회전을 1회 수행
            yield return StartCoroutine(AnimateRotation(rotA, rotB, stepDuration));
            yield return StartCoroutine(AnimateRotation(rotB, rotC, stepDuration));
            yield return StartCoroutine(AnimateRotation(rotC, rotB, stepDuration));
            yield return StartCoroutine(AnimateRotation(rotB, rotA, stepDuration));

            // 다음 대기를 위해 회전값을 정확히 초기값으로 설정
            rectTransform.localRotation = Quaternion.Euler(0, 0, rotA);
        }
    }

    /// <summary>
    /// 지정된 시간 동안 한 회전값에서 다른 회전값으로 Z축을 부드럽게 변경합니다.
    /// </summary>
    /// <param name="startAngle">시작 각도</param>
    /// <param name="endAngle">종료 각도</param>
    /// <param name="time">변경에 걸리는 시간</param>
    private IEnumerator AnimateRotation(float startAngle, float endAngle, float time)
    {
        float elapsedTime = 0f;
        Quaternion startRotation = Quaternion.Euler(0, 0, startAngle);
        Quaternion endRotation = Quaternion.Euler(0, 0, endAngle);

        // 시간이 0 이하일 경우 즉시 종료값으로 설정하고 루프를 건너뛴다.
        if (time <= 0)
        {
            rectTransform.localRotation = endRotation;
            yield break;
        }

        while (elapsedTime < time)
        {
            float t = elapsedTime / time;
            rectTransform.localRotation = Quaternion.LerpUnclamped(startRotation, endRotation, t);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        rectTransform.localRotation = endRotation;
    }
}