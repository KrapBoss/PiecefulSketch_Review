
using Custom;
using Global;
using JetBrains.Annotations;
using Managers;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UI.Core;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 인스펙터에서 설정할 수 있는 가이드 UI의 범위와 간격 값입니다.
/// </summary>
[System.Serializable]
public class GuideSetting
{
    [Tooltip("대상으로부터 손가락까지의 거리(n)")]
    public float fingerOffset = 50f;
    [Tooltip("손가락의 움직임 범위(m)")]
    public float fingerMovementRange = 20f;
    [Tooltip("손가락으로부터 지문까지의 거리")]
    public float descOffset = 100f;
}

/// <summary>
/// 튜토리얼 진행을 관리하는 싱글톤 클래스입니다.
/// </summary>
public class TutorialGuide : MonoBehaviour, IPointerClickHandler
{
    #region Singleton
    public static TutorialGuide Instance { get; private set; }
    #endregion

    public enum TutorialStep
    {
        None,
        EnterLobby,                 // 로비 입성
        ViewStartUI,                // 시작 UI 활성화    
        EnterInGame,                // 게임에 입장 및 버튼 소개
        InGamePieceTransfer,        // 퍼즐 이동 시키기 위한 설명
        ViewResultUI,               // 결과 버튼 보았음
        Done,                       // 완료
    }

    /// <summary>
    /// 손가락 오브젝트가 가리킬 방향을 지정하는 열거형입니다.
    /// </summary>
    public enum PointingDirection
    {
        Up,
        Down,
        Left,
        Right
    }

    [Header("튜토리얼 진행 상태")]
    [SerializeField] private TutorialStep currentStep = TutorialStep.None;

    [Header("필수 UI 요소")]
    [Tooltip("A 오브젝트: 가이드를 위한 손가락 UI")]
    [SerializeField] private GameObject fingerObject;
    [Tooltip("B 오브젝트: 지문 UI의 부모 오브젝트. TypingEffectUI 컴포넌트를 포함해야 합니다.")]
    [SerializeField] private GameObject descriptionObject;
    [SerializeField] private TextMeshProUGUI descTextMesh;
    [Tooltip("C 오브젝트: 배경을 어둡게 처리할 UI")]
    [SerializeField] private GameObject backgroundDimmer;
    
    [Header("가이드 UI 상세 설정")]
    [Tooltip("가이드 애니메이션에 사용될 단일 설정입니다.")]
    [SerializeField] private GuideSetting guideSetting;
    [SerializeField] private TextMeshProUGUI skipText;
    [SerializeField] private GameObject skipObject;

    private Coroutine guideAnimationCoroutine; // 현재 실행 중인 가이드 애니메이션 코루틴
    private Coroutine repetitiveMoveCoroutine; // 새로 추가될 반복 동작 코루틴
    private GameObject currentHighlightedObject; // 현재 강조된 UI 오브젝트를 추적
    [SerializeField] private TypingEffectUI typingEffect; // 지문 출력을 위한 타이핑 UI 컴포넌트

    private Action act_Skip;    //만약 스킵이 되었을 경우 되돌려야되는 설정을 되돌려주는 역할, 각 단계에서 설정하며 만약 단계를 넘어가면 비워둔다

    /// <summary> 클릭 입력 대기 </summary>
    bool click = false;

    /// <summary> 튜토리얼을 진행합니다. </summary>
    public static bool Tutorialing => (Instance != null);


    public Image[] mascotts;


    /// <summary>  튜토리얼 완료 사항을 체크합니다.  </summary>
    public static void CheckTutorial()
    {
        var item = SaveDataManager.GetAllPuzzleProgress();

        if (item != null && item.Count > 0)
        {
            CustomDebug.PrintW("퍼즐 클리어 데이터가 있으므로 퍼즐을 완료합니다.");
            GameSetting.TutorialStep = (int)TutorialStep.Done;
            return;
        }

        if (GameSetting.TutorialStep == (int)TutorialStep.Done)
        {
            CustomDebug.PrintW("튜토리얼을 이미 완료하였습니다.");
            return;
        }

        // 튜토리얼 객체생성
        Instantiate(Resources.Load<GameObject>("TutorialGuide"));
    }

    #region Unity Lifecycle
    private void Awake()
    {
        // 싱글톤 인스턴스 설정
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        Instance = null;
    }

    private void Start()
    {
        //타이핑 객체 숨김
        typingEffect.Stop();
        //모두 숨김
        HideAllGuideElements();
        // 현재 튜토링러 진행 상태 설정
        currentStep = TutorialStep.EnterLobby;
        StartTutorial(currentStep);

        // 스킵 오브젝트 비활성화
        skipObject.SetActive(false);
        ApplyFontToUI(skipObject);

        typingEffect.Set(this);

        skipText.text = Localization.Localize("Skip");
    }
    #endregion

    #region 튜토리얼 제어 함수

    /// <summary>
    /// 지정된 튜토리얼 단계를 시작합니다.
    /// </summary>
    /// <param name="step">시작할 튜토리얼 단계</param>
    public void StartTutorial(TutorialStep step)
    {
        if (step == TutorialStep.None || step == TutorialStep.Done) return;

        currentStep = step;
        StartCoroutine(GetTutorialCoroutine(step));
    }

    /// <summary>
    /// 현재 튜토리얼 단계가 완료되었음을 알리고 다음 단계 진행을 시도합니다.
    /// </summary>
    /// <param name="completedStep">완료된 튜토리얼 단계</param>
    public void CompleteCurrentStep(TutorialStep completedStep)
    {
        // 현재 진행 중인 단계와 완료된 단계가 일치하는지 확인
        if (currentStep == completedStep)
        {
            Debug.Log($"튜토리얼 단계 완료: {completedStep}");
            SaveTutorialProgress();
            ProceedToNextStep();
        }
    }
     
    /// <summary>
    /// 다음 튜토리얼 단계로 진행하기 전에 이전 단계를 정리하고 다음 단계를 시작합니다.
    /// </summary>
    private void ProceedToNextStep()
    {
        // 1. 현재 단계의 모든 UI 효과를 정리합니다.
        HideAllGuideElements();     // 모든 가이드 정보 숨기기
        if (currentHighlightedObject != null)
        {
            RemoveHighlight(currentHighlightedObject);
            currentHighlightedObject = null;
        }

        // 2. 다음 단계로 진행합니다.
        int nextStepIndex = (int)currentStep + 1;
        TutorialStep nextStep = (TutorialStep)nextStepIndex;

        // Enum의 마지막 값이 Done이라고 가정
        if (nextStep >= TutorialStep.Done)
        {
            currentStep = TutorialStep.Done;
            Debug.Log("모든 튜토리얼을 완료했습니다.");
            SaveTutorialProgress();   // 완료 후 한번 더 저장
            Destroy(gameObject);
        }
        else
        {
            StartTutorial(nextStep);
        }
    }

    /// <summary>
    /// 튜토리얼 진행 상황을 저장합니다.
    /// </summary>
    private void SaveTutorialProgress()
    {
        // TODO: GameSetting 또는 다른 데이터 관리자를 통해 현재 단계(currentStep)를 저장하는 로직 구현
        Debug.Log($"진행도 저장: {currentStep}");
        // 예: GameSetting.SaveTutorialStep(currentStep);

        GameSetting.TutorialStep = (int)currentStep;
        GameSetting.Save();
    }

    /// <summary>
    /// 튜토리얼 단계에 해당하는 코루틴을 반환합니다.
    /// </summary>
    /// <param name="step">실행할 튜토리얼 단계</param>
    /// <returns>해당 단계의 코루틴</returns>
    private IEnumerator GetTutorialCoroutine(TutorialStep step)
    {
        switch (step)
        {
            case TutorialStep.EnterLobby:
                return Tutorial_EnterLobby();
            case TutorialStep.ViewStartUI:
                return Tutorial_ViewStartUI();
            case TutorialStep.EnterInGame:
                return Tutorial_EnterInGame();
            case TutorialStep.InGamePieceTransfer:
                return Tutorial_InGamePieceTransfer();
            case TutorialStep.ViewResultUI:
                return Tutorial_ViewResultUI();
            default:
                // 유효하지 않은 단계의 경우 비어있는 코루틴을 반환하여 오류를 방지합니다.
                return EmptyCoroutine();
        }
    }

    #endregion

    #region 튜토리얼 단계별 코루틴 (Empty Stubs)

    /// <summary>
    /// 아무 작업도 수행하지 않는 비어있는 코루틴입니다.
    /// </summary>
    private IEnumerator EmptyCoroutine()
    {
        yield break;
    }

    private IEnumerator Tutorial_EnterLobby()
    {
        Debug.Log("튜토리얼 단계 시작: 로비 입장");

        ShowDimmer();   // 배경 보이기

        //지문 대사 추가
        List<string> texts = new List<string>() { "EnterLobby1", "EnterLobby2" };
        bool done = false;
        SetTyping(texts, () => done = true);

        //지문 대사 종료 대기
        yield return new WaitUntil(() => done);

        //첫번재 퍼즐 리스트 아이템 불러오기
        var screen = UIScreenManager.Instance.GetUI(UIName.LOBBY_BASE_UI);
        PuzzleListView puzzleListView = screen.GetComponentInChildren<PuzzleListView>();
        puzzleListView.GetComponent<ScrollRect>().enabled = false;
        GameObject item = puzzleListView.GetFirst();
        HighlightUIObject(item);

        act_Skip = () => puzzleListView.GetComponent<ScrollRect>().enabled = true;

        ShowGuideAnimation(item, PointingDirection.Down, PointingDirection.Right, "Click on the first puzzle!");

        //퍼즐 시작 UI 활성화 대기
        yield return new WaitUntil(() => UIScreenManager.Instance.GetUI(UIName.PUZZLE_START_UI) != null);
        // 하이라이트 제거
        RemoveHighlight(item);
        act_Skip = null;

        // 다음 튜토리얼 보이기 및 모든 것 정리
        CompleteCurrentStep(currentStep);

        // 로비 입장에 필요한 가이드 로직 구현
        yield return null;
    }

    private IEnumerator Tutorial_ViewStartUI()
    {
        Debug.Log("튜토리얼 단계 시작: 시작 UI 활성화");

        ShowDimmer();

        //지문 대사 추가
        List<string> texts = new List<string>() { "ViewStartUI1"};
        bool done = false;
        SetTyping(texts, () => done = true);

        //지문 대사 종료 대기
        yield return new WaitUntil(() => done);

        //퍼즐 정보 보이기
        var screen = UIScreenManager.Instance.GetUI(UIName.PUZZLE_START_UI);
        GameObject itemn = FindChildRecursive(screen.gameObject, "BackGroundSample");
        HighlightUIObject(itemn);
        ShowGuideAnimation(itemn, PointingDirection.Down, PointingDirection.Down, "This is when you complete the puzzle and the puzzle name!");
        // 클릭 대기
        click = false;
        yield return new WaitUntil(() => click);
        // 하이라이트 제거
        RemoveHighlight(itemn);
        SoundManager.Instance.PlayClickUI();

        // 타임 스탬프
        GameObject item2 = FindChildRecursive(screen.gameObject, "TimeStampGroup");
        HighlightUIObject(item2);
        ShowGuideAnimation(item2, PointingDirection.Up, PointingDirection.Up, "You can get additional rewards especially if you clear it in a short time!");

        // 클릭 대기
        click = false;
        yield return new WaitUntil(() => click);
        // 하이라이트 제거
        RemoveHighlight(item2);
        SoundManager.Instance.PlayClickUI();


        // 타임 스탬프
        GameObject item3 = FindChildRecursive(screen.gameObject, "StartButton");
        // 시작 이벤트 등록
        UnityAction completeAction = () => CompleteCurrentStep(currentStep);
        item3.GetComponent<Button>().onClick.AddListener(completeAction);

        // 스킵 시 리스너를 제거하도록 설정
        act_Skip = () => item3.GetComponent<Button>().onClick.RemoveListener(completeAction);

        HighlightUIObject(item3);
        ShowGuideAnimation(item3, PointingDirection.Up, PointingDirection.Up, "Now, shall we press the button to start?");

        // 다음 단계로 넘어가기 전에 스킵 액션 비우기
        act_Skip = null;

        // 다음 튜토리얼 보이기 및 모든 것 정리

        // 시작 UI 관련 가이드 로직 구현
        yield return null;
    }

    private IEnumerator Tutorial_EnterInGame()
    {
        Debug.Log("튜토리얼 단계 시작: 게임에 입장");

        //UI 활성화 대기
        yield return new WaitUntil(() => UIScreenManager.Instance.GetUI(UIName.SINGLE_BASE_UI) != null);
        //카메라 사이즈 정렬
        CameraController.Instance?.ResizeCamera(true);
        ShowDimmer();


        //지문 대사 추가
        List<string> texts = new List<string>() { "EnterInGame1", "EnterInGame2", "EnterInGame3" };
        bool done = false;
        SetTyping(texts, () => done = true);

        //지문 대사 종료 대기
        yield return new WaitUntil(() => done);

        Button btn = null;

        // 싱글 베이스 UI
        var screen = UIScreenManager.Instance.GetUI(UIName.SINGLE_BASE_UI);
        GameObject item = FindChildRecursive(screen.gameObject, "Button_Hint");
        btn = item.GetComponent<Button>();
        btn.enabled = false;

        GameObject item2 = FindChildRecursive(screen.gameObject, "Button_ResizeCam");
        Button btn2 = item2.GetComponent<Button>();
        btn2.enabled = false;

        GameObject item3 = FindChildRecursive(screen.gameObject, "Button_Sampler");
        Button btn3 = item3.GetComponent<Button>();
        btn3.enabled = false;

        // 스킵 시 버튼들을 다시 활성화하도록 설정
        act_Skip = () => {
            if (btn != null) btn.enabled = true;
            if (btn2 != null) btn2.enabled = true;
            if (btn3 != null) btn3.enabled = true;
        };

        HighlightUIObject(item);
        ShowGuideAnimation(item, PointingDirection.Down, PointingDirection.Down, "T_Hint Button");
        // 클릭 대기
        click = false;
        yield return new WaitUntil(() => click);
        RemoveHighlight(item);
        btn.enabled = true;
        SoundManager.Instance.PlayClickUI();

        HighlightUIObject(item2);
        ShowGuideAnimation(item2, PointingDirection.Down, PointingDirection.Down, "T_Button_ResizeCam");
        // 클릭 대기
        click = false;
        yield return new WaitUntil(() => click);
        RemoveHighlight(item2);
        btn2.enabled = true;
        SoundManager.Instance.PlayClickUI();

        HighlightUIObject(item3);
        ShowGuideAnimation(item3, PointingDirection.Down, PointingDirection.Down, "T_Button_Sampler");
        // 클릭 대기
        click = false;
        yield return new WaitUntil(() => click);
        RemoveHighlight(item3);
        btn3.enabled = true;
        act_Skip = null;

        SoundManager.Instance.PlayClickUI();

        CompleteCurrentStep(currentStep);

        // 게임 입장 관련 가이드 로직 구현
        yield return null;
    }

    private IEnumerator Tutorial_InGamePieceTransfer()
    {
        Debug.Log("튜토리얼 단계 시작: 퍼즐 조각 이동");

        ShowDimmer();

        var screen = UIScreenManager.Instance.GetUI(UIName.SINGLE_BASE_UI);

        GameObject item = FindChildRecursive(screen.gameObject, "Panel_ObjectScrollBar_Horizontal");
        PuzzlePieceScrollView puzzlePieceScrollView = item.GetComponent<PuzzlePieceScrollView>();
        HighlightUIObject(item);
        GraphicRaycaster caster = item.GetComponent<GraphicRaycaster>();
        caster.enabled = false;
        act_Skip = () =>
        {
            if (caster != null) caster.enabled = true;
        };
        ShowGuideAnimation(item, PointingDirection.Up, PointingDirection.Up, "T_Panel_ObjectScrollBar_Horizontal");
        // 클릭 대기
        click = false;
        yield return new WaitUntil(() => click);
        RemoveHighlight(item);
        SoundManager.Instance.PlayClickUI();

        HideGuideAnimation();

        //지문 대사 추가
        List<string> texts = new List<string>() { "InGamePieceTransfer1" };
        bool done = false;
        SetTyping(texts, () => done = true);
        //지문 대사 종료 대기
        yield return new WaitUntil(() => done);


        GameObject item2 = FindChildRecursive(screen.gameObject, "Button_Sampler");
        Button btn = item2.GetComponent<Button>();
        HighlightUIObject(item2);
        ShowGuideAnimation(item2, PointingDirection.Down, PointingDirection.Down, "T_Button_Sampler2");
        // 클릭 대기
        bool _click = false;
        UnityAction tempACtion = () => _click = true;
        btn.onClick.AddListener(tempACtion);

        act_Skip = () =>
        {
            if (caster != null) caster.enabled = true;
            if (btn != null) btn.onClick.RemoveListener(tempACtion);
        };

        yield return new WaitUntil(() => _click);
        btn.onClick.RemoveListener(tempACtion);
        RemoveHighlight(item2);

        //다음 동작 대기
        CompleteCurrentStep(currentStep);

        act_Skip = null;

        //퍼즐 드래그앤 드롭을 알려줌
        ShowRepetitiveMoveAnimation(puzzlePieceScrollView.GetFirstActive(), 132.0f, new Vector2(180.0f, 480.0f), 2.0f, 2);
        click = false;

        // 퍼즐 조각 이동 가이드 로직 구현
        yield return null;
    }

    private IEnumerator Tutorial_ViewResultUI()
    {
        Debug.Log("튜토리얼 단계 시작: 결과 UI 확인");

        //퍼즐 종료 대기
        //yield return new WaitUntil(() => PuzzleContainer.Container.GetProgress == 1.0f);

        //퍼즐 결과창 대기
        yield return new WaitUntil(() => UIScreenManager.Instance.GetUI(UIName.RESULT_UI) != null);

        // 입력 방지
        var screen = UIScreenManager.Instance.GetUI(UIName.RESULT_UI);
        Canvas canvas = screen.AddComponent<Canvas>();

        // 스킵 시 캔버스 제거
        act_Skip = () =>
        {
            if (canvas != null) Destroy(canvas);
        };

        // 애니메이션 대기
        yield return new WaitForSeconds(4.0f);

        Destroy(screen.GetComponent<Canvas>());

        act_Skip = null;

        ShowDimmer();

        //지문 대사 추가
        List<string> texts = new List<string>() { "ViewResultUI1", "ViewResultUI2", "ViewResultUI3" }; 
        bool done = false;
        SetTyping(texts, () => done = true);

        yield return new WaitUntil(() => done);


        //코인 추가 지급
        PlayerData.TryAddCoin(150);


        //다음 동작 대기
        CompleteCurrentStep(currentStep);

        //지문 대사 종료 대기
    }

    #endregion

    #region UI 강조 효과 함수

    /// <summary>
    /// 특정 UI GameObject를 최상단에 그려지도록 하여 강조합니다. (상호작용 가능)
    /// </summary>
    /// <param name="target">강조할 UI GameObject</param>
    public void HighlightUIObject(GameObject target)
    {
        if (target == null) return;

        Canvas canvas = target.GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = target.AddComponent<Canvas>();
        }
        canvas.overrideSorting = true;
        canvas.sortingOrder = 100;

        // 상호작용을 위해 GraphicRaycaster 추가
        if (target.GetComponent<GraphicRaycaster>() == null)
        {
            target.AddComponent<GraphicRaycaster>();
        }
        
        // 정리를 위해 현재 강조된 오브젝트를 추적합니다.
        currentHighlightedObject = target;
    }

    /// <summary>
    /// UI GameObject의 강조 효과를 제거합니다.
    /// </summary>
    /// <param name="target">강조 효과를 제거할 UI GameObject</param>
    public void RemoveHighlight(GameObject target)
    {
        if (target == null) return;

        currentHighlightedObject = null;

        GraphicRaycaster raycaster = target.GetComponent<GraphicRaycaster>();
        if (raycaster != null)
        {
            Destroy(raycaster);
        }

        Canvas canvas = target.GetComponent<Canvas>();
        if (canvas != null)
        {
            Destroy(canvas);
        }
    }

    #endregion

    #region 가이드 UI(손가락, 지문) 제어 함수

    /// <summary>
    /// 지정된 방향과 인스펙터에 설정된 값으로 가이드 애니메이션을 시작합니다.
    /// </summary>
    /// <param name="target">가리킬 대상 오브젝트</param>
    /// <param name="fingerDirection">손가락이 위치할 방향</param>
    /// <param name="descDirection">지문이 위치할 방향</param>
    /// <param name="descText">표시할 설명 문장</param>
    public void ShowGuideAnimation(GameObject target, PointingDirection fingerDirection, PointingDirection descDirection, string descText)
    {
        if (target == null || fingerObject == null)
        {
            Debug.LogWarning("가이드 UI에 필요한 Target 또는 Finger 오브젝트가 설정되지 않았습니다.");
            return;
        }

        if (guideSetting == null)
        {
            Debug.LogError("GuideSetting이 인스펙터에 설정되지 않았습니다.");
            return;
        }

        //설명 문구 지정
        if(descTextMesh != null)
        {
            descTextMesh.text = Localization.Localize(descText);
            descriptionObject.SetActive(true);
            LayoutRebuilder.ForceRebuildLayoutImmediate(descriptionObject.GetComponent<RectTransform>());
        }

        // 2. 애니메이션 시작
        if (guideAnimationCoroutine != null)
        {
            StopCoroutine(guideAnimationCoroutine);
        }
        StopRepetitiveMoveAnimation();

        fingerObject.SetActive(true);

        // 파라미터로 받은 방향(Direction)과 인스펙터의 설정값(Offset, Range)을 조합하여 코루틴 시작
        guideAnimationCoroutine = StartCoroutine(AnimateGuide(target, fingerDirection, guideSetting.fingerOffset, guideSetting.fingerMovementRange, descDirection, guideSetting.descOffset));
    }

    /// <summary>
    /// 손가락 및 지문 가이드 UI를 모두 숨깁니다.
    /// </summary>
    public void HideGuideAnimation()
    {
        if (guideAnimationCoroutine != null)
        {
            StopCoroutine(guideAnimationCoroutine);
            guideAnimationCoroutine = null;
        }

        if (repetitiveMoveCoroutine != null)
        {
            StopCoroutine(repetitiveMoveCoroutine);
            repetitiveMoveCoroutine = null;
        }

        if (fingerObject != null) fingerObject.SetActive(false);
        if (descriptionObject != null) descriptionObject.SetActive(false);
    }
    
    /// <summary>
    /// 가이드 UI(A, B)를 움직이고 배치하는 코루틴입니다.
    /// </summary>
    private IEnumerator AnimateGuide(GameObject target, PointingDirection fingerDirection, float fingerOffset, float fingerMovementRange, PointingDirection descDirection, float descOffset)
    {
        //배경 활성화
        ShowDimmer();

        SoundManager.Instance.PlayEffect($"Talk{UnityEngine.Random.Range(1, 3)}");

        RectTransform targetRect = target.GetComponent<RectTransform>();
        RectTransform fingerRect = fingerObject.GetComponent<RectTransform>();
        RectTransform descRect = descriptionObject.activeSelf ? descriptionObject.GetComponent<RectTransform>() : null;

        // 월드 코너를 계산하여 타겟의 월드 기준 크기와 중심을 구합니다.
        Vector3[] corners = new Vector3[4];
        targetRect.GetWorldCorners(corners);
        // corners[0]: Bottom-Left, [1]: Top-Left, [2]: Top-Right, [3]: Bottom-Right
        Vector3 worldSize = corners[2] - corners[0];
        Vector3 targetCenter = (corners[0] + corners[2]) / 2f;

        // 1. 방향에 따른 손가락(A)의 '기본' 위치 계산
        Vector3 fingerBasePos = Vector3.zero;

        switch (fingerDirection)
        {
            case PointingDirection.Up:
                fingerBasePos = new Vector3((corners[1].x + corners[2].x) / 2, corners[1].y + fingerOffset, 0);
                break;
            case PointingDirection.Down:
                fingerBasePos = new Vector3((corners[0].x + corners[3].x) / 2, corners[0].y - fingerOffset, 0);
                break;
            case PointingDirection.Left:
                fingerBasePos = new Vector3(corners[0].x - fingerOffset, (corners[0].y + corners[1].y) / 2, 0);
                break;
            case PointingDirection.Right:
                fingerBasePos = new Vector3(corners[2].x + fingerOffset, (corners[2].y + corners[3].y) / 2, 0);
                break;
        }

        // 2. 지문(B)의 위치를 손가락의 '기본' 위치 기준으로 한번만 설정
        if (descriptionObject.activeSelf && descRect != null)
        {
            Vector3 descPos = fingerBasePos;
            // 방향에 따른 오프셋과 피봇 보정을 함께 적용합니다.
            switch (descDirection)
            {
                case PointingDirection.Up:
                    descPos += new Vector3(0, descOffset, 0);  // Up: Y축 양의 방향
                    descPos.y += descRect.rect.height / 2;      // 피봇이 중앙일 경우, 박스 하단에 맞추기 위해 높이의 절반을 더함
                    break;
                case PointingDirection.Down:
                    descPos += new Vector3(0, -descOffset, 0); // Down: Y축 음의 방향
                    descPos.y -= descRect.rect.height / 2;      // 피봇이 중앙일 경우, 박스 상단에 맞추기 위해 높이의 절반을 뺌
                    break;
                case PointingDirection.Left:
                    descPos += new Vector3(-descOffset, 0, 0); // Left: X축 음의 방향
                    descPos.x -= descRect.rect.width / 2;       // 피봇이 중앙일 경우, 박스 우측에 맞추기 위해 너비의 절반을 뺌
                    break;
                case PointingDirection.Right:
                    descPos += new Vector3(descOffset, 0, 0);  // Right: X축 양의 방향
                    descPos.x += descRect.rect.width / 2;       // 피봇이 중앙일 경우, 박스 좌측에 맞추기 위해 너비의 절반을 더함
                    break;
            }
            descRect.position = descPos;
        }

        // 3. 손가락(A)만 움직이는 애니메이션 루프 시작
        while (true)
        {
            // 손가락의 반복 움직임(PingPong) 계산
            float move = (fingerMovementRange / 2f) * Mathf.Sin(Time.time * 5f); // -m/2 ~ +m/2 범위로 움직임
            Vector3 fingerMoveOffset = Vector3.zero;

            switch (fingerDirection)
            {
                case PointingDirection.Up: fingerMoveOffset = new Vector3(0, move, 0); break;
                case PointingDirection.Down: fingerMoveOffset = new Vector3(0, -move, 0); break;
                case PointingDirection.Left: fingerMoveOffset = new Vector3(-move, 0, 0); break;
                case PointingDirection.Right: fingerMoveOffset = new Vector3(move, 0, 0); break;
            }

            fingerRect.position = fingerBasePos + fingerMoveOffset;

            // 손가락이 대상 오브젝트를 바라보도록 회전
            Vector3 dirToTarget = (targetCenter - fingerRect.position).normalized;
            float angle = Mathf.Atan2(dirToTarget.y, dirToTarget.x) * Mathf.Rad2Deg;
            fingerRect.rotation = Quaternion.Euler(0, 0, angle); // 스프라이트 방향에 따라 각도 조절 필요

            yield return null; // 다음 프레임까지 대기
        }
    }


    #endregion

    #region 배경(Dimmer) 제어 함수

    /// <summary>
    /// 배경을 가리는 UI(C 오브젝트)를 활성화합니다.
    /// </summary>
    public void ShowDimmer()
    {
        if (backgroundDimmer != null)
        {
            backgroundDimmer.SetActive(true);
        }
    }


    /// <summary>
    /// 타이핑 지문을 사용합니다.
    /// </summary>
    /// <param name="descriptions"></param>
    public void SetTyping(List<string> descriptions, Action onComplete)
    {
        // 1. 설명 텍스트 나레이션 시작
        if (typingEffect != null)
        {
            //ShowMascott();
            // descriptionObject 자체의 활성화/비활성화는 TypingEffectUI가 담당합니다.
            typingEffect.StartNarration(descriptions);
            typingEffect.OnCompleted = onComplete;
        }
    }


    /// <summary>
    /// 배경을 가리는 UI(C 오브젝트)를 비활성화합니다.
    /// </summary>
    public void HideDimmer()
    {
        if (backgroundDimmer != null)
        {
            backgroundDimmer.SetActive(false);
        }
    }

    #endregion
    
    /// <summary>
    /// 튜토리얼과 관련된 모든 UI 요소를 숨깁니다.
    /// </summary>
    private void HideAllGuideElements()
    {
        HideGuideAnimation();
        HideDimmer();
        skipObject.SetActive(false);
    }

    /// <summary>
    /// 스킵
    /// </summary>
    public void OnClickSkip()
    {
        //# 여기서는 튜토리얼 진행 중 강제로 설정된 컴포넌트 설정을 원상복구 시키는 액션함수를 실행합니다.
        act_Skip?.Invoke();

        //현재 단계 저장
        currentStep = TutorialStep.Done;
        CompleteCurrentStep(TutorialStep.Done);
        SoundManager.Instance.PlayClickUI();
    }

    /// <summary>
    /// 지정된 부모 GameObject부터 시작하여 모든 자식 및 하위 자식들을 재귀적으로 탐색하여 특정 이름을 가진 GameObject를 찾습니다.
    /// </summary>
    /// <param name="parent">탐색을 시작할 부모 GameObject</param>
    /// <param name="name">찾고자 하는 GameObject의 이름</param>
    /// <returns>이름이 일치하는 첫 번째 GameObject를 반환합니다. 찾지 못하면 null을 반환합니다.</returns>
    public static GameObject FindChildRecursive(GameObject parent, string name)
    {
        if (parent == null) return null;

        foreach (Transform child in parent.transform)
        {
            // 현재 자식의 이름이 일치하는지 확인
            if (child.name == name)
            {
                return child.gameObject;
            }

            // 현재 자식을 기준으로 재귀적으로 탐색
            GameObject found = FindChildRecursive(child.gameObject, name);
            if (found != null)
            {
                return found;
            }
        }

        // 모든 자식을 탐색했지만 찾지 못한 경우
        return null;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!click)
        {
            StopRepetitiveMoveAnimation();
        }

        click = true;
    }

    /// <summary>
    /// 지정된 위치로 핑거가 N회 T초 간격으로 움직이는 애니메이션을 표시합니다.
    /// </summary>
    /// <param name="target">애니메이션의 기준이 될 GameObject</param>
    /// <param name="rotationZ">핑거의 Z축 회전 값</param>
    /// <param name="additionalVector">Target 위치에 추가할 변위 값</param>
    /// <param name="interval">한번의 왕복 이동에 걸리는 시간 (초)</param>
    /// <param name="count">반복 횟수</param>
    public void ShowRepetitiveMoveAnimation(GameObject target, float rotationZ, Vector2 additionalVector, float interval, int count)
    {
        // 다른 모든 가이드 애니메이션을 먼저 중지시킵니다.
        HideGuideAnimation();

        // 새로운 반복 이동 애니메이션을 시작합니다.
        repetitiveMoveCoroutine = StartCoroutine(AnimateRepetitiveMove(target, rotationZ, additionalVector, interval, count));
    }

    void StopRepetitiveMoveAnimation()
    {
        if (repetitiveMoveCoroutine != null)
        {
            StopCoroutine(repetitiveMoveCoroutine);
            repetitiveMoveCoroutine = null;
        }
    }

    public void OnClickActiveSkip()
    {
        skipObject?.SetActive(true);
        SoundManager.Instance.PlayClickUI();
    }

    /// <summary>
    /// 핑거가 지정된 시간동안 목표 지점으로 이동하는 동작을 N회 반복하는 애니메이션 코루틴입니다.
    /// </summary>
    private IEnumerator AnimateRepetitiveMove(GameObject target, float rotationZ, Vector2 additionalVector, float interval, int count)
    {
        fingerObject.SetActive(true);
        fingerObject.transform.rotation = Quaternion.Euler(0, 0, rotationZ);

        Vector3 startPos = target.transform.position;
        Vector3 endPos = startPos + (Vector3)additionalVector;

        for (int i = 0; i < count; i++)
        {
            // 매 반복마다 시작 위치에서 다시 시작합니다.
            fingerObject.transform.position = startPos;

            // --- startPos에서 endPos로 지정된 시간(interval) 동안 부드럽게 이동 ---
            float elapsedTime = 0f;
            while (elapsedTime < interval)
            {
                float t = elapsedTime / interval;
                // SmoothStep을 사용하여 가속/감속 효과 적용
                fingerObject.transform.position = Vector3.Lerp(startPos, endPos, Mathf.SmoothStep(0.0f, 1.0f, t));
                elapsedTime += Time.deltaTime;
                yield return null; // 다음 프레임까지 대기
            }
            fingerObject.transform.position = endPos; // 정확한 최종 위치 보정
        }

        fingerObject.SetActive(false);
        repetitiveMoveCoroutine = null;
    }


    /// <summary>
    /// 특정 UI의 하위 TextMeshProUGUI 컴포넌트들의 폰트를 변경합니다.
    /// </summary>
    private void ApplyFontToUI(GameObject ui)
    {
        // 비활성화된 객체까지 포함하여 모든 텍스트 컴포넌트 검색
        var texts = ui.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (var text in texts)
        {
            text.text = Localization.Localize(text.text);
        }
    }

    /// <summary>
    /// 마스코트 2개 중 1개 활성화
    /// </summary>
    public void ShowMascott()
    {
        CustomDebug.Print("마스코트 변경");
        foreach(var item in mascotts)
        {
            item.gameObject.SetActive(false);
        }

        mascotts[UnityEngine.Random.Range(0, mascotts.Length)].gameObject.SetActive(true);    
    }
}
