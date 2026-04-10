using System.Collections;
using UnityEngine;

/// <summary>
/// RectTransform의 Z축 회전을 a -> b -> c -> b -> a 순서로 무한 반복하는 애니메이션입니다.
/// </summary>
public class UIRotateLoopAnimation : UIAnimationParent
{
    // 인스펙터에서 할당할 RectTransform
    [SerializeField]
    private RectTransform rectTransform;

    // 목표 회전값 A (Euler angles)
    [SerializeField]
    private float rotA = -15f;

    // 목표 회전값 B (Euler angles)
    [SerializeField]
    private float rotB = 0f;

    // 목표 회전값 C (Euler angles)
    [SerializeField]
    private float rotC = 15f;

    // 각 회전 단계 사이의 시간 (부모의 duration과 별개로 정의)
    [SerializeField]
    private float stepDuration = 0.5f;

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
    /// 회전 애니메이션 코루틴을 시작합니다.
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
            // a -> b
            yield return StartCoroutine(AnimateRotation(rotA, rotB, stepDuration));
            // b -> c
            yield return StartCoroutine(AnimateRotation(rotB, rotC, stepDuration));
            // c -> b
            yield return StartCoroutine(AnimateRotation(rotC, rotB, stepDuration));
            // b -> a
            yield return StartCoroutine(AnimateRotation(rotB, rotA, stepDuration));
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

        while (elapsedTime < time)
        {
            float t = elapsedTime / time;
            // Quaternion.Lerp를 사용하여 회전을 부드럽게 보간
            rectTransform.localRotation = Quaternion.Lerp(startRotation, endRotation, t);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        // 최종 회전값으로 정확하게 설정
        rectTransform.localRotation = endRotation;
    }
}