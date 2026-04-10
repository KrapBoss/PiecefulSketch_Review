using UnityEngine;
using System.Collections;

/// <summary>
/// 지정된 두 RectTransform의 위치 사이를 이동하는 슬라이드 애니메이션입니다.
/// </summary>
public class UISlideAnimator : UIAnimationParent
{
    [Header("Target UI")]
    [SerializeField] private RectTransform m_target;      // 실제로 움직일 UI

    [Header("Position References")]
    [SerializeField] private RectTransform m_startPoint;  // 시작 위치 기준
    [SerializeField] private RectTransform m_endPoint;    // 종료 위치 기준

    private CanvasGroup m_group;

    /// <summary>
    /// 대상에 CanvasGroup을 강제 할당하여 상호작용을 제어합니다.
    /// </summary>
    private void EnsureCanvasGroup()
    {
        if (m_target == null) return;
        if (!m_target.TryGetComponent(out m_group))
            m_group = m_target.gameObject.AddComponent<CanvasGroup>();
    }

    /// <summary>
    /// 시작 지점에서 종료 지점으로 이동합니다.
    /// </summary>
    public override void Show()
    {
        if (m_startPoint == null || m_endPoint == null) return;
        EnsureCanvasGroup();
        StopAllCoroutines();
        StartCoroutine(CoAnimate(m_startPoint.anchoredPosition, m_endPoint.anchoredPosition));
    }

    /// <summary>
    /// 현재 위치에서 다시 시작 지점으로 돌아갑니다.
    /// </summary>
    public override void Hide()
    {
        if (m_startPoint == null) return;
        EnsureCanvasGroup();
        StopAllCoroutines();
        StartCoroutine(CoAnimate(m_target.anchoredPosition, m_startPoint.anchoredPosition));
    }

    private IEnumerator CoAnimate(Vector2 start, Vector2 end)
    {
        // 애니메이션 도중 클릭 방지
        if (m_group != null) m_group.interactable = false;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            // 두 참조 포인트의 anchoredPosition 사이를 보간 이동
            m_target.anchoredPosition = Vector2.Lerp(start, end, elapsed / duration);
            yield return null;
        }
        m_target.anchoredPosition = end;

        // 애니메이션 종료 후 클릭 활성화
        if (m_group != null) m_group.interactable = true;
    }
}