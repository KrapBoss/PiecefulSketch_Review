using Managers;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 사운드 매니저에서 소리를 얻어와 사용합니다.
/// </summary>
[RequireComponent(typeof(Button))]
public class UIElementString : MonoBehaviour
{

    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(Click);
    }
    
    void Click()
    {
        SoundManager.Instance.PlayClickUI();
    }
}
