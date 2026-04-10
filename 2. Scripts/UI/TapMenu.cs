using System;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 활성화 비활성화 연결을 위한 메뉴 모음
/// </summary>
public class TapMenu : MonoBehaviour
{
    [SerializeField] int defaultIndex;
    [SerializeField] TapManuContainer[] menu;

    [Serializable]
    public struct TapManuContainer
    {
        public TapMenuButton button;
        public TapMenuPanel panel;

        public void Deactivate()
        {
            panel?.DeActive();
            button?.Deactivate();
        }

        public void Activate()
        {
            panel?.Active();
            button?.Activate();
        }

        public void AddListener(UnityAction act) => button?.btn.onClick.AddListener(act);
    }

    private void Start()
    {
        Init();
    }

    void Init()
    {
        for (int i = 0; i < menu.Length; i++)
        {
            int index = i;
            menu[i].AddListener(() => ActiveThePanel(index));
            menu[i].Deactivate();
        }

        if (menu.Length <= defaultIndex) defaultIndex = 0;
        menu[defaultIndex].Activate();
    }

    void ActiveThePanel(int index)
    {
        for (int i = 0; i < menu.Length; i++)
        {
            if(index == i)
                menu[i].Activate();
            else
                menu[i].Deactivate();
        }
    }
}
