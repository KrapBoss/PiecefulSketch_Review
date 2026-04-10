using Global;
using Managers;
using System.Threading.Tasks;
using TMPro;
using UI.Core;
using UnityEngine;

public class LoadingUI : UIBase 
{
    [Header("Settings")]
    [SerializeField] private TMP_Text targetText;     // 효과를 적용할 텍스트 컴포넌트
    [SerializeField] private string format = "Loading"; // 기본 문구
    [SerializeField] private float dotInterval = 0.5f; // 점이 추가되는 시간 간격
    [SerializeField] private int maxDots = 3;         // 최대 점 개수

    private float _timer;
    private int _currentDotCount;

    private void Update()
    {
        if (targetText == null) return;
        if (string.IsNullOrEmpty(format)) return;

        _timer += Time.deltaTime;

        if (_timer >= dotInterval)
        {
            _timer = 0f;
            UpdateText();
        }
    }

    /// <summary>
    /// 점의 개수를 계산하여 최종 문자열을 적용합니다.
    /// </summary>
    private void UpdateText()
    {
        _currentDotCount = (_currentDotCount + 1) % (maxDots + 1);

        // new string(문자, 개수)를 사용하여 효율적으로 문자열 생성
        string dots = new string('.', _currentDotCount);
        targetText.text = $"{format}{dots}";
    }


    public async static Task Show(string message)
    {
        var item = await UIScreenManager.Instance?.ShowUI(UIName.LOADING_UI);
        if(item as LoadingUI)
        {
            Debug.Log("[LoadingUI] : Loaded");
            (item as LoadingUI).format = Localization.Localize(message);
        }
    }

    public static void Close()
    {
        UIScreenManager.Instance.CloseUI(UIName.LOADING_UI);
    }
}
