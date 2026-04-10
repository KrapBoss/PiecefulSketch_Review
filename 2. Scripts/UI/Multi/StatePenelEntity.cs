using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 
/// </summary>
[System.Serializable]
public class StatePenelEntity : MonoBehaviour
{
    public bool empty = true;

    [SerializeField] TMP_Text nickName;
    [SerializeField] Image playerIcon;
    [SerializeField] Slider slider_Progress;
    [SerializeField] TMP_Text sliderText;

    PlayerDataNetwork _playerData;

    //초기 등록
    public bool Register(PlayerDataNetwork playerData)
    {
        if (!empty) return false;

        _playerData = playerData;

        nickName.text = _playerData.NickName.ToString();

        slider_Progress.value = playerData.Perfection;

        sliderText.text = $"{slider_Progress.value}";

        empty = false;

        return true;

        //나중에 Icon 정보를 만들어서 불러올 것
        //playerIcon.sprite = playerData.PlayerIcon;
    }

    public bool Remove(PlayerDataNetwork playerData)
    {
        if (_playerData == null) return false;
        if (playerData != _playerData) return false;

        _playerData = null;

        nickName.text = "Empty";

        slider_Progress.value = 0;

        sliderText.text = $"{slider_Progress.value}";

        empty = true;

        return true;

        //나중에 Icon 정보를 만들어서 불러올 것
        //playerIcon.sprite = playerData.PlayerIcon;
    }

    public bool UpdateState(PlayerDataNetwork player)
    {
        if (_playerData == null) return false;

        if (player != _playerData) return false;

        slider_Progress.value = _playerData.Perfection;

        sliderText.text = $"{slider_Progress.value * 100: 0.#}";

        return true;
    }
}
