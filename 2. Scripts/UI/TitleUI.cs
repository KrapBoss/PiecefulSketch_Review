using TMPro;
using UnityEngine;

public class TitleUI : MonoBehaviour
{
    [SerializeField] TMP_Text text;

    private void Awake()
    {
        text.text = "";
    }

    public void SetTitle(string title)
    {
        text.text = title;
    }
}
