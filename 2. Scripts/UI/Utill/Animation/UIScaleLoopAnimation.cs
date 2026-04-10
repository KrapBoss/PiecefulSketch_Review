using System.Collections;
using UnityEngine;

/// <summary>
/// RectTransform의 스케일을 두 지점 사이에서 반복적으로 왕복시키는 애니메이션입니다.
/// </summary>
public class UIScaleLoopAnimation : UIAnimationParent
{
    // 인스펙터에서 할당할 RectTransform
    [SerializeField]
    private RectTransform rectTransform;

    // 목표 스케일 A
    [SerializeField]
    private Vector3 scaleA = Vector3.one;

    // 목표 스케일 B
    [SerializeField]
    private Vector3 scaleB = new Vector3(1.2f, 1.2f, 1.2f);

    private Coroutine _animationCoroutine;
    private Vector3 _originalScale;

    /// <summary>
    /// 초기화 시 RectTransform을 가져오고 초기 스케일을 저장합니다.
    /// </summary>
    private void Awake()
    {
        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
        }
        _originalScale = rectTransform.localScale;
    }

    /// <summary>
    /// 스케일 애니메이션 코루틴을 시작합니다.
    /// </summary>
    public override void Show()
    {
        // 이미 실행 중인 코루틴이 있다면 중지하고 새로 시작합니다.
        if (_animationCoroutine != null)
        {
            StopCoroutine(_animationCoroutine);
        }
        _animationCoroutine = StartCoroutine(AnimationRoutine());
    }

    /// <summary>
    /// 애니메이션을 중지하고 RectTransform의 스케일을 원래대로 복원합니다.
    /// </summary>
    public override void Hide()
    {
        if (_animationCoroutine != null)
        {
            StopCoroutine(_animationCoroutine);
            _animationCoroutine = null;
        }
        rectTransform.localScale = _originalScale;
    }

    /// <summary>
    /// 실제 애니메이션 로직을 처리하는 코루틴입니다.
    /// </summary>
    private IEnumerator AnimationRoutine()
    {
        // 초기 스케일을 scaleA로 설정
        rectTransform.localScale = scaleA;

        while (true)
        {
            // scaleA -> scaleB로 이동 (duration은 부모 클래스에서 상속받음)
            yield return StartCoroutine(AnimateScale(scaleA, scaleB, duration));

            // scaleB -> scaleA로 이동 (duration은 부모 클래스에서 상속받음)
            yield return StartCoroutine(AnimateScale(scaleB, scaleA, duration));
        }
    }

    /// <summary>
    /// 지정된 시간 동안 한 스케일에서 다른 스케일로 부드럽게 변경합니다.
    /// </summary>
    /// <param name="startScale">시작 스케일</param>
    /// <param name="endScale">종료 스케일</param>
    /// <param name="time">변경에 걸리는 시간</param>
    private IEnumerator AnimateScale(Vector3 startScale, Vector3 endScale, float time)
    {
        float elapsedTime = 0f;
        while (elapsedTime < time)
        {
            // 경과 시간에 따른 보간 계수 계산 (0과 1 사이)
            float t = elapsedTime / time;
            // Vector3.Lerp를 사용하여 스케일을 부드럽게 변경
            rectTransform.localScale = Vector3.Lerp(startScale, endScale, t);
            // 다음 프레임까지 대기
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        // 최종 스케일로 정확하게 설정
        rectTransform.localScale = endScale;
    }
}