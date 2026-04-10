using System.Collections;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class UIScaleAnimator : UIAnimationParent
{
    [SerializeField] private RectTransform m_target;
    [SerializeField] private Vector3 m_startScale = Vector3.zero;
    [SerializeField] private Vector3 m_endScale = Vector3.one;

    private CanvasGroup m_group;

    private void EnsureCanvasGroup()
    {
        if (m_target == null) return;
        if (!m_target.TryGetComponent(out m_group))
            m_group = m_target.gameObject.AddComponent<CanvasGroup>();
    }

    public override void Show()
    {
        EnsureCanvasGroup();
        StopAllCoroutines();
        if(m_target.gameObject.activeSelf) StartCoroutine(CoAnimate(m_startScale, m_endScale));
    }

    public override void Hide()
    {
        EnsureCanvasGroup();
        StopAllCoroutines();
        if (m_target.gameObject.activeSelf) StartCoroutine(CoAnimate(m_target.localScale, m_startScale));
    }

    private IEnumerator CoAnimate(Vector3 start, Vector3 end)
    {
        if (m_group != null) m_group.interactable = false;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            m_target.localScale = Vector3.Lerp(start, end, elapsed / duration);
            yield return null;
        }
        m_target.localScale = end;

        if (m_group != null) m_group.interactable = true;

        if (m_next) m_next.Show();
    }
}