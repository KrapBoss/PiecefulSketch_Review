using Custom;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.SceneManagement;
using UnityEngine.U2D;

/// <summary>
/// Addressables 동기/비동기 로딩 및 참조 카운트 기반 메모리 관리 매니저
/// </summary>
public class ResourceManager : MonoBehaviour
{
    private static ResourceManager m_instance;
    public static ResourceManager Instance
    {
        get
        {
            if (m_instance == null)
            {
                GameObject go = new GameObject("#ResourceManager");
                m_instance = go.AddComponent<ResourceManager>();
                DontDestroyOnLoad(go);
            }
            return m_instance;
        }
    }

    // 로드된 핸들 보관 (Key: Addressable Key / Value: OperationHandle)
    private Dictionary<string, AsyncOperationHandle> _loadedHandles = new();

    // [추가 사항] 현재 로딩이 진행 중인 태스크 보관 (중복 로드 방지용)
    // Key: Addressable Key, Value: 로딩 결과를 반환할 Task
    private Dictionary<string, Task<object>> _loadingTasks = new();

    // [추가 사항] Addressables 초기화 상태 관리를 위한 Task
    private Task<bool> _initTask = null;
    private string _baseUrl = string.Empty;

    // 참조 카운트 관리
    private Dictionary<string, int> _refCounts = new();
    // 해제 대기 중인 에셋의 타임스탬프
    private Dictionary<string, float> _unusedTimestamps = new();

    // 씬 변경 시 무조건 해제할 에셋 키 리스트
    private List<string> _forceReleaseKeys = new();

    // 30초 동안 참조가 없으면 실제 메모리에서 해제
    private const float ReleaseDelay = 1.5f;

    private void Awake()
    {
        if (m_instance != null && m_instance != this)
        {
            Destroy(gameObject);
            return;
        }

        if (m_instance == null)
        {
            m_instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    /// <summary>
    /// Addressables 시스템 초기화를 비동기로 수행하고 완료를 보장합니다.
    /// 모든 에셋 로드 함수는 이 함수를 먼저 호출해야 합니다.
    /// </summary>
    public Task<bool> EnsureInitialized()
    {
        if (_initTask != null) return _initTask;

        _initTask = InitializeInternal();
        return _initTask;
    }

    /// <summary>
    /// Addressables 시스템 초기화를 비동기로 수행하고 완료를 보장합니다.
    /// 핸들의 유효성을 검사하여 'invalid operation handle' 예외를 방지합니다.
    /// </summary>
    private async Task<bool> InitializeInternal()
    {
        Debug.Log("[ResourceManager] Addressables 초기화 시작...");

        try
        {
            // [추가] 환경별 URL 설정 및 변환 함수 등록
            _baseUrl = BuildVersion.GetAddressableURL();
            Addressables.InternalIdTransformFunc = TransformInternalId;

            var handle = Addressables.InitializeAsync(true);
            await handle.Task;

            bool isSuccess = false;

            // [수정] 핸들이 유효한지 먼저 확인해야 'Attempting to use an invalid operation handle' 예외를 피할 수 있습니다.
            if (handle.IsValid())
            {
                isSuccess = handle.Status == AsyncOperationStatus.Succeeded;
                if (isSuccess)
                {
                    Debug.Log($"[ResourceManager] Addressables 초기화 성공 (Mode: {(BuildVersion.IsLocal ? "Local" : "Live")}, URL: {_baseUrl})");
                }
                else
                {
                    Debug.LogError($"[ResourceManager] Addressables 초기화 실패: {handle.OperationException}");
                }
            }
            else
            {
                // 핸들이 유효하지 않다면 이미 다른 곳에서 초기화가 완료되었거나 시스템에 의해 해제된 상태입니다.
                // 이 경우 초기화가 성공한 것으로 간주하고 진행하는 것이 일반적입니다.
                Debug.LogWarning("[ResourceManager] Addressables Handle이 이미 완료되어 Invalid 상태입니다. 초기화된 것으로 간주합니다.");
                isSuccess = true;
            }

            return isSuccess;
        }
        catch (Exception e)
        {
            // 로그 내 주석과 에러 메시지를 제거하지 않고 상세히 출력합니다.
            Debug.LogError($"[ResourceManager] Addressables 초기화 중 예외 발생: {e.Message}");
            return false;
        }
    }
    /// <summary>
    /// Addressables URL 변환 함수 (ResourceDownloadManager에서 이관)
    /// </summary>
    private string TransformInternalId(IResourceLocation location)
    {
        string url = location.InternalId;

        // http/https만 변환
        if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                Uri uri = new Uri(url);
                string convertedURL = $"{_baseUrl}{uri.AbsolutePath}";
                Debug.Log($"[ResourceManager] URL 변환: {convertedURL}");
                return convertedURL;
            }
            catch
            {
                return url;
            }
        }
        return url;
    }

    private void OnDestroy()
    {
        // 종료 처리 로직
    }

    private void Update()
    {
        UpdateGC();
    }

    #region Core Logic (Synchronous Return)

    /// <summary>
    /// 에셋 로드 상태를 정밀하게 추적하는 리소스 매니저 함수입니다. 기기 내 에셋만 선별 로드하며 중복 요청을 방지합니다.
    /// </summary>
    public async Task<T> LoadAsset<T>(string key, bool forceReleaseOnSceneChange = false) where T : UnityEngine.Object
    {
        if (string.IsNullOrEmpty(key)) return null;

        string originalKey = key;
        // 키 변환 로직
        switch (key)
        {
            //**
        }

        // 1. 이미 로드 완료된 캐시 확인
        if (_loadedHandles.TryGetValue(originalKey, out var handle))
        {
            Debug.Log($"<color=cyan>[ResourceManager]</color> 캐시 사용: {originalKey} : {_refCounts[originalKey] + 1}");
            _refCounts[originalKey]++;
            _unusedTimestamps.Remove(originalKey); // 사용 중이므로 GC 예약 해제
            return handle.Result as T;
        }

        // 2. [수정 사항] 현재 로딩 중인지 확인 (중복 요청 방지)
        if (_loadingTasks.TryGetValue(originalKey, out Task<object> existingTask))
        {
            Debug.Log($"<color=magenta>[ResourceManager]</color> 로딩 중인 태스크 대기: {originalKey}");
            var result = await existingTask;
            return result as T;
        }

        Debug.Log($"<color=green>[ResourceManager]</color> 리소스 로드 요청: {originalKey}");
        // 3. 로딩 태스크 생성 및 등록
        Task<object> loadTask = LoadAssetInternal<T>(key, originalKey, forceReleaseOnSceneChange);

        // 로딩 맵에 등록
        _loadingTasks.Add(originalKey, loadTask);

        try
        {
            // 실제 로딩 수행 대기
            var result = await loadTask;
            return result as T;
        }
        catch (Exception e)
        {
            Debug.LogError($"<color=red>[ResourceManager]</color> 로드 태스크 예외: {e.Message}");
            return null;
        }
        finally
        {
            // 성공하든 실패하든 로딩이 끝나면 목록에서 제거
            if (_loadingTasks.ContainsKey(originalKey))
            {
                _loadingTasks.Remove(originalKey);
            }
        }
    }

    /// <summary>
    /// 실제 Addressables 로딩 로직을 수행하는 내부 함수
    /// </summary>
    private async Task<object> LoadAssetInternal<T>(string key, string originalKey, bool forceReleaseOnSceneChange) where T : UnityEngine.Object
    {
        // 1. 로컬 저장 에셋 판단
        AsyncOperationHandle<IList<IResourceLocation>> locationsHandle = Addressables.LoadResourceLocationsAsync(key);
        try
        {
            IList<IResourceLocation> locations = await locationsHandle.Task;
            bool isAvailable = false;

            if (locations != null && locations.Count > 0)
            {
                var sizeHandle = Addressables.GetDownloadSizeAsync(key);
                try
                {
                    long size = await sizeHandle.Task;
                    if (size == 0) isAvailable = true; // 다운로드 크기가 0이어야 기기 내 존재함
                }
                finally
                {
                    Addressables.Release(sizeHandle);
                }
            }

            if (!isAvailable)
            {
                Debug.LogWarning($"<color=orange>[ResourceManager]</color> 기기 내 에셋 없음(로드 스킵): {key}");
                return null;
            }
        }
        finally
        {
            Addressables.Release(locationsHandle);
        }

        // 2. 비동기 로드 시작
        Debug.Log($"<color=yellow>[ResourceManager]</color> 로드 시작(1/2): {key}");

        var op = Addressables.LoadAssetAsync<T>(key);

        try
        {
            T asset = await op.Task;

            Debug.Log($"<color=yellow>[ResourceManager]</color> 로드 완료(2/2): {key} (Status: {op.Status})");

            if (op.Status == AsyncOperationStatus.Succeeded)
            {
                // 로드 성공 시 핸들 저장 및 참조 카운트 초기화
                _loadedHandles[originalKey] = op;
                _refCounts[originalKey] = 1;

                if (forceReleaseOnSceneChange)
                {
                    _forceReleaseKeys.Add(originalKey);
                }
                return asset;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"<color=red>[ResourceManager]</color> 로드 중 예외 발생: {key}\n{e.Message}");
        }

        // 실패 처리
        Debug.LogError($"<color=red>[ResourceManager]</color> 로드 최종 실패: {key}");
        if (op.IsValid()) Addressables.Release(op);

        return null;
    }

    /// <summary>
    /// 특정 라벨이 붙은 에셋 중 기기 내에 존재하는(로컬/캐시) 에셋만 즉시 로드하여 리스트로 반환합니다.
    /// </summary>
    public async Task<List<T>> LoadAllByLabel<T>(string label, bool forceReleaseOnSceneChange = false) where T : UnityEngine.Object
    {
        if (string.IsNullOrEmpty(label)) return null;

        string cacheKey = $"label_{label}";

        // 1. 캐시 확인
        if (_loadedHandles.TryGetValue(cacheKey, out var cachedHandle))
        {
            _refCounts[cacheKey]++;
            _unusedTimestamps.Remove(cacheKey);
            return new List<T>((IEnumerable<T>)cachedHandle.Result);
        }

        // 2. 라벨에 해당하는 모든 위치 정보 조회
        var locationsHandle = Addressables.LoadResourceLocationsAsync(label);
        IList<IResourceLocation> locations = await locationsHandle.Task;

        List<IResourceLocation> availableLocations = new List<IResourceLocation>();
        List<Task> checkTasks = new List<Task>();

        // 3. 기기 내 존재 여부(다운로드 사이즈 0) 체크
        foreach (var location in locations)
        {
            var sizeHandle = Addressables.GetDownloadSizeAsync(location);
            checkTasks.Add(sizeHandle.Task.ContinueWith(task =>
            {
                if (task.Status == TaskStatus.RanToCompletion && task.Result == 0)
                {
                    lock (availableLocations) { availableLocations.Add(location); }
                }
                Addressables.Release(sizeHandle);
            }));
        }
        await Task.WhenAll(checkTasks);

        // 4. 로드 진행
        if (availableLocations.Count > 0)
        {
            var loadHandle = Addressables.LoadAssetsAsync<T>(availableLocations, null);
            var result = await loadHandle.Task;

            if (loadHandle.Status == AsyncOperationStatus.Succeeded)
            {
                _loadedHandles[cacheKey] = loadHandle;
                _refCounts[cacheKey] = 1;
                if (forceReleaseOnSceneChange)
                {
                    _forceReleaseKeys.Add(cacheKey);
                }
                Addressables.Release(locationsHandle);
                return new List<T>(result);
            }
            if (loadHandle.IsValid()) Addressables.Release(loadHandle);
        }

        CustomDebug.PrintE($"[ResourceManager] 사용 가능한 라벨 에셋 없음: {label}");
        Addressables.Release(locationsHandle);
        return new List<T>();
    }

    /// <summary>
    /// 아틀라스에서 특정 스프라이트를 비동기로 찾아 반환합니다.
    /// </summary>
    public async Task<Sprite> LoadSpriteFromAtlas(string atlasName, string spriteName, bool forceReleaseOnSceneChange = false)
    {
        Debug.Log($"<color=yellow>[ResourceManager]</color> 아틀라스 로드 시도: {atlasName}");

        SpriteAtlas atlas = await LoadAsset<SpriteAtlas>(atlasName, forceReleaseOnSceneChange);

        if (atlas == null)
        {
            Debug.LogError($"<color=red>[ResourceManager]</color> 아틀라스 로드 실패: {atlasName}");
            return null;
        }

        Sprite sprite = atlas.GetSprite(spriteName);

        if (sprite == null)
        {
            Debug.LogWarning($"<color=orange>[ResourceManager]</color> 스프라이트 누락: {spriteName} (Atlas: {atlasName})");
            ReleaseAsset(atlasName);
            return null;
        }

        Debug.Log($"<color=green>[ResourceManager]</color> 스프라이트 로드 완료: {spriteName}");
        return sprite;
    }

    #endregion

    #region Memory Management (Release & GC)

    /// <summary> 리소스 해제 함수 [true : 리소스 해제할 게 있는 경우]</summary>
    /// <param name="key">리소스 할당 시 사용한 이름</param>
    public bool ReleaseAsset(string key)
    {
        CustomDebug.PrintW($"<color=red>[ResourceManager]</color> 리소스 해제 요청 : {key}");

        if (string.IsNullOrEmpty(key)) return false;
        if (!_refCounts.ContainsKey(key)) return false;

        _refCounts[key]--;

        CustomDebug.PrintW($"<color=red>[ResourceManager]</color> 리소스 해제 : {key} : {_refCounts[key]}");

        if (_refCounts[key] <= 0)
        {
            _refCounts[key] = 0;
            _unusedTimestamps[key] = Time.time;
        }
        return true;
    }

    private void UpdateGC()
    {
        if (_unusedTimestamps.Count == 0) return;

        float currentTime = Time.time;
        List<string> toRemove = new();

        foreach (var item in _unusedTimestamps)
        {
            if (currentTime - item.Value > ReleaseDelay)
                toRemove.Add(item.Key);
        }

        foreach (var key in toRemove)
        {
            ForceRelease(key);
            CustomDebug.PrintW($"[ResourceManager] 미사용 에셋 제거 완료: {key}");
        }
    }

    public void CleanupOnSceneChange()
    {
        CustomDebug.PrintE("씬 변경 감지: 미사용 리소스 즉시 정리 진행");
        List<string> toRemove = new();
        foreach (var item in _refCounts)
        {
            if (item.Value <= 0) toRemove.Add(item.Key);
        }

        foreach (var key in toRemove)
        {
            ForceRelease(key);
            CustomDebug.PrintW($"리소스 제거 : <color=green>{key}</color>");
        }

        // 강제 해제 대상 리소스 처리
        foreach (var key in _forceReleaseKeys)
        {
            ForceRelease(key);
        }
        _forceReleaseKeys.Clear(); // 처리 후 리스트 비우기

        Resources.UnloadUnusedAssets();
    }

    private void ForceRelease(string key)
    {
        if (_loadedHandles.TryGetValue(key, out var handle))
        {
            if (handle.IsValid()) Addressables.Release(handle);
            _loadedHandles.Remove(key);
            _refCounts.Remove(key);

            CustomDebug.PrintW($"리소스 제거 : <color=green>{key}</color>");
        }
        _unusedTimestamps.Remove(key);
    }

    #endregion
}