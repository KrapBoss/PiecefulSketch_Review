using Custom;
using System;
using System.Threading.Tasks;
using UnityEngine;

public class SaveDataChecker : MonoBehaviour
{
    private static SaveDataChecker _instance;

    // [핵심] 호출 시점에 인스턴스가 없으면 스스로 생성 (Lazy Instantiation)
    public static SaveDataChecker Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<SaveDataChecker>();
                if (_instance == null)
                {
                    CustomDebug.PrintW("세이브 체커 생성");
                    GameObject go = new GameObject("# SaveDataChecker");
                    _instance = go.AddComponent<SaveDataChecker>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }

    [Header("Settings")]
    [Tooltip("자동 저장 주기 (초)")]
    [SerializeField] private const float _autoSaveInterval = 300.0f;

    /// <summary> 제한 시간 초기화  </summary>
    public void ImmediatelySave()
    {
        _timer = _autoSaveInterval;
    }

    /// <summary> 저장이 타 기능에서 되었음 </summary>
    public void ImmediatelySaved()
    {
        _timer = 0;
    }

    // 내부 변수
    private float _timer = 0f;
    private bool _isSaving = false; // 현재 저장 중인지 확인하는 플래그

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // [변경점] 코루틴 대신 Update에서 시간 체크
    private void Update()
    {
        // 이미 저장 중이라면 대기
        if (_isSaving) return;

        // 플래그가 없다면, 저장 대기
        if (!SaveDataManager.IsDirty) _timer = 0;

        //플래그가 있다면 저장 실행
        if (SaveDataManager.IsDirty)
        {
            // 타이머 증가 (Time.timeScale에 영향받지 않도록 unscaledDeltaTime 사용 권장)
            _timer += Time.unscaledDeltaTime;

            // 주기 도달
            if (_timer >= _autoSaveInterval)
            {
                _timer = 0f; // 타이머 리셋
                _= ProcessSaveAsync();
            }
        }
    }

    /// <summary>
    /// [기능] 비동기 저장 실행 (Update에서 호출)
    /// [제작 의도] try-finally 블록을 적용하여 어떠한 예외 상황에서도 플래그가 원상복구 되도록 강제함
    /// [Unity 효율성] 논리적 데드락(State Lock) 방지
    /// </summary>
    private async Task ProcessSaveAsync()
    {
        _isSaving = true; // 저장 시작 플래그 On

        try
        {
            // Task 완료 대기
            await SaveDataManager.UploadToCloudAsync();
        }
        finally
        {
            // [핵심 방어] 업로드 중 에러가 발생해도 무조건 저장 플래그를 해제하여 시스템 멈춤 방지
            _isSaving = false;
        }
    }

    /// <summary>
    /// [기능] 앱 일시정지 시 (홈 버튼 등)
    /// [제작 의도] 백그라운드 진입 시 불안정한 네트워크 업로드를 차단하여 소켓 증발로 인한 무한 대기 방지
    /// [Unity 효율성] 이미 로컬 저장은 완료된 상태이므로, 앱 복귀 시 안전하게 동기화되도록 우회
    /// </summary>
    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus && SaveDataManager.IsDirty && !_isSaving)
        {
            // 앱으로 복귀(Resume)했을 때 데이터가 남아있다면 저장을 시도합니다.
            _=ProcessSaveAsync();
        }
    }

    // [안전장치 2] 앱 종료 시
    private void OnApplicationQuit()
    {
        // 앱 종료 시점의 안전장치 없음 (로컬 저장에 의존)
    }

    /// <summary> 저장해야되는 상태인가? </summary>
    /// <returns></returns>
    public bool CheckSaving()
    {
        return SaveDataManager.IsDirty && !_isSaving;
    }

    /// <summary>
    /// 외부 저장 기능
    /// </summary>
    /// <param name="act"> 저장 종료 후 실행할 동작 </param>
    /// <returns></returns>
    public async Task SaveDataOutter(Action act)
    {
        await ProcessSaveAsync();

        if (act != null) act.Invoke();
    }
}