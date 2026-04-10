using Custom;
using TMPro;
using UnityEngine;

/// <summary>
/// 퍼즐 보상 단계별 UI의 상태(On/Off)를 제어하는 스크립트입니다.
/// </summary>
public class RewardStepUI : MonoBehaviour
{
    public TextMeshProUGUI txt_itemCount;
    public TextMeshProUGUI txt_time;

    [SerializeField]
    [Tooltip("보상 달성(On) 상태를 나타내는 게임 오브젝트")]
    private GameObject onStateObject;

    [SerializeField]
    [Tooltip("보상 미달성(Off) 상태를 나타내는 게임 오브젝트")]
    private GameObject offStateObject;

    bool active = false;

    /// <summary>
    /// 초반 세팅 함수
    /// </summary>
    /// <param name="count">코인 개수</param>
    /// <param name="time"> 달성 시간 정보 </param>
    /// <param name="_active">활성화 여부 (지금은 사용 X)</param>
    public void Set(int count,string time, bool _active= true)
    {
        txt_itemCount.text = CustomCalculator.TFCoinString(count);
        txt_time.text = Localization.Localize(time);
        active = _active;
    }

    /// <summary>
    /// 보상을 받을 수 있는 상태
    /// </summary>
    public void TurnOn()
    {
        if (onStateObject != null)
        {
            onStateObject.SetActive(true);
        }
        if (offStateObject != null)
        {
            offStateObject.SetActive(false);
        }
    }

    /// <summary>
    /// 보상을 받은 상태
    /// </summary>
    public void TurnOff()
    {
        if (onStateObject != null)
        {
            onStateObject.SetActive(false);
        }
        if (offStateObject != null)
        {
            offStateObject.SetActive(true);
        }
    }

    /// <summary>
    /// 모든 상태를 비활성화하여 UI를 초기화합니다.
    /// </summary>
    public void ResetUI()
    {
        if (onStateObject != null)
        {
            onStateObject.SetActive(false);
        }
        if (offStateObject != null)
        {
            offStateObject.SetActive(false);
        }
    }
}
