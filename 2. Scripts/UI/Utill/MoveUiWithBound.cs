using UnityEngine;
using UnityEngine.EventSystems;

public class MoveUiWithBound : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    [Header("Settings")]
    public RectTransform boundary; // 이동 제한 영역 (부모 Panel 등)
    public RectTransform myRect;   // 이동할 이미지 UI

    [Header("Option")]
    [Range(0f, 1f)]
    public float maxOutRatio = 0.9f;

    [Tooltip("낮을수록 빠릿하고, 높을수록 부드럽습니다. (0.05 권장)")]
    public float smoothTime = 0.05f;

    [Tooltip("추격하는 최대 속도입니다.")]
    public float maxSpeed = 10000f;

    private bool isDragging = false;
    private Vector2 targetPosition;    // 목표 위치
    private Vector2 currentVelocity;   // SmoothDamp 내부 계산용 속도
    private Vector2 pointerOffset;     // 클릭 시점의 UI와 마우스 간 거리

    /// <summary>
    /// 외부 드래깅 중지 요청과 활성화 시 사용
    /// </summary>
    public void StopDragging()=> isDragging = false;
    public void StartDragging()=> isDragging = true;

    private void Awake()
    {
        // RectTransform이 할당되지 않았다면 자신을 할당
        if (myRect == null) myRect = GetComponent<RectTransform>();

        // 시작 시 현재 위치를 목표 위치로 초기화
        if (myRect != null) targetPosition = myRect.anchoredPosition;
    }

    /// <summary>
    /// 클릭 시작 시점의 마우스-UI 간 오프셋 계산
    /// </summary>
    public void OnPointerDown(PointerEventData eventData)
    {
        if (boundary == null || myRect == null) return;

        Vector2 localPointerPos;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(boundary, eventData.position, eventData.pressEventCamera, out localPointerPos))
        {
            // 드래그를 시작한 위치와 UI 중심점 사이의 간격을 저장 (튀는 현상 방지)
            pointerOffset = myRect.anchoredPosition - localPointerPos;
            isDragging = true;
        }
    }

    /// <summary>
    /// 드래그 중 실시간 목표 좌표 갱신
    /// </summary>
    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging || boundary == null || myRect == null) return;

        Vector2 localPointerPos;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(boundary, eventData.position, eventData.pressEventCamera, out localPointerPos))
        {
            // 목표 좌표 = 현재 마우스 위치 + 초기 오프셋
            Vector2 desiredPos = localPointerPos + pointerOffset;

            // 경계 영역을 벗어나지 않도록 보정한 값을 목표로 설정
            targetPosition = GetClampedPosition(desiredPos);
        }
    }

    /// <summary>
    /// 드래그 종료 시 상태 해제
    /// </summary>
    public void OnPointerUp(PointerEventData eventData)
    {
        isDragging = false;
    }

    public void SetPosition(Vector2 pos)
    {
        targetPosition = pos;
    }

    private void Update()
    {
        // SmoothDamp: 현재 위치에서 targetPosition까지 smoothTime 동안 부드럽게 이동
        // 0.05s는 약 3~4프레임 내에 추격하므로 "느리다"는 느낌 없이 "부드럽다"는 느낌만 줌
        myRect.anchoredPosition = Vector2.SmoothDamp(
            myRect.anchoredPosition,
            targetPosition,
            ref currentVelocity,
            smoothTime,
            maxSpeed
        );
    }

    /// <summary>
    /// 부모 경계 및 Pivot/Anchor를 고려한 좌표 제한 계산
    /// </summary>
    private Vector2 GetClampedPosition(Vector2 pos)
    {
        Rect bRect = boundary.rect;

        // 스케일이 적용된 실제 UI 크기 계산
        float currentWidth = myRect.rect.width * Mathf.Abs(myRect.localScale.x);
        float currentHeight = myRect.rect.height * Mathf.Abs(myRect.localScale.y);

        // 허용된 밖으로 나갈 거리 계산
        float outX = currentWidth * maxOutRatio;
        float outY = currentHeight * maxOutRatio;

        // 부모의 앵커 위치 보정
        float anchorOffsetX = Mathf.Lerp(bRect.xMin, bRect.xMax, myRect.anchorMin.x);
        float anchorOffsetY = Mathf.Lerp(bRect.yMin, bRect.yMax, myRect.anchorMin.y);

        // 각 방향별 Clamp 범위 산출
        float minX = (bRect.xMin - anchorOffsetX) + (currentWidth * myRect.pivot.x) - outX;
        float maxX = (bRect.xMax - anchorOffsetX) - (currentWidth * (1f - myRect.pivot.x)) + outX;
        float minY = (bRect.yMin - anchorOffsetY) + (currentHeight * myRect.pivot.y) - outY;
        float maxY = (bRect.yMax - anchorOffsetY) - (currentHeight * (1f - myRect.pivot.y)) + outY;

        pos.x = Mathf.Clamp(pos.x, minX, maxX);
        pos.y = Mathf.Clamp(pos.y, minY, maxY);

        return pos;
    }
}