using System;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Managers;
using Custom; // DOTween 네임스페이스는 계속 사용 (오브젝트 이동, 회전)

/// <summary>
/// [제작 의도] 재화 획득 시 UI로 날아가는 애니메이션을 관리하는 매니저 클래스.
/// [효율성] Object Pool을 사용하여 잦은 생성/삭제(GC)를 방지하고 메모리 효율을 높입니다.
/// </summary>
public class RewardAnimationManager : MonoBehaviour
{
    public static RewardAnimationManager Instance { get; private set; }

    //사용한 리소스 이름
    [SerializeField] string resourcesName = string.Empty;
    // 사용 중인 스프라이트 이름
    Sprite icon = null;

    [Header("설정")]
    [SerializeField] private GameObject _rewardObjectPrefab;
    [SerializeField] private int _maxObjectCount = 50; // 한 번에 생성될 최대 오브젝트 수

    // [추가] 1개 오브젝트가 표현할 기본 재화 개수
    [SerializeField] private int _amountPerObject = 5;

    private RewardObjectPool _pool;
    private List<RewardObject> _activeObjects = new List<RewardObject>();

    private Action _onCompleteCallback; // 완료 시 호출될 콜백
    private Canvas _mainCanvas; // Canvas 참조

    private bool _skip = false;

    /// <summary> 스프라이트 캐싱 </summary>
    Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();

    /// <summary>
    /// [제작 의도] 싱글톤 초기화 및 풀링 컴포넌트 부착
    /// </summary>
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        _pool = gameObject.AddComponent<RewardObjectPool>();
    }

    /// <summary>
    /// [제작 의도] 풀 초기화. 최대 오브젝트 수만큼 미리 생성
    /// </summary>
    private void Start()
    {
        if (_rewardObjectPrefab != null)
        {
            _pool.Init(_rewardObjectPrefab, transform, _maxObjectCount);
        }
    }

    /// <summary>
    /// [제작 의도] 재화 획득 애니메이션 연출. 클래스에 설정된 개당 가치를 기준으로 계산하되, 최대치 초과 시 동적으로 보정된 가치를 사용합니다.
    /// [효율성] 1개당 가치 설정 및 최대 생성 개수 제한을 통해 드로우콜을 최적화하고 누락을 방지합니다.
    /// 재화 획득 애니메이션 연출만 수행합니다. 숫자 갱신은 onComplete 콜백에서 일괄 처리됩니다.
    /// </summary>
    /// <param name="targetUI">아이템이 날아갈 목표 UI (RectTransform)</param>
    /// <param name="totalAmount">총 획득량 (오브젝트 개수 계산에만 사용)</param>
    /// <param name="atlasName">아틀라스 리소스명</param>
    /// <param name="itemName">아이템 리소스명</param>
    /// <param name="objectComplete">각 오브젝트가 동작 완료 시 실행되는 동작</param>
    /// <param name="onComplete">애니메이션이 모두 끝나거나 스킵될 때 호출될 콜백</param>
    public async void Play(RectTransform targetUI, int totalAmount, string atlasName, string itemName, Action objectComplete = null, Action onComplete = null)
    {
        if (_rewardObjectPrefab == null || targetUI == null)
        {
            Debug.LogError("RewardObject Prefab 또는 TargetUI가 지정되지 않았습니다!");
            onComplete?.Invoke();
            return;
        }

        if (string.IsNullOrEmpty(itemName))
        {
            Debug.LogError("아이템 이름에 해당하는 아틀라스 이름 지정이 필요합니다.");
            onComplete?.Invoke();
            return;
        }

        // 이미지 리소스 로드 부분 수정
        if (string.IsNullOrEmpty(atlasName))
        {
            if (!itemName.Equals(resourcesName))
            {
                if (spriteCache.TryGetValue(resourcesName, out Sprite _icon))
                {
                    icon = _icon;
                }
                else
                {   // 원본 에셋 로드
                    Sprite original = await ResourceManager.Instance.LoadAsset<Sprite>(itemName);

                    if (original != null)
                    {
                        // [핵심] 원본의 데이터를 복사하여 새로운 독립 스프라이트 생성
                        icon = CreateIndependentSprite(original);

                        spriteCache[resourcesName] = icon;

                        // 로드 직후 바로 어드레서블 핸들 해제 (이제 복제본을 쓰므로 안전함)
                        ResourceManager.Instance.ReleaseAsset(itemName);
                        resourcesName = string.Empty; // 핸들을 이미 해제했으므로 추적할 필요 없음
                    }
                }
            }
        }
        else
        {
            if (icon == null || !itemName.Equals(icon.name))
            {
                if (spriteCache.TryGetValue(resourcesName, out Sprite _icon))
                {
                    icon = _icon;
                }
                else
                {
                    // 아틀라스에서 원본 스프라이트 로드
                    Sprite original = await ResourceManager.Instance.LoadSpriteFromAtlas(atlasName, itemName);

                    if (original != null)
                    {
                        // 독립 스프라이트로 복제
                        icon = CreateIndependentSprite(original);

                        spriteCache[resourcesName] = icon;

                        // 아틀라스 핸들 즉시 해제
                        ResourceManager.Instance.ReleaseAsset(atlasName);
                        resourcesName = string.Empty;
                    }
                }
                    
            }
        }

        _mainCanvas = UIScreenManager.Instance.GetComponent<Canvas>();
        if (_mainCanvas == null)
        {
            onComplete?.Invoke();
        }

        _skip = false;

        _onCompleteCallback = null; // 이전 콜백 초기화
        _onCompleteCallback = onComplete; // 새 콜백 할당

        // 1. 시작 월드 좌표 계산 (화면 중앙)
        Vector3 startWorldPos;
        Vector2 screenCenter = new Vector2(Screen.width / 2, Screen.height / 2);

        // 캔버스의 카메라를 이용하여 화면 중앙의 월드 좌표를 가져옴
        RectTransformUtility.ScreenPointToWorldPointInRectangle(
            _mainCanvas.GetComponent<RectTransform>(),
            screenCenter,
            _mainCanvas.worldCamera,
            out startWorldPos);

        // 2. 타겟 UI의 월드 좌표 가져오기
        Vector3 targetWorldPos = targetUI.position;

        // [수정점] 설정된 _amountPerObject와 최대 생성 수를 고려해 1개당 실제 가치 도출
        int finalAmountPerObject = GetOneValue(totalAmount);

        // 실제 생성할 오브젝트 수 계산
        int spawnCount = Mathf.CeilToInt((float)totalAmount / finalAmountPerObject);
        spawnCount = Mathf.Min(spawnCount, _maxObjectCount); // 안전장치 유지

        int remainingObjects = spawnCount;

        for (int i = 0; i < spawnCount; i++)
        {
            RewardObject obj = _pool.Get();
            _activeObjects.Add(obj);

            // currentAmount는 더 이상 RewardObject에 전달되지 않음
            // FlyTo 함수 시그니처 변경에 맞춰 currentAmount 파라미터 제거
            obj.FlyTo(icon, startWorldPos, targetWorldPos, (rewardObject) =>
            {
                _activeObjects.Remove(rewardObject);
                _pool.Release(rewardObject);

                if (!_skip)
                {
                    CustomDebug.PrintW("개별 동작 실행");
                    objectComplete?.Invoke();   // 개별 오브젝트 시 완료 동작
                }

                remainingObjects--; // 오브젝트가 목적지에 도착했으므로 카운터 감소

                if (remainingObjects == 0) // 모든 오브젝트가 도착했다면
                {
                    CustomDebug.PrintW("전체 동작 실행");
                    _onCompleteCallback?.Invoke(); // 완료 콜백 호출
                    _onCompleteCallback = null; // 콜백 실행 후 초기화
                }

            });
        }
    }

    /// <summary>
    /// [제작 의도] 활성화된 모든 애니메이션을 즉시 중지하고 풀에 반환합니다.
    /// 모든 애니메이션을 즉시 중지하고 정리합니다.
    /// </summary>
    public void StopAll()
    {
        // 반복 중 리스트 변경을 피하기 위해 사본 사용
        var objectsToStop = new List<RewardObject>(_activeObjects);
        foreach (var obj in objectsToStop)
        {
            obj.Stop(); // 트윈 즉시 중지
            _pool.Release(obj); // 풀에 반환
        }

        _activeObjects.Clear(); // 활성 객체 리스트 초기화

        // 콜백이 있다면 호출하지 않고 초기화
        _onCompleteCallback?.Invoke();
        _onCompleteCallback = null;
    }

    /// <summary>
    /// [제작 의도] 재생 중인 연출을 강제로 완료 상태로 전환합니다.
    /// 스킵
    /// </summary>
    public void CompleteAll()
    {
        _skip = true;

        var currentActiveObjects = new List<RewardObject>(_activeObjects);
        foreach (var obj in currentActiveObjects)
        {
            obj.Complete(); // 개별 오브젝트 애니메이션 강제 완료
        }

        if (currentActiveObjects.Count == 0)
        {
            _onCompleteCallback?.Invoke();
            _onCompleteCallback = null;
        }
    }

    /// <summary>
    /// [제작 의도] 총 획득량을 기준으로 최대 생성 개수를 초과하지 않도록 1개당 가치를 재계산합니다.
    /// 개별 분할 개수
    /// </summary>
    /// <param name="totalAmount">총 획득량</param>
    /// <returns>최종 적용될 1개 오브젝트당 가치</returns>
    public int GetOneValue(int totalAmount)
    {
        int validAmountPerObject = Mathf.Max(1, _amountPerObject); // 0으로 나누기 방지
        int expectedSpawnCount = Mathf.CeilToInt((float)totalAmount / validAmountPerObject);

        // 예상 생성 개수가 최대 제한을 초과하면, 최대 제한 개수(maxObjectCount)에 맞춰 개당 가치를 증가시킴
        if (expectedSpawnCount > _maxObjectCount)
        {
            return Mathf.CeilToInt((float)totalAmount / _maxObjectCount);
        }

        return validAmountPerObject;
    }

    /// <summary>
    /// [제작 의도] 원본 스프라이트의 데이터를 복사해 독립적인 메모리를 갖는 객체를 만듭니다.
    /// 원본 스프라이트의 Texture와 Rect 정보를 기반으로 독립적인 새로운 Sprite 객체를 생성합니다.
    /// </summary>
    private Sprite CreateIndependentSprite(Sprite original)
    {
        if (original == null) return null;

        // 원본이 참조하는 Texture2D, Rect, Pivot 정보를 사용하여 새 객체 생성
        // 주의: 원본 Texture2D 자체가 메모리에서 해제되면 안 되므로, 
        // 실제 픽셀 데이터까지 복사하려면 Texture2D.Apply가 포함된 새 Texture 생성이 필요할 수 있습니다.
        // 하지만 일반적인 UI 해제 대응이라면 Sprite.Create 만으로도 참조 유지가 가능할 때가 많습니다.
        return Sprite.Create(
            original.texture,
            original.rect,
            new Vector2(original.pivot.x / original.rect.width, original.pivot.y / original.rect.height),
            original.pixelsPerUnit
        );
    }
}