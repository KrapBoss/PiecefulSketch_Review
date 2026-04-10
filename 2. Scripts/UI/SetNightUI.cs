using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SetNightUI : MonoBehaviour, INight
{
    [Header("UI 요소 할당")]
    [SerializeField] private Image img;   // 인스펙터에서 Image 할당
    [SerializeField] private TextMeshProUGUI txt;    // 인스펙터에서 Text 할당

    [Header("색상 설정")]
    [SerializeField] private Color morning = Color.white;
    [SerializeField] private Color night = Color.black;

    private void Start()
    {
        SetNight(GameSetting.Night);
        EventManager.Instance.action_SetNight += SetNight;
    }

    private void OnDestroy()
    {
        // 이벤트 구독 해제: 메모리 누수 방지
        if (EventManager.Instance != null)
        {
            EventManager.Instance.action_SetNight -= SetNight;
        }
    }

    /// <summary>
    /// 주야간 상태(n)에 따라 할당된 UI 요소의 색상을 변경합니다.
    /// </summary>
    /// <param name="n">true: 야간, false: 주간</param>
    public void SetNight(bool n)
    {
        Color targetColor = n ? night : morning;

        // Image가 할당되어 있는 경우에만 적용
        if (img != null)
        {
            img.color = targetColor;
        }

        // Text가 할당되어 있는 경우에만 적용
        if (txt != null)
        {
            txt.color = targetColor;
        }
    }
}