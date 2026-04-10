using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 뒤로가기 버튼을 누르면 이동합니다.
/// </summary>
public class SceneLoadButton : MonoBehaviour
{
    [Header("View in Editor"),SerializeField]bool done;

    public enum SceneName
    {
        Title, Single, Multi
    }
    public SceneName scene;

    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(GoToScene);
    }

    public void GoToScene()
    {
        //중복 버튼 클릭 방지
        if (done) return;

        done = true;

        switch (scene)
        {
            case SceneName.Single:
                SceneLoader.LoadLoaderScene(GameConfig.SingleSceneName);
                break;
            case SceneName.Title:
                SceneLoader.LoadLoaderScene(GameConfig.TitleSceneName);
                break;
            case SceneName.Multi:
                SceneLoader.LoadLoaderScene(GameConfig.MultiSceneName);
                break;
        }
    }
}
