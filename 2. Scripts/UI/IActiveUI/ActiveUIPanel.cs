using Custom;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 패널이 활성되 되며 다음 동작을 수행하기 위한 정의가 포함됩니다.
/// 가장 기본 정의입니다.
/// </summary>
public class ActiveUIPanel : MonoBehaviour, IActiveUI
{
    public Button[] btn_Close;

    UIStack _stack;

    //현재 패널을 활성화하면서 해야되는 설정을 진행합니다.
    public void Active(UIStack stack)
    {
        CustomDebug.Print("ActiveUIPanel :: 스택에 의한 활성화");

        gameObject.SetActive(true);

        if (_stack == null) _stack = stack;

        //닫기 버튼을 누를 경우 제거합니다.
        if(btn_Close != null)
        {
            foreach(Button btn in btn_Close)
            {
                //등록된 이벤트가 없는 경우
                if(btn.onClick.GetPersistentEventCount() < 1) {
                    btn.onClick.AddListener(() => _stack.Pop());
                }
            }
        }
    }

    //잠시 비활성화하는 용도입니다.
    public void DeActive()
    {
        gameObject.SetActive(false);
    }

    //삭제하면서 해야되는 설정이 있다면 진행합니다.
    public void Delete()
    {
        Destroy(gameObject);
    }
}
