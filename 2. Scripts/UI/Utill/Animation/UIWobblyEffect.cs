using UnityEngine;
using DG.Tweening;

/// <summary>
/// DOTween을 사용하여 UI 이미지(RectTransform)를 몽글몽글하게 움직이는 효과를 줍니다.
/// 캐릭터가 살아있는 듯한 귀여운 느낌을 표현하기 위해 사용됩니다.
/// UIAnimationParent를 상속받아 Show/Hide로 제어할 수 있습니다.
/// </summary>
public class UIWobblyEffect : UIAnimationParent
{
    [Header("Wobbly Effect Settings")]
    [Tooltip("애니메이션을 적용할 대상 RectTransform 입니다. null이면 이 게임오브젝트의 RectTransform을 사용합니다.")]
    [SerializeField] private RectTransform targetTransform;

    [Tooltip("수평 방향으로 얼마나 커질지에 대한 값입니다. (예: 1.05는 5% 커짐)")]
    [SerializeField] private float xScaleAmount = 1.02f;

    [Tooltip("수직 방향으로 얼마나 커질지에 대한 값입니다. (예: 1.05는 5% 커짐)")]
    [SerializeField] private float yScaleAmount = 1.05f;
    
    [Tooltip("한번 커졌다가 작아지는 데 걸리는 시간입니다.")]
    [SerializeField] private float cycleDuration = 1.2f;

    [Tooltip("사용할 DOTween의 Ease 타입입니다.")]
    [SerializeField] private Ease easeType = Ease.InOutSine;

    private Vector3 originalScale;
    private Tween xTween;
    private Tween yTween;
    private bool isInitialized = false;

    private void Awake()
    {
        if (targetTransform == null)
        {
            targetTransform = GetComponent<RectTransform>();
        }
    }

    /// <summary>
    /// 몽글몽글 움직이는 애니메이션을 시작합니다.
    /// </summary>
    public override void Show()
    {
        // 안전을 위해 기존 트윈이 있다면 제거하고 시작합니다.
        // Hide()가 SetActive(false)등으로 중단되었을 경우를 대비해 스케일을 한번 더 복구해줍니다.
        if (isInitialized)
        {
             targetTransform.localScale = originalScale;
        }

        Hide();

        // 초기 스케일을 한번만, 그리고 0이 아닐 때 저장합니다.
        if (!isInitialized)
        {
            originalScale = targetTransform.localScale;
            // 스케일이 0인 비정상적인 경우, 1로 강제 보정합니다.
            if (originalScale.x == 0f) originalScale.x = 1f;
            if (originalScale.y == 0f) originalScale.y = 1f;
            if (originalScale.z == 0f) originalScale.z = 1f;
            isInitialized = true;
        }

        // X축과 Y축에 대한 트윈을 각각 생성하고 무한반복 설정합니다.
        xTween = targetTransform.DOScaleX(originalScale.x * xScaleAmount, cycleDuration)
            .SetEase(easeType)
            .SetLoops(-1, LoopType.Yoyo);

        yTween = targetTransform.DOScaleY(originalScale.y * yScaleAmount, cycleDuration * 0.8f) // Y축은 다른 주기로 설정하여 더 자연스럽게
            .SetEase(easeType)
            .SetLoops(-1, LoopType.Yoyo);
    }

    /// <summary>
    /// 애니메이션을 중지하고 원래 크기로 되돌립니다.
    /// </summary>
    public override void Hide()
    {
        // 각 트윈이 존재하고 재생 중일 때만 Kill 합니다.
        xTween?.Kill();
        yTween?.Kill();
        
        // 트윈이 적용된 스케일을 원래대로 즉시 복구합니다.
        // isInitialized는 Hide가 Show보다 먼저 호출되는 상황을 방지합니다.
        if (targetTransform != null && isInitialized)
        {
            targetTransform.localScale = originalScale;
        }
    }

    private void OnDestroy()
    {
        // 오브젝트가 파괴될 때 확실하게 트윈을 제거하여 메모리 누수를 방지합니다.
        Hide();
    }
}
