using UnityEngine;

/// <summary>
/// 해당 오브젝트를 활성, 비활성화 함
/// </summary>
public class BeActived : MonoBehaviour, IOnOff
{
    public bool Active { get; set; }

    public bool Off()
    {
        //CustomDebug.Print("사랑안합니다.");
        gameObject.SetActive(false);

        Active = false;
        return Active;
    }

    public bool On()
    {
        //CustomDebug.Print("사랑합니다.");
        gameObject.SetActive(true);

        Active = true;
        return Active;
    }
}
