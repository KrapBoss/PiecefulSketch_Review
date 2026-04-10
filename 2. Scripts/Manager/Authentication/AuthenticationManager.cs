using Custom;
using System;
using System.Collections;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

// [제작 의도] 인증 프로세스만 전담하는 매니저 (UI 로직 없음)
public class AuthenticationManager : MonoBehaviour
{
    private IAuthProvider _authProvider;
    private int _retryCount = 0;
    private const int MaxRetries = 2;

    // 로그인 진행 중인지 확인 (중복 요청 방지)
    public bool IsLoggingIn { get; private set; }

    // 백킹 필드 (실제 인스턴스 저장용)
    private static AuthenticationManager _instance;

    // [핵심 변경] 호출 시점에 인스턴스가 없으면 생성
    public static AuthenticationManager Instance
    {
        get
        {
            // 인스턴스가 아직 없는 경우
            if (_instance == null)
            {
                // 1. 혹시 씬에 이미 존재하는지 찾기
                _instance = FindObjectOfType<AuthenticationManager>();

                // 2. 씬에도 없다면 새로 생성
                if (_instance == null)
                {
                    GameObject go = new GameObject("#AuthenticationManager");
                    _instance = go.AddComponent<AuthenticationManager>();

                    // 생성 즉시 파괴 방지 설정
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }
    // [핵심 기능] 외부에서 호출 가능한 로그인 함수
    // onComplete 콜백: (성공 여부, 메시지)
    public async void LoginProcess(Action<bool, string> onComplete)
    {
        if (IsLoggingIn) return; // 이미 진행 중이면 패스


        IsLoggingIn = true;
        _retryCount = 0;

        //await UIScreenManager.Instance.ShowUI(UIName.LOADING_UI);

        // 1. 네트워크 체크
        if (!NetworkMonitor.IsConnected)
        {
            IsLoggingIn = false;
            onComplete?.Invoke(false, "Network Disconnected");
            //UIScreenManager.Instance.CloseUI(UIName.LOADING_UI);
            return;
        }

        try
        {
            // 1. 초기화 상태 체크
            if (UnityServices.State == ServicesInitializationState.Uninitialized)
            {
                // 옵션을 비워서 기본 설정으로 시도
                var options = new InitializationOptions();
                await UnityServices.InitializeAsync(options);

                Debug.Log($"UGS 초기화 성공: {UnityServices.State}");
            }
        }
        catch (ServicesInitializationException ex)
        {
            // UGS 전용 초기화 에러 (프로젝트 ID 누락 등)
            Debug.LogError($"UGS 서비스 에러:- {ex.InnerException.Message} - {ex.Message}");
        }
        catch (Exception e)
        {
            Debug.LogError($"UGS 서비스 에러:- {e.InnerException.Message} - {e.Message}");
        }

        // 3. 에디터 환경 처리: 익명 로그인(Guest)으로 즉시 진행
#if UNITY_EDITOR
        Debug.Log("[AuthenticationManager] Editor 환경 감지: 익명 로그인을 진행합니다.");
        SignInAnonymously(onComplete);
        return;
#endif

        Debug.Log("[Authentication] : Start Login");
        // 4. Provider 생성 및 로그인 시작
        _authProvider = AuthProviderFactory.CreateProvider();
        if (_authProvider == null)
        {
            IsLoggingIn = false;
            onComplete?.Invoke(false, "Unsupported Platform (Editor?)");
            //UIScreenManager.Instance.CloseUI(UIName.LOADING_UI);
            return;
        }

        _authProvider.Initialize();
        StartCoroutine(TryPlatformLoginRoutine(onComplete));
    }

    // [에디터 전용] 익명 로그인 처리
    private async void SignInAnonymously(Action<bool, string> onComplete)
    {
        try
        {
            if (AuthenticationService.Instance.IsSignedIn)
            {
                AuthenticationService.Instance.SignOut();
            }

            await AuthenticationService.Instance.SignInAnonymouslyAsync();

            Debug.Log("[AuthenticationManager] 익명 로그인 성공");
            IsLoggingIn = false;
            onComplete?.Invoke(true, "Success (Anonymous)");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[AuthenticationManager] 익명 로그인 실패: {ex.Message}");
            IsLoggingIn = false;
            onComplete?.Invoke(false, $"Anonymous Login Failed: {ex.Message}");
        }
    }

    // 내부 로직: 플랫폼 로그인 -> UGS 로그인
    private IEnumerator TryPlatformLoginRoutine(Action<bool, string> onComplete)
    {
        Debug.Log("[Authentication] : Try Login");

        // 플랫폼 로그인 요청
        bool isDone = false;
        string authCode = null;
        string errorMsg = null;

        _authProvider.SignIn(
            onSuccess: (code) => { authCode = code; isDone = true; },
            onFail: (msg) => { errorMsg = msg; isDone = true; }
        );

        // 콜백 대기
        yield return new WaitUntil(() => isDone);

        if (authCode != null)
        {
            // 플랫폼 성공 -> UGS 로그인 시도
            LoginToUGS(authCode, onComplete);
        }
        else
        {
            // 실패 -> 재시도 로직
            Debug.LogWarning($"Platform Login Failed: {errorMsg}");
            yield return StartCoroutine(RetryRoutine(onComplete));
        }
    }

    // 내부 로직: UGS 연결
    private async void LoginToUGS(string token, Action<bool, string> onComplete)
    {
        try
        {
            if (AuthenticationService.Instance.IsSignedIn)
            {
                AuthenticationService.Instance.SignOut();
            }

            //로컬에 저장된 세션 토큰을 삭제하여 새로운 PlayerId를 생성할 수 있는 상태로 만듦
            //AuthenticationService.Instance.ClearSessionToken();

            //PlayerAccountService.Instance.AccessToken
#if UNITY_ANDROID
            await AuthenticationService.Instance.SignInWithGooglePlayGamesAsync(token);
#elif UNITY_IOS
            await AuthenticationService.Instance.SignInWithAppleAsync(token);
#endif

            // 최종 성공
            IsLoggingIn = false;
            onComplete?.Invoke(true, "Success");
            //UIScreenManager.Instance.CloseUI(UIName.LOADING_UI);
        }
        catch (Exception ex)
        {
            Debug.LogError($"UGS Login Error: {ex.Message} : {ex.InnerException.Message}");
            // UGS 실패 시에도 재시도 시도
            StartCoroutine(RetryRoutine(onComplete));
        }
    }

    // 재시도 로직
    private IEnumerator RetryRoutine(Action<bool, string> onComplete)
    {
        if (_retryCount < MaxRetries)
        {
            _retryCount++;
            Debug.Log($"Retrying login... ({_retryCount}/{MaxRetries})");
            yield return new WaitForSeconds(1.0f);

            // 다시 처음부터 시도 (재귀적 호출 대신 코루틴 재진입)
            StartCoroutine(TryPlatformLoginRoutine(onComplete));
        }
        else
        {
            IsLoggingIn = false;
            onComplete?.Invoke(false, "Max Retries Reached");
            //UIScreenManager.Instance.CloseUI(UIName.LOADING_UI);
        }
    }


    #region GET PLAYER GUID


    private const string KEY_LOCAL_UUID = "LocalUserUUID";
    private static string _uuid = string.Empty;

    // [제작 의도] 네트워크가 없는 첫 접속 시에도 데이터 소유권을 식별할 고유 ID 확보
    // [효율성] 한 번 생성된 ID는 보안 저장소에 보관하여 재사용함

    /// <summary>
    /// 현재 사용 가능한 최적의 PlayerId를 반환합니다.
    /// </summary>
    public static string GetPlayerId()
    {
        try
        {
            // 1. UGS 로그인이 되어 있다면 UGS ID 우선 사용
            if (AuthenticationService.Instance != null && AuthenticationService.Instance.IsSignedIn)
            {
                _uuid = AuthenticationService.Instance.PlayerId;
                CustomDebug.Print($"UUID1 : {_uuid}");
                return _uuid;
            }

            // 2. 오프라인일 경우 로컬에 저장된 UUID 확인
            LoadUUID();

            if (string.IsNullOrEmpty(_uuid))
            {
                // 3. 최초 접속 & 오프라인일 경우 새로 생성
                _uuid = GenerateSafeId();
                SaveUUID();
            }

            CustomDebug.Print($"UUID2 : {_uuid}");
        }
        catch (Exception e)
        {
            // 2. 오프라인일 경우 로컬에 저장된 UUID 확인
            LoadUUID();
            CustomDebug.PrintE("Temp UUID : " + _uuid +" | " + GenerateSafeId());

            if (string.IsNullOrEmpty(_uuid))
            {
                // 3. 최초 접속 & 오프라인일 경우 새로 생성
                _uuid = GenerateSafeId();
                SaveUUID();
            }

            CustomDebug.Print($"UUID3 : {_uuid} : {e.Message}");
        }

        return _uuid;
    }

    /// <summary>
    /// 로그인 되어있다는 가정 하에 로컬 내 GUID 데이터 변경을 위한 것
    /// </summary>4
    public static void OverrideGUID()
    {
        try
        {
            // 1. UGS 로그인이 되어 있다면 UGS ID 우선 사용
            if (AuthenticationService.Instance.IsSignedIn)
            {
                Debug.LogWarning("플레이어 GUID 를 익명이 아닌 실제 로그인된 GUID로 갱신합니다");
                _uuid = AuthenticationService.Instance.PlayerId;
                SaveUUID();
            }
        }
        catch(Exception e)
        {
            Debug.LogError("OverrideGUID : " + e.Message);
        }
    }

    public static void Clear()
    {
        PlayerPrefs.DeleteKey(KEY_LOCAL_UUID);
    }

    private static void SaveUUID()
    {
        PlayerPrefs.SetString(KEY_LOCAL_UUID, _uuid);
        PlayerPrefs.Save(); // 즉시 저장 보장
    }

    private static void LoadUUID()
    {
        _uuid = PlayerPrefs.GetString(KEY_LOCAL_UUID, "");
    }

    private static string GenerateSafeId()
    {
        // GUID와 기기 고유값을 조합하여 중복 가능성을 원천 차단
        string guid = Guid.NewGuid().ToString();
        //string deviceId = SystemInfo.deviceUniqueIdentifier;
        return $"Guest_{guid}";
    }

    /// <summary> 게스트 계정인가요? </summary>
    public static bool IsGuest => _uuid.StartsWith("Guest_"); 

    #endregion
}