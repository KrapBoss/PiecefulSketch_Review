using UnityEngine;
using UnityEngine.UI; // Image 컴포넌트 사용을 위해 필요
using UnityEngine.EventSystems; // UI 이벤트 인터페이스 사용을 위해 필요

/// <summary>
/// 이 스크립트가 할당된 UI 영역 내의 입력만 감지하여 투명도를 자동 조절합니다.
/// Image 컴포넌트가 Raycast Target 역할을 해야 합니다.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
[RequireComponent(typeof(Image))] // Image 컴포넌트 필수 강제
public class UIAutoFadeWithImage : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    [Header("설정 (N초 후 A값으로)")]
    [SerializeField] private float m_idleTimeout = 3.0f; // N초
    [SerializeField] private float m_targetAlpha = 0.3f; // A값 (최종 투명도)
    [SerializeField] private float m_fadeSpeed = 2.0f;   // 사라지는 속도

    private CanvasGroup m_canvasGroup;
    private Image m_image;

    private float _idleTimer;
    private bool _isInteracting = false; // 현재 이 UI와 상호작용 중인지 여부

    void Awake()
    {
        m_canvasGroup = GetComponent<CanvasGroup>();
        m_image = GetComponent<Image>();

        // 중요: 입력을 받기 위해선 Image의 RaycastTarget이 켜져 있어야 함. 강제로 켬.
        if (m_image != null)
        {
            m_image.raycastTarget = true;
        }
    }

    // 1. 터치가 시작될 때 (영역 내)
    public void OnPointerDown(PointerEventData eventData)
    {
        _isInteracting = true;
        ResetFade(); // 즉시 밝게
    }

    // 2. 드래그 중일 때 (영역 내에서 움직일 때)
    // 이게 없으면 누른 채로 가만히 있을 때만 감지될 수 있음
    public void OnDrag(PointerEventData eventData)
    {
        _isInteracting = true;
        ResetFade(); // 계속 밝게 유지
    }

    // 3. 터치가 끝났을 때 (손을 뗐을 때)
    public void OnPointerUp(PointerEventData eventData)
    {
        _isInteracting = false;
        // 이제부터 Update에서 타이머가 돌기 시작함
    }

    void Update()
    {
        if (m_canvasGroup == null) return;

        // 상호작용 중이라면 타이머 계산을 하지 않고 리턴
        if (_isInteracting)
        {
            return;
        }

        // --- 입력이 없는 상태 ---

        // 타이머 증가
        _idleTimer += Time.deltaTime;

        // N초 경과 시 알파값 감소 (A값까지)
        if (_idleTimer >= m_idleTimeout)
        {
            m_canvasGroup.alpha = Mathf.MoveTowards(m_canvasGroup.alpha, m_targetAlpha, Time.deltaTime * m_fadeSpeed);
        }
    }

    /// <summary>
    /// 타이머와 알파값을 초기화합니다.
    /// </summary>
    public void ResetFade()
    {
        _idleTimer = 0f;
        m_canvasGroup.alpha = 1.0f;
    }

    /// <summary>
    /// UI가 켜질 때(SetActive(true)) 상태를 초기화하기 위함
    /// </summary>
    private void OnEnable()
    {
        ResetFade();
        _isInteracting = false;
    }
}
