using Managers;
using System; // For Enum
using System.Collections.Generic; // For Dictionary
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 버튼 클릭 시 미리 정의된 외부 링크로 이동하는 기능을 제공하는 스크립트입니다.
/// Inspector에서 링크 타입을 선택하고 URL을 정의할 수 있습니다.
/// </summary>
[RequireComponent(typeof(Button))]
public class OnClickExternalLinkOpener : MonoBehaviour
{
    /// <summary>
    /// 관리할 외부 링크의 종류를 정의하는 열거형입니다.
    /// </summary>
    public enum LinkType
    {
        None,             // 링크 없음 또는 미지정
        PrivacyPolicy,    // 개인정보처리방침 링크
        GameInformation   // 게임 정보 링크
    }

    [Header("URL Definitions")]
    [Tooltip("개인정보처리방침 페이지의 URL입니다.")]
    [SerializeField] private string privacyPolicyUrl = "https://frontier-money.tistory.com/3";

    [Tooltip("게임 정보 페이지의 URL입니다.")]
    [SerializeField] private string gameInformationUrl = "https://frontier-money.tistory.com/4";

    [Header("Link Assignment")]
    [Tooltip("이 버튼에 연결할 링크의 종류를 선택하세요.")]
    [SerializeField] private LinkType selectedLinkType = LinkType.None;

    private Button _button;
    private Dictionary<LinkType, string> _linkMap;

    private void Awake()
    {
        // URL 맵 초기화
        _linkMap = new Dictionary<LinkType, string>
        {
            { LinkType.PrivacyPolicy, privacyPolicyUrl },
            { LinkType.GameInformation, gameInformationUrl }
        };

        // 버튼 컴포넌트 참조 및 클릭 이벤트 등록
        _button = GetComponent<Button>();
        if (_button != null)
        {
            _button.onClick.AddListener(OpenSelectedLink);
        }
        else
        {
            Debug.LogError($"ExternalLinkOpener: Button component not found on GameObject '{gameObject.name}'. This script requires a Button component.", this);
        }
    }

    private void OnDestroy()
    {
        // 스크립트 파괴 시 이벤트 리스너 해제
        if (_button != null)
        {
            _button.onClick.RemoveListener(OpenSelectedLink);
        }
    }

    /// <summary>
    /// 선택된 링크 타입에 해당하는 URL을 웹 브라우저로 엽니다.
    /// </summary>
    private void OpenSelectedLink()
    {
        SoundManager.Instance.PlayClickUI();

        if (_linkMap.TryGetValue(selectedLinkType, out string url))
        {
            if (!string.IsNullOrEmpty(url))
            {
                Application.OpenURL(url);
                Debug.Log($"ExternalLinkOpener: Opening URL for '{selectedLinkType}': {url}");
            }
            else
            {
                Debug.LogWarning($"ExternalLinkOpener: URL is not defined for LinkType '{selectedLinkType}'.");
            }
        }
        else
        {
            Debug.LogError($"ExternalLinkOpener: LinkType '{selectedLinkType}' not found in the URL map. Please check the enum and map initialization.");
        }
    }
}
