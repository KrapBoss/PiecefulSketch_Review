using System.Collections.Generic;
using UnityEngine;

//일시적으로 쌓인 UI를 관리합니다.
public class UIStack : MonoBehaviour
{
    Stack<IActiveUI> activeUIs;

    private void Awake()
    {
        activeUIs = new Stack<IActiveUI> ();
    }

    //이전 UI는 비활성화 후 다음 UI를 활성화합니다.
    public void Push(IActiveUI tmp)
    {
        if(activeUIs.Count > 0)
        {
            activeUIs.Peek().DeActive ();
        }
        tmp.Active(this);
        activeUIs.Push(tmp);
    }

    //현재 UI를 제거 후 하위 UI를 활성화합니다.
    public void Pop()
    {
        if(activeUIs.Count > 0)
        {
            activeUIs.Pop().Delete();

            if (activeUIs.Count > 0) activeUIs.Peek().Active(this);
        }
    }

    public void Clear()
    {
        while(activeUIs.Count > 0)
        {
            activeUIs.Pop().Delete();
        }
    }
}
