using Custom;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CameraController : MonoBehaviour, INight
{
    public static CameraController Instance { get; private set; }

    [Header("Basic Settings")]
    public Vector3 Offset = new Vector3(0, 0, -10);

    [Header("Movement")]
    public float MovementSensitivity = 1.0f;
    public Vector2 MinLeftBotton = new Vector2(-10, -10);
    public Vector2 MaxRightTop = new Vector2(10, 10);

    [Header("Smoothing (User Control)")]
    [Tooltip("사용자 조작 시 반응 속도 (높을수록 빠름, 10~20 추천)")]
    public float moveLerpSpeed = 15.0f;
    [Tooltip("줌 동작 반응 속도")]
    public float zoomLerpSpeed = 10.0f;

    [Header("Scroll / Zoom")]
    public float ScrollSensitivity = 0.05f;
    public float MinOrthoSize = 1.0f;
    public float MaxOrthoSize = 25.0f;

    private float baseOrthSize = 5.0f;

    [Header("Components")]
    public GraphicRaycaster graphicRaycaster;
    public EventSystem eventSystem;

    [Header("Debug / Auto Ref")]
    [SerializeField] Vector3 previosPosition;
    [SerializeField] Vector3 currentPosition;

    PointerEventData pointerEventData;
    Camera cam;
    InputData input;
    PuzzleContainer container = null;

    bool Down;

    // --- 타겟 변수 ---
    private Vector3 targetPosition;
    private float targetOrthoSize;

    // --- 리사이즈(Duration) 제어 변수 ---
    private bool isResizing = false;        // 현재 자동 리사이즈 중인가?
    private Vector3 resizeStartPos;         // 리사이즈 시작 위치
    private float resizeStartSize;          // 리사이즈 시작 사이즈
    private float resizeDuration;           // 목표 소요 시간
    private float resizeTimer;              // 경과 시간

    public PuzzleContainer Container { set { CustomDebug.PrintE("카메라 컨테이너 등록"); container = value; } }
    public static float RatioOrthoValue => Instance == null ? 1.0f : (Instance.cam.orthographicSize / Instance.baseOrthSize);

    private void Awake()
    {
        Instance = this;
        pointerEventData = new PointerEventData(eventSystem);
        cam = GetComponent<Camera>();

        StateManager.Instance.InsertGameStateAction(LocalGameState.Starting, () => ResizeCamera(true));
        EventManager.Instance.action_SetNight += SetNight;
    }

    private void OnDestroy()
    {
        EventManager.Instance.action_SetNight -= SetNight;
        if (StateManager.Instance) StateManager.Instance.DeleteGameStateAction(LocalGameState.Starting, () => ResizeCamera(true));
        Instance = null;
    }

    void Start()
    {
        input = InputManager.Instance.input;
        input.UseOtherScroll = false;

        targetPosition = transform.position;
        targetOrthoSize = cam.orthographicSize;
        baseOrthSize = cam.orthographicSize;
    }

    void Update()
    {
        // 1. 사용자 입력 체크 및 목표값 계산
        // 입력이 발생하면 isResizing은 false가 됩니다.
        CalculateMovementTarget();
        CalculateZoomTarget();

        // 2. 실제 이동 적용 (자동 리사이즈 vs 사용자 제어 분기 처리)
        ApplySmoothMovement();
    }

    /// <summary>
    /// 입력 처리: 터치/마우스 이동
    /// </summary>
    void CalculateMovementTarget()
    {
        if (StateManager.Instance.State == LocalGameState.Playing)
        {
            switch (input.touchState)
            {
                case InputData.TouchState.Down:
                    if (IsPointerOverUIElement(input.OriginTouchPosition)) break;

                    Down = true;
                    previosPosition = input.S2WTouchPosition;

                    // ★ 핵심: 사용자 입력 발생 시 자동 리사이즈 즉시 중단 및 제어권 획득
                    if (isResizing)
                    {
                        isResizing = false;
                        targetOrthoSize = cam.orthographicSize; // 줌도 현재 상태에서 멈춤
                    }

                    targetPosition = transform.position; // 현재 위치에서 다시 시작
                    break;

                case InputData.TouchState.Move:
                    if (!Down) break;

                    currentPosition = input.S2WTouchPosition;
                    Vector3 difference = currentPosition - previosPosition;
                    previosPosition = currentPosition;

                    Vector3 pos = targetPosition - difference * MovementSensitivity;
                    targetPosition = CustomCalculator.Clamp(pos, MinLeftBotton, MaxRightTop) + Offset;
                    break;

                case InputData.TouchState.Up:
                    Down = false;
                    previosPosition = Vector2.zero;
                    break;
            }
        }
    }

    /// <summary>
    /// 입력 처리: 줌
    /// </summary>
    void CalculateZoomTarget()
    {
        if (input.Scroll != 0.0f && !input.UseOtherScroll)
        {
            if (IsPointerOverUIElement(input.OriginTouchPosition)) return;

            // ★ 핵심: 줌 입력 발생 시 자동 리사이즈 즉시 중단
            if (isResizing)
            {
                isResizing = false;
                targetPosition = transform.position; // 이동도 현재 상태에서 멈춤
            }

            // 현재 카메라 크기 기준으로 목표 재설정 (즉각 반응)
            targetOrthoSize = Mathf.Clamp(cam.orthographicSize + (input.Scroll * ScrollSensitivity), MinOrthoSize, MaxOrthoSize);
        }
    }

    /// <summary>
    /// 이동 적용: 리사이즈 모드와 사용자 제어 모드를 분기하여 처리
    /// </summary>
    void ApplySmoothMovement()
    {
        // [Mode 1] 자동 리사이즈 (Duration 기반 보간 이동)
        if (isResizing)
        {
            resizeTimer += Time.deltaTime;
            float t = resizeTimer / resizeDuration;

            // SmoothStep을 사용하여 시작과 끝을 부드럽게 (Ease In-Out 효과)
            t = Mathf.SmoothStep(0.0f, 1.0f, t);

            if (t >= 1.0f)
            {
                // 완료 시 정확한 목표값 설정 후 모드 종료
                transform.position = targetPosition;
                cam.orthographicSize = targetOrthoSize;
                isResizing = false;
            }
            else
            {
                // 시간 진행도(t)에 따른 위치/크기 결정
                transform.position = Vector3.Lerp(resizeStartPos, targetPosition, t);
                cam.orthographicSize = Mathf.Lerp(resizeStartSize, targetOrthoSize, t);
            }
        }
        // [Mode 2] 사용자 제어 / 일반 대기 (Speed 기반 Lerp 이동)
        else
        {
            // 위치 이동
            if (Vector3.Distance(transform.position, targetPosition) > 0.001f)
            {
                transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * moveLerpSpeed);
            }
            else
            {
                transform.position = targetPosition;
            }

            // 줌
            if (Mathf.Abs(cam.orthographicSize - targetOrthoSize) > 0.001f)
            {
                cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetOrthoSize, Time.deltaTime * zoomLerpSpeed);
            }
            else
            {
                cam.orthographicSize = targetOrthoSize;
            }
        }
    }

    // --- Resize Functions ---

    /// <summary>
    /// 퍼즐 컨테이너에 맞춰 카메라 크기와 위치를 재설정합니다.
    /// </summary>
    /// <param name="immediate">true: 즉시 이동</param>
    public void ResizeCamera(bool immediate = false)
    {
        // Duration을 명시하지 않으면 기본적으로 즉시 이동하거나(초기화), 
        // 혹은 아주 짧은 시간(예: 0.5초)을 기본값으로 줄 수도 있습니다.
        // 현재 로직상 immediate가 true면 즉시, false면 기존 speed 로직을 따르도록 isResizing을 켜지 않습니다.

        var (calSize, calPos) = CalculateCameraTarget();
        if (calSize == -1) return;

        targetOrthoSize = calSize;
        targetPosition = calPos;
        baseOrthSize = calSize;

        if (immediate)
        {
            cam.orthographicSize = targetOrthoSize;
            transform.position = targetPosition;
            isResizing = false;
        }
    }

    /// <summary>
    /// 지정된 시간(duration) 동안 목표 지점으로 부드럽게 이동합니다.
    /// 도중에 사용자 입력이 들어오면 취소됩니다.
    /// </summary>
    public void ResizeCamera(float duration)
    {
        var (calSize, calPos) = CalculateCameraTarget();
        if (calSize == -1) return;

        // 목표 설정
        targetOrthoSize = calSize;
        targetPosition = calPos;
        baseOrthSize = calSize;

        // 리사이즈 모드 활성화
        isResizing = true;
        resizeDuration = duration;
        resizeTimer = 0f;

        // 보간을 위한 시작점 저장
        resizeStartPos = transform.position;
        resizeStartSize = cam.orthographicSize;
    }

    private (float size, Vector3 position) CalculateCameraTarget()
    {
        if (container == null) container = FindObjectOfType<PuzzleContainer>();
        if (container == null) return (-1, Vector3.zero);

        SpriteRenderer renderer = container.BackgroundPiece.GetComponentInChildren<SpriteRenderer>();
        if (renderer == null) return (-1, Vector3.zero);

        float targetWidth = renderer.bounds.size.x;
        float targetHeight = renderer.bounds.size.y;

        float limitWidth = 0.98f;
        float limitHeight = 0.78f;

        float camAspect = (float)Screen.width / Screen.height;
        float restrictedScreenAspect = (camAspect * limitWidth) / limitHeight;
        float targetAspect = targetWidth / targetHeight;

        float calSize;

        if (targetAspect > restrictedScreenAspect)
            calSize = (targetWidth * 0.5f) / (camAspect * limitWidth);
        else
            calSize = (targetHeight * 0.5f) / limitHeight;

        Vector3 calPos = renderer.bounds.center + Offset;
        calPos.z = transform.position.z;

        return (calSize, calPos);
    }

    // --- Helper Functions ---

    public void SetNight(bool night)
    {
        if (cam != null) cam.backgroundColor = night ? Color.black : Color.white;
    }

    private bool IsPointerOverUIElement(Vector2 position)
    {
        pointerEventData.position = position;
        List<RaycastResult> raycastResults = new List<RaycastResult>();
        graphicRaycaster.Raycast(pointerEventData, raycastResults);
        return raycastResults.Count > 0;
    }

    public List<string> GetAllUIElementNames(Vector2 position)
    {
        pointerEventData.position = position;
        List<RaycastResult> results = new List<RaycastResult>();
        graphicRaycaster.Raycast(pointerEventData, results);
        List<string> uiNames = new List<string>();
        foreach (RaycastResult result in results) uiNames.Add(result.gameObject.name);
        return uiNames;
    }
}