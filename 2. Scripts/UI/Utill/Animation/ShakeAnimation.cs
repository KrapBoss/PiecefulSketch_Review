using System.Collections;
using UnityEngine;

/// <summary>
/// RectTransform을 좌우로 흔드는 진동 애니메이션을 제공합니다.
/// UIAnimationParent를 상속받아 UI 애니메이션 시스템에 통합됩니다.
/// </summary>
public class ShakeAnimation : UIAnimationParent
{
    [Header("Shake Settings")]
    [SerializeField] private float shakeStrength = 10.0f; // 흔들림의 강도
    [SerializeField] private int vibrato = 10; // 흔들리는 횟수

    [SerializeField]private RectTransform rectTransform;
    private Vector2 originalPosition;
    private Coroutine shakeCoroutine;

    void Awake()
    {
        originalPosition = rectTransform.anchoredPosition;
    }

    /// <summary>
    /// 진동 애니메이션을 재생합니다.
    /// </summary>
    public override void Show()
    {
        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
        }
        shakeCoroutine = StartCoroutine(ShakeRoutine());
    }

    /// <summary>
    /// 애니메이션을 숨기는 기능은 이 클래스에서 사용되지 않습니다.
    /// </summary>
    public override void Hide()
    {
        // 이 애니메이션은 일회성이므로 Hide 동작이 필요 없습니다.
    }

    private IEnumerator ShakeRoutine()
    {
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            float x = originalPosition.x + Mathf.Sin(Time.time * vibrato) * shakeStrength * (1 - (elapsedTime / duration));
            rectTransform.anchoredPosition = new Vector2(x, originalPosition.y);
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        rectTransform.anchoredPosition = originalPosition; // 애니메이션 후 원래 위치로 복귀
    }
}
