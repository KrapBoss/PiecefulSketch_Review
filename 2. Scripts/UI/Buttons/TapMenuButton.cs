using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TapMenuButton : MonoBehaviour
{
    [SerializeField] Image Selector;

    [HideInInspector] public Button btn;
    [HideInInspector] public Image img;

    private void OnEnable()
    {
        btn = GetComponent<Button>();
        img = btn.GetComponent<Image>();
    }

    public void Deactivate()
    {
        SelectorActive(false);
    }

    public void Activate()
    {
        SelectorActive(true);
    }
    
    /// <summary>
    /// 실제 활성 비활성화 시 실행되는 동작
    /// </summary>
    /// <param name="isActive"></param>
    protected virtual void SelectorActive(bool isActive)
    {
        if (Selector == null) return;

        Selector.gameObject.SetActive(isActive);
        btn.enabled = !isActive;
    }
}
