using Custom;
using UnityEngine;
using UnityEngine.UI;

public class ActiveUIButton : MonoBehaviour
{
    [SerializeField] GameObject Element;

    Button _btn;
    UIStack _stack;

    private void Awake()
    {
        _btn = GetComponent<Button>();

        _btn.onClick.AddListener(ActiveUI);
    }

    public void ActiveUI()
    {
        if (Element == null) CustomDebug.Exeption("지정된 UI Element가 없습니다.");

        var go = Instantiate(Element, _stack.transform);
        _stack.Push(go.GetComponent<IActiveUI>());
    }
}
