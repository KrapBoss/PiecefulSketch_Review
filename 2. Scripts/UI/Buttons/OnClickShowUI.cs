using Global;
using Managers;
using UnityEngine;
using UnityEngine.UI;

public class OnClickShowUI : MonoBehaviour
{
    public string UIName;

    private void Start()
    {
        GetComponent<Button>()?.onClick.AddListener(ShowUI);
    }

    void ShowUI()
    {

        _ = UIScreenManager.Instance.ShowUI(UIName);
    }
}
