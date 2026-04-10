using System.Collections;
using UnityEngine;

/// <summary>
/// [기능 설명] CanvasGroup의 Alpha 값을 이용해 투명/불투명을 무한 반복하는 클래스
/// [제작 의도] 
/// 1. RequireComponent로 CanvasGroup 존재를 보장하여 Null 참조 방지
/// 2. 코루틴을 사용하여 프레임 단위로 부드러운 페이드 인/아웃 구현 (Update보다 효율적 관리)
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class UIBlinkAnimation : UIAnimationParent
{
    private CanvasGroup _canvasGroup;
    private Coroutine _blinkCoroutine;
    public float alpha = 1.0f;



    /// <summary>
    /// 애니메이션을 시작합니다. (불투명 -> 투명 루프 시작)
    /// </summary>
    public override void Show()
    {
        gameObject.SetActive(true);

        // 기존 코루틴이 돌고 있다면 중지 후 재시작
        if (_blinkCoroutine != null) StopCoroutine(_blinkCoroutine);

        _canvasGroup = GetComponent<CanvasGroup>();
        // 초기화 (완전 불투명 상태에서 시작 원할 경우)
        _canvasGroup.alpha = alpha;

        _blinkCoroutine = StartCoroutine(BlinkRoutine());
    }

    /// <summary>
    /// 애니메이션을 중지하고 객체를 비활성화합니다.
    /// </summary>
    public override void Hide()
    {
        if (_blinkCoroutine != null)
        {
            StopCoroutine(_blinkCoroutine);
            _blinkCoroutine = null;
        }

        _canvasGroup = GetComponent<CanvasGroup>();
        // 숨길 때는 깔끔하게 안보이게 처리하거나 비활성화
        _canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 실제 페이드 인/아웃을 반복하는 루프
    /// </summary>
    private IEnumerator BlinkRoutine()
    {
        while (true)
        {
            // 1. Fade Out (1 -> 0) : 투명해짐
            yield return StartCoroutine(Fade(1f * alpha, 0f));

            // 2. Fade In (0 -> 1) : 불투명해짐
            yield return StartCoroutine(Fade(0f, 1f * alpha));
        }
    }

    /// <summary>
    /// 지정된 시간(duration) 동안 알파값을 선형 보간하는 로직
    /// </summary>
    private IEnumerator Fade(float startAlpha, float endAlpha)
    {
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;

            // Lerp를 이용해 부드러운 전환
            _canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, elapsedTime / duration);

            yield return null; // 다음 프레임 대기
        }

        // 오차 보정: 루프가 끝난 후 정확한 목표값 설정
        _canvasGroup.alpha = endAlpha;
    }
}