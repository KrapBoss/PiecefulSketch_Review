using UnityEngine;
using System.Collections;

/// <summary>
/// 알파값 조절
/// </summary>
public class UIAlphaAnimator : UIAnimationParent
{
    [SerializeField] private RectTransform m_target;
    [SerializeField] private float m_startAlpha = 0f;
    [SerializeField] private float m_endAlpha = 1f;
    [SerializeField] private bool m_random = false;    // 랜덤하게 duration 지정

    private CanvasGroup m_group;
    public CanvasGroup Group => m_group;

    private void EnsureCanvasGroup()
    {
        if (m_target == null || m_group) return;
        if (!m_target.TryGetComponent(out m_group))
            m_group = m_target.gameObject.AddComponent<CanvasGroup>();
    }

    public override void Show()
    {
        EnsureCanvasGroup();
        StopAllCoroutines();
        if(gameObject.activeInHierarchy)StartCoroutine(CoAnimate(m_startAlpha, m_endAlpha));
        Debug.Log($"[test] {gameObject.transform.childCount} 알파 쇼");
    }

    public override void Hide()
    {
        EnsureCanvasGroup();
        m_group.alpha = 0;
        StopAllCoroutines();
        Debug.Log($"[test] {gameObject.transform.childCount} 알파 하이드");
    }

    private IEnumerator CoAnimate(float start, float end)
    {
        if (m_group != null) m_group.interactable = false;

        float elapsed = 0f;
        float dura = m_random ? Random.Range(duration * 0.8f, duration * 1.5f) : duration;
        while (elapsed < dura)
        {
            elapsed += Time.deltaTime;
            m_group.alpha = Mathf.Lerp(start, end, elapsed / dura);
            yield return null;
        }
        m_group.alpha = end;

        if (m_group != null) m_group.interactable = true;
    }
}
