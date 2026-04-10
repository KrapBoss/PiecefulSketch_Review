using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;

/// <summary>
/// 화면 비율에 따라 GridLayoutGroup을 자동 조정하는 유틸
/// 제작 의도: 모바일 비율/회전 대응
/// Unity 효율성: 화면 변경 시에만 재계산
/// </summary>
[RequireComponent(typeof(GridLayoutGroup))]
public class ResponsiveGridLayout : MonoBehaviour, IOnOff
{
    [Serializable]
    public struct AspectRule
    {
        public float minAspect;
        public int constraintCount;
    }

    public enum SizeBaseDirection
    {
        Horizontal,
        Vertical
    }

    public AspectRule[] aspectRules;
    public SizeBaseDirection sizeBaseDirection;
    public RectTransform refRect;

    public float cellSizeMultiplier = 1f;
    public Vector2 cellRatio = Vector2.one;

    public GameObject go_view;
    private PuzzleItemContainerParent m_view;

    private GridLayoutGroup grid;

    // 화면 변경 감지용 캐시
    private int lastScreenWidth;
    private int lastScreenHeight;

    private bool m_isInit = false;
    private bool m_firstInit = false;   //  첫 초기화 여부

    bool _active = false;
    bool IOnOff.Active { get => _active; set => _active = value; }

    /// <summary> 시작 시 INit을 할 것인가? </summary>
    public bool OnStart = false;

    private void Awake()
    {
        m_firstInit = false;
        if(go_view) m_view = go_view.GetComponent<PuzzleItemContainerParent>();
        //Init();
    }

    private void Start()
    {
        if (OnStart)
        {
            Init();
        }
    }

    /// <summary>
    /// 화면 해상도 / 회전 변경 감지
    /// </summary>
    private void Update()
    {
        if (HasScreenChanged() && m_isInit)
        {
            Init();
        }
    }

    /// <summary>
    /// 일정 시간 딜레이 후 셀 설정을 진행합니다.
    /// </summary>
    void Init()
    {
        m_isInit = false;
        StartCoroutine(InitCoroutine());
    }

    IEnumerator InitCoroutine()
    {
        yield return null;

        grid = GetComponent<GridLayoutGroup>();
        CacheScreenSize();
        ApplyGrid();
        m_firstInit = true;
        m_isInit = true;

        m_view?.UpdateState();
    }

    /// <summary>
    /// 현재 화면 크기 캐싱
    /// </summary>
    private void CacheScreenSize()
    {
        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;
    }

    /// <summary>
    /// 화면 크기 변경 여부 확인
    /// </summary>
    private bool HasScreenChanged()
    {
        return Screen.width != lastScreenWidth ||
               Screen.height != lastScreenHeight;
    }

    /// <summary>
    /// 전체 Grid 설정 적용
    /// </summary>
    public void ApplyGrid()
    {
        float aspect = (float)Screen.width / Screen.height;
        int count = GetConstraintCount(aspect);

        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = count;
        grid.cellSize = CalculateCellSize(count);

        //Canvas.ForceUpdateCanvases();
        //LayoutRebuilder.ForceRebuildLayoutImmediate(grid.GetComponent<RectTransform>());

    }

    /// <summary>
    /// 화면 비율 기준 Constraint Count 결정
    /// </summary>
    private int GetConstraintCount(float aspect)
    {
        int result = aspectRules.Length > 0 ? aspectRules[0].constraintCount : 1;

        foreach (var rule in aspectRules)
        {
            if (aspect >= rule.minAspect)
                result = rule.constraintCount;
        }

        return Mathf.Max(1, result);
    }

    /// <summary>
    /// 기준 방향 유지 + cellRatio 비율로 반대축 보정
    /// </summary>
    private Vector2 CalculateCellSize(int count)
    {
        Rect rect = refRect.rect;

        if (sizeBaseDirection == SizeBaseDirection.Horizontal)
        {
            float available = rect.width;
            float spacing = grid.spacing.x * (count - 1);
            float padding = grid.padding.left + grid.padding.right;

            float baseWidth = (available - spacing - padding) / count;
            baseWidth *= cellSizeMultiplier;

            float height = baseWidth * (cellRatio.y / cellRatio.x);
            return new Vector2(baseWidth, height);
        }
        else
        {
            float available = rect.height;
            float spacing = grid.spacing.y * (count - 1);
            float padding = grid.padding.top + grid.padding.bottom;

            float baseHeight = (available - spacing - padding) / count;
            baseHeight *= cellSizeMultiplier;

            float width = baseHeight * (cellRatio.x / cellRatio.y);
            return new Vector2(width, baseHeight);
        }
    }

    public bool On()
    {
        if (!m_firstInit)
        {
            Init();
        }
        _active = true;
        return true;
    }

    public bool Off()
    {
        _active = false;
        return true;
    }
}
