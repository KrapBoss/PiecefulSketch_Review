using Custom;
using UnityEngine;

public class TapMenuPanel : MonoBehaviour
{
    IOnOff[] iOnOff;

    public virtual void Active()
    {
        if (gameObject.activeSelf) return;

        //하위에 참조하는 버튼 동작이 없다면?
        if(iOnOff == null) iOnOff = GetComponentsInChildren<IOnOff>();

        gameObject.SetActive(true);

        //모든 오브젝트 비활성화
        if (iOnOff != null)
        {
            foreach (var item in iOnOff)
            {
                if (!item.Active) item.On();
            }
        }

        CustomDebug.Print($"{transform.name} 활성화");
    }

    public virtual void DeActive()
    {
        if (!gameObject.activeSelf) return;

        CustomDebug.Print($"{transform.name} 비활성화");

        //모든 오브젝트 비활성화
        if (iOnOff != null)
        {
            foreach (var item in iOnOff)
            {
                if (item.Active) item.Off();
            }
        }

        gameObject.SetActive(false);
    }
}
