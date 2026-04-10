using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ShowSampleImage : UIBehaviour, IOnOff
{
    public Image imageSample;
    public RectTransform rect_sizer;
    public MoveUiWithBound cs_moveBound;    // 샘플 이미지 움직임 담당
    public RectTransform rectFrame;
    public float scaleOffset = 1.8f;    // 추가적으로 부여할 사이즈 크기
    public float scaleSize = 0.5f;      // 사용자가 조절하는 사이즈
    public UISliderPack cs_sliderPack;      // 슬라이더 값 조절자
    public UIAutoFadeWithImage cs_autoFade; // 자동 투명화

    [SerializeField] Sprite texture;
    [SerializeField] float zoomOffsetValue = 0.025f;
    [SerializeField] float smoothZoomSpeed = 5.0f; // [추가] 부드러운 줌 조절 속도

    private Capture2D capture;
    private Vector2 _baseSizeDelta; // 슬라이더 적용 전 기준 크기
    private float _targetScaleSize; // [추가] 스크롤 시 도달할 목표 스케일 값
    bool first = false;

    public bool Active { get; set; }

    protected override void Awake()
    {
        base.Awake();
        capture = FindAnyObjectByType<Capture2D>();
    }

    /// <summary>
    /// [기능] 매 프레임 입력 검사 및 이미지 스케일 조절
    /// [제작 의도] 사용자의 더블 터치 줌 입력 시 목표 수치를 계산하고 부드럽게(Lerp) 슬라이더에 적용하기 위함
    /// [Unity 효율성] Update 내에서 무거운 연산을 배제하고, 입력이 있을 때만 보간 연산을 수행하여 프레임 드랍 방지
    /// </summary>
    private void Update()
    {
        if (!Active) return;
        if (!InputManager.Instance) return;

#if UNITY_EDITOR
        //이미지 스크롤 진행 (사용자 직접 입력 조건)
        if (InputManager.Instance.input.CompareDoubleTouch1Name(imageSample.gameObject.name) && Mathf.Abs(InputManager.Instance.input.Scroll) > 0.01f)
#else
        //이미지 스크롤 진행 (사용자 직접 입력 조건)
        if (InputManager.Instance.input.CompareDoubleTouch1Name(imageSample.gameObject.name))
#endif
        {
            InputManager.Instance.input.UseOtherScroll = true;  // 줌인 아웃을 이미 사용 중임을 표기
            if (cs_moveBound) cs_moveBound.StopDragging();

            // 1. 스크롤 델타값을 구하여 목표(Target) 사이즈 누적 계산
            float scrollDelta = InputManager.Instance.input.Scroll * (CameraController.Instance == null ? 1 : CameraController.Instance.ScrollSensitivity) * zoomOffsetValue;

            if (Mathf.Abs(scrollDelta) > 0.0001f)
            {
                _targetScaleSize -= scrollDelta;
            }

            // 2. 현재 스케일에서 목표 스케일로 부드럽게 이동 (Smooth Lerp)
            float smoothedScale = Mathf.Lerp(scaleSize, _targetScaleSize, Time.deltaTime * smoothZoomSpeed);

            // 3. 슬라이더에 값 세팅 (이로 인해 UpdateImageScale 이벤트가 정상 호출됨)
            cs_sliderPack.SetValue(smoothedScale);
        }
        else
        {
            InputManager.Instance.input.UseOtherScroll = false;

            // [안전장치] 터치가 끝났을 때 현재 배율을 목표 배율로 덮어씌워 다음 터치 시 튀는 현상 방지
            _targetScaleSize = scaleSize;
        }
    }

    /// <summary>
    /// [기능] 화면 크기 변경 이벤트 처리
    /// [제작 의도] 해상도나 화면 비율 변경 시 샘플 이미지 UI를 재조정하기 위함
    /// [Unity 효율성] 이벤트 콜백 기반으로 동작하여 불필요한 매 프레임 해상도 체크 생략
    /// </summary>
    protected override void OnRectTransformDimensionsChange()
    {
        base.OnRectTransformDimensionsChange();

        first = false;
        if (Active) On();

        if (CameraController.Instance != null) CameraController.Instance.ResizeCamera();
        Debug.Log($"화면 크기 변경 감지: {Screen.width}x{Screen.height}");
    }

    /// <summary>
    /// [기능] 샘플 이미지 UI 비활성화
    /// [제작 의도] 이벤트 해제 및 UI 숨김 처리
    /// [Unity 효율성] 등록된 리스너를 명확히 제거하여 메모리 누수(Leak) 및 중복 호출 방지
    /// </summary>
    public bool Off()
    {
        // 슬라이더 이벤트 해제 (메모리 누수 및 중복 방지)
        if (cs_sliderPack != null)
        {
            cs_sliderPack.RemoveOnValueChangedListener(UpdateImageScale);
        }

        this.gameObject.SetActive(false);
        Active = false;
        InputManager.Instance.input.UseOtherScroll = false;
        //cs_moveBound?.SetLock(false);
        return true;
    }

    /// <summary>
    /// [기능] 샘플 이미지 UI 활성화 및 초기화
    /// [제작 의도] 캡처된 텍스처를 UI에 맞게 비율을 계산하고 슬라이더 이벤트를 연결하기 위함
    /// [Unity 효율성] 텍스처 비율에 따른 기준 크기(BaseSize)를 한 번만 계산하여 Update 연산 최소화
    /// </summary>
    public bool On()
    {
        texture = capture.Picture;
        if (texture == null) return false;

        this.gameObject.SetActive(true);
        Active = true;

        // 1. 기준 사이즈 계산 (프레임 기준)
        float frameSizeY = rectFrame.rect.height * 0.6f;
        float frameSizeX = rectFrame.rect.width;
        float frameRatio = frameSizeX / frameSizeY;

        float sizeX = texture.bounds.size.x;
        float sizeY = texture.bounds.size.y;
        float ratio = sizeX / sizeY;

        //scaleSize = GameSetting.SampleSize;

        if (!first)
        {

            scaleSize = 1.0f / scaleOffset;
            //rect_sizer.anchoredPosition = Vector2.zero;
        }

        cs_autoFade.ResetFade();


        if (ratio > frameRatio)
        {
            _baseSizeDelta = new Vector2(frameSizeX, frameSizeX / ratio) * scaleOffset;
        }
        else
        {
            _baseSizeDelta = new Vector2(frameSizeY * ratio, frameSizeY) * scaleOffset;
        }

        // 2. 슬라이더 배율이 적용되지 않은 '순수 기준 크기' 저장

        // 3. 슬라이더 이벤트 등록 및 초기 스케일 적용
        if (cs_sliderPack != null)
        {
            cs_sliderPack.RemoveOnValueChangedListener(UpdateImageScale); // 중복 등록 방지
            cs_sliderPack.AddOnValueChangedListener(UpdateImageScale);

            _targetScaleSize = scaleSize; // [추가] 초기화 시 목표 스케일 동기화
            cs_sliderPack.SetValue(scaleSize);

            // 현재 슬라이더 값을 즉시 반영하여 크기 설정
            UpdateImageScale(scaleSize);
        }
        else
        {
            // 슬라이더가 없을 경우 기본 크기 적용
            rect_sizer.sizeDelta = _baseSizeDelta;
        }

        imageSample.sprite = texture;


        if (!first)
        {
            cs_moveBound.SetPosition(new Vector2(0, _baseSizeDelta.y * -0.5f * scaleSize));

            first = true;
        }


        return true;
    }

    /// <summary>
    /// [기능] 슬라이더 값(배율)에 따라 이미지의 크기를 실시간으로 변경
    /// [제작 의도] 슬라이더 직접 조작 및 Update에서의 Lerp 조작 시 최종적으로 이미지 크기를 반영
    /// [Unity 효율성] Vector2 연산을 최소화하여 할당(Allocation) 없는 크기 갱신 처리
    /// </summary>
    /// <param name="scaleValue">슬라이더에서 전달된 배율 값</param>
    private void UpdateImageScale(float scaleValue)
    {
        if (!Active || imageSample == null) return;

        scaleSize = scaleValue;

        // [핵심 해결] 사용자가 UI 슬라이더를 직접 드래그(1순위 이벤트)할 때 목표값을 즉시 동기화시켜 Update의 Lerp 연산과 충돌하는 현상 방지
        _targetScaleSize = scaleValue;

        GameSetting.SampleSize = scaleSize;
        // 기준 크기(baseSizeDelta)에 슬라이더 값(scaleValue)을 곱하여 최종 크기 결정
        rect_sizer.sizeDelta = _baseSizeDelta * scaleSize;
    }
}