using UnityEngine;
using System.Collections;

/// <summary>
/// 화면 밖에서 안으로, 안에서 밖으로 슬라이드하는 애니메이션 클래스
/// </summary>
public class UIFullySlideAnimation : UIAnimationParent
{
    public RectTransform rectTransform;

    public enum Direction { Left, Right, Top, Bottom }

    [Header("슬라이드 설정")]
    public Direction showDirection = Direction.Left; // 나타날 방향

    /// <summary> 사라집니까? </summary>
    public bool disappear = false;

    private Coroutine activeCoroutine;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    /// <summary>
    /// 화면 밖에서 중앙(0,0)으로 슬라이드 인
    /// </summary>
    public override void Show()
    {
        Vector2 startPos = GetOutsidePosition(showDirection);
        Vector2 endPos = Vector2.zero;

        if (disappear)
        {
            endPos = startPos;
            startPos = Vector2.zero;
        }
        StartAnimation(startPos, endPos);
    }

    /// <summary>
    /// 중앙(0,0)에서 화면 밖으로 슬라이드 아웃
    /// </summary>
    public override void Hide()
    {
    }

    /// <summary>
    /// 방향에 따른 화면 밖 좌표 계산 (Pivot 0.5 기준)
    /// </summary>
    private Vector2 GetOutsidePosition(Direction dir)
    {
        // Stretch 상태이므로 rect.width/height가 화면 전체 크기와 동일함
        float width = rectTransform.rect.width;
        float height = rectTransform.rect.height;

        return dir switch
        {
            Direction.Left => new Vector2(-width, 0),
            Direction.Right => new Vector2(width, 0),
            Direction.Top => new Vector2(0, height),
            Direction.Bottom => new Vector2(0, -height),
            _ => Vector2.zero
        };
    }

    /// <summary>
    /// 코루틴 중첩 방지 및 실행
    /// </summary>
    private void StartAnimation(Vector2 start, Vector2 end)
    {
        if (activeCoroutine != null) StopCoroutine(activeCoroutine);
        activeCoroutine = StartCoroutine(CoMove(start, end));
    }

    private IEnumerator CoMove(Vector2 start, Vector2 end)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            rectTransform.anchoredPosition = Vector2.Lerp(start, end, t);
            yield return null;
        }
        rectTransform.anchoredPosition = end;
        activeCoroutine = null;
    }
}