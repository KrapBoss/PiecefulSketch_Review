
using Custom;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class OnOffButton : MonoBehaviour
{
    [SerializeField] bool offWhenStart = true;
    [SerializeField] GameObject GoOnOff;
    IOnOff iOnOff;

    // Start is called before the first frame update
    void Start()
    {
        if (GoOnOff != null) iOnOff = GoOnOff.GetComponent<IOnOff>();
        else { iOnOff = GetComponent<IOnOff>(); }

        if (iOnOff == null) { CustomDebug.PrintE($"{gameObject.name} 에 OnOffButton이 없습니다."); return; }
        if (offWhenStart) iOnOff.Off();

        GetComponent<Button>().onClick.AddListener(OnOff);
    }

    public void OnOff()
    {
        if (iOnOff == null) return;
        if (iOnOff.Active)
        {
            iOnOff.Off();
        }
        else
        {
            iOnOff.On();
        }
    }
}
