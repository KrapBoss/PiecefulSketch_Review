using TMPro;
using UnityEngine;
using UnityEngine.UI;

//°á°úÃ¢
public class ResultMultiEntity : MonoBehaviour
{
    [SerializeField] TMP_Text txt_Persent;
    [SerializeField] TMP_Text txt_Nickname;
    [SerializeField] Image slider_Persent;

    public void ShowResult(PlayerDataNetwork data)
    {
        gameObject.SetActive(true);

        slider_Persent.fillAmount = data.Perfection;
        txt_Nickname.text = $"{data.NickName}";
        txt_Persent.text = $"{data.Perfection * 100}";
    }
}
