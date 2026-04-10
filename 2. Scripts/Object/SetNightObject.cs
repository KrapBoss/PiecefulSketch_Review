using Custom;
using UnityEngine;

public class SetNightObject : MonoBehaviour, INight
{
    [SerializeField] SpriteRenderer sr;
    [SerializeField] Color morningColor = Color.black;
    [SerializeField] Color nightColor = Color.white;

    private void Start()
    {
        SetNight(GameSetting.Night);
        EventManager.Instance.action_SetNight += SetNight;
    }

    private void OnDestroy()
    {
        EventManager.Instance.action_SetNight -= SetNight;
    }

    public void SetNight(bool night)
    {
        CustomDebug.PrintW($"{transform.name} 오브젝트의 색을 변경합니다. ");
        if (night) { sr.color = nightColor; }
        else { sr.color = morningColor; }
    }
}
