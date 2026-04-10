using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// CanvasGroup을 사용하여 공지 사항의 페이드 효과와 출력을 관리하는 매니저입니다.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class Notice : MonoBehaviour
{
    private static Notice _instance;

    [Header("UI Components")]
    [SerializeField] private TextMeshProUGUI noticeText;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Settings")]
    [SerializeField] private float showDuration = 3.0f; // a초: 보여지는 시간
    [SerializeField] private float fadeDuration = 0.5f; // b초: 페이드 아웃 시간

    private Coroutine _fadeCoroutine;

    static bool loading = false;

    // [제작 의도] 전역 어디서든 Notice() 호출만으로 UI 메커니즘을 구동하기 위함
    // [효율성] CanvasGroup을 사용하여 오브젝트 활성/비활성 오버헤드 없이 시각적 효과만 제어

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);

            // 초기 설정: 즉시 숨김
            HideImmediateInternal();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 전역 공지 사항을 출력합니다.
    /// </summary>
    /// <param name="message">출력할 내용</param>
    public static async void Message(string message)
    {
        if (loading) return;

        if (_instance == null)
        {
            try
            {
                loading = true;
                var prefab = await ResourceManager.Instance.LoadAsset<GameObject>("NoticeObject");
                _instance = Instantiate(prefab).GetComponent<Notice>();
                loading = false;
            }
            catch(System.Exception e)
            {
                Debug.LogWarning(e.InnerException.Message);
                return;
            }
        }

        _instance.ShowNotice(message);
    }

    /// <summary>
    /// 공지를 즉시 보이지 않게 처리합니다.
    /// </summary>
    public static void HideImmediate()
    {
        _instance?.HideImmediateInternal();
    }

    private void ShowNotice(string message)
    {
        // 기존 실행 중인 페이드가 있다면 중지
        if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);

        // 텍스트 할당 및 즉시 표시
        if (noticeText != null) noticeText.text = Localization.Localize(message);
        canvasGroup.alpha = 1f;

        _fadeCoroutine = StartCoroutine(Co_NoticeProcess());
    }

    private void HideImmediateInternal()
    {
        if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
        canvasGroup.alpha = 0f;
    }

    private IEnumerator Co_NoticeProcess()
    {
        // 1. a초 동안 대기
        yield return new WaitForSeconds(showDuration);

        // 2. b초 동안 Fade Out
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = 0f;
        _fadeCoroutine = null;
    }
}