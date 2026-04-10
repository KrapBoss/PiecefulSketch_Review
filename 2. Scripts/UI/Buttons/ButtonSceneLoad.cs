using Custom;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ButtonSceneLoad : MonoBehaviour
{
    public string SceneName;
    private void Awake()
    {
        CustomDebug.PrintW($"{SceneName} 을 로드합니다.");
        GetComponent<Button>().onClick.AddListener(
            () =>
            {
                SceneManager.LoadScene(SceneName);
            }
            );
    }
}
