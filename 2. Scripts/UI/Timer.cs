using Custom;
using System;
using TMPro;
using UnityEngine;

/// <summary>
/// �ð��� ǥ���մϴ�.
/// </summary>
public class TimerUI : MonoBehaviour
{
    [SerializeField] TMP_Text text;

    [SerializeField]static SecureFloat timer = 0;
    public static SecureFloat Timer => timer;

    public static bool IsStop = false;

    private void Awake()
    {
        if(text != null)
        {
            text.text = "";
            text.color = Color.yellow;
        }

        timer = 0;
        IsStop = true;
    }



    public void SetColor(Color color)
    {
        text.color = color;
    }

    private void Update()
    {
        if (IsStop) return;

        if(StateManager.Instance.State == LocalGameState.Playing)
             timer += Time.deltaTime;
    }

    ///�ʴ��� �ð��� �ִ� Hour ������ ��ȯ���� ����
    public void SetTime(float timeBySec, bool transition = true)
    {
        if (!transition) { text.text = $"{timeBySec: 0.#}"; }
        //��ȯ
        else
        {
            float totalSeconds = timeBySec; // ��ȯ�� ��
            TimeSpan timeSpan = TimeSpan.FromSeconds(totalSeconds);

            // ��:��:�� �������� ���
            string formattedTime = string.Format("{0:D2}:{1:D2}",
                timeSpan.Minutes,
                timeSpan.Seconds);

            text.text = formattedTime; // ���: 01:01:01
        }
    }
}
