using Managers;
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 씬에 대한 정보를 가지고 있는다.
public class SceneInformation
{
    public string SceneName = null;          // 로딩할 씬의 이름
    public Action SceneEndAction = null;     // 씬 로딩 후 실행 할 것
    public bool SceneLoading = false;        // 씬 로딩 완료 여부
}

public class SceneLoader : MonoBehaviour
{
    private static SceneInformation sceneInformation = new SceneInformation();
    public static SceneInformation SceneInformation => sceneInformation;

    //public Image image_Progress1;
    //public Image image_Progress2;

    //public Animation anim_Loading;

    void Start()
    {
        DontDestroyOnLoad(this.gameObject);
        StartCoroutine(SceneLoad());

        Notice.HideImmediate();

        Debug.LogWarning("씬이 전환되어 사운드 체인저를 비워줬음");
    }

    IEnumerator SceneLoad()
    {
        Debug.LogWarning("씬을 불러오는 중.....");

        // [수정] 로딩 씬 진입 시 화면이 가려져(Black) 있으므로, 로딩바를 보여주기 위해 화면을 밝힘
        // 제작 의도: 이전 씬에서 페이드 아웃된 상태로 넘어오므로, 로딩 UI를 보여주기 위해 페이드 인 처리

        //anim_Loading.Play();

        if(ResourceManager.Instance) ResourceManager.Instance.CleanupOnSceneChange();

        // 씬 로딩을 위한 함수 (비동기 로드 시작)
        // Unity 효율성: 대규모 리소스 로드 시 메인 스레드 멈춤 방지를 위해 비동기 사용
        AsyncOperation _operation = SceneManager.LoadSceneAsync(sceneInformation.SceneName);
        //_operation.allowSceneActivation = false; // 로드 완료 후 자동 전환 방지

        // 로딩 진행도 표시
        //float _progress = 0.0f;
        //while (_progress < 0.89f)
        //{
        //    // 진행도가 0.9 미만일 때는 실제 로딩 진행률을 따라감
        //    if (_progress < _operation.progress)
        //    {
        //        _progress = _operation.progress;
        //    }
        //    // (선택 사항) 로딩바가 너무 빨리 차는 것을 방지하거나 부드럽게 채우려면 Mathf.Lerp 사용 권장

        //    image_Progress1.fillAmount = _progress;
        //    image_Progress2.fillAmount = _progress;

        //    yield return null;
        //}

        //// 로딩바를 100%로 채워줌 (시각적 완성을 위함)
        //image_Progress1.fillAmount = 1.0f;
        //image_Progress2.fillAmount = 1.0f;

        // [요청 사항 반영] 타 씬은 모두 불러온 이후에 나머지 기능이 진행되도록 처리
        _operation.allowSceneActivation = true;

        // 실제 씬 전환이 완료될 때까지 대기
        while (!_operation.isDone)
        {
            Debug.Log("맵을 불러오고 있습니다 (전환 대기 중)...");
            yield return null;
        }

        // --- 이 시점은 타겟 씬이 완벽하게 로드되고 활성화된 상태임 ---

        // 씬 로드 후 지정된 동작 수행
        if (sceneInformation.SceneEndAction != null)
        {
            sceneInformation.SceneEndAction();
            sceneInformation.SceneEndAction = null;
        }

        Debug.Log("씬 로드 성공 및 전환 완료 : " + sceneInformation.SceneName);

        // 종료를 알림
        sceneInformation.SceneLoading = false;
        Time.timeScale = 1.0f;

        yield return new WaitForSeconds(0.2f);

        Fade.FadeSetting(false, 0.5f, GameSetting.Night ? Color.black : Color.white);

        // 로딩이 끝났으므로 화면을 다시 밝혀주거나(이미 밝음), 혹은 연출에 따라 페이드 처리
        // 여기서는 로딩바 제거 전 페이드를 할지, 그냥 제거할지 결정해야 함.
        // 일반적인 흐름: 로딩 UI 상태에서 페이드 아웃(가림) -> 로더 삭제 -> 페이드 인(게임 화면)
        // 현재 코드 로직 유지: FadeSetting(false)는 '화면을 보이게 함'으로 추정됨.

        // 현재 로딩 오브젝트 제거
        Destroy(gameObject);
    }

    /// <summary>
    /// 씬을 불러오는 중 
    /// </summary>
    // 씬 로딩을 위한 전역함수
    public static void LoadLoaderScene(string _name, Action _action = null)
    {
        if(SceneLoader.sceneInformation.SceneLoading)
        {
            Debug.LogWarning($"씬을 불러오고 있는 중입니다.");
        }

        SceneLoader.sceneInformation.SceneLoading = true;
        //AdsManager.Instance.ShowRewardedInterstitialAd(() => LoadScene(_name, _action));
        AdsManager.Instance.CheckAD(() => LoadScene(_name, _action));
    }

    static void LoadScene(string _name, Action _action = null)
    {// [요청 사항 반영] Fade 호출 -> 0.5초 대기 -> LoadScene 호출
        // static 함수에서는 Coroutine을 직접 쓸 수 없으므로, 임시 MonoBehaviour를 생성하여 위임합니다.
        GameSetting.Save();

        // 1. 정보를 미리 설정
        SceneLoader.sceneInformation.SceneName = _name;
        SceneLoader.sceneInformation.SceneEndAction = _action;

        // 2. 임시 객체를 생성하여 전환 코루틴 실행
        GameObject dummyRunner = new GameObject("SceneTransitionRunner");
        DontDestroyOnLoad(dummyRunner); // 씬 전환 중 파괴 방지 (LoadScene 호출 직전까지 생존 필요)

        TransitionHelper helper = dummyRunner.AddComponent<TransitionHelper>();
        helper.StartCoroutine(helper.ProcessTransitionSequence());
    }

    // [내부 클래스] Static 메서드에서 코루틴을 실행하기 위한 헬퍼
    private class TransitionHelper : MonoBehaviour
    {
        public IEnumerator ProcessTransitionSequence()
        {
            // 1. 화면 가리기 (Fade Out)
            // 제작 의도: 씬 전환 전 화면을 부드럽게 암전시킴
            Fade.FadeSetting(true, 0.5f, GameSetting.Night ? Color.black : Color.white);

            SoundManager.Instance.PlayEffect(SoundNames.SCENE_MOVE);

            // 2. 0.5초 대기 (페이드 애니메이션 시간 확보)
            yield return new WaitForSeconds(0.5f);


            // 3. 로딩 씬 호출
            // Unity 효율성: LoadScene은 동기식이므로 호출 즉시 프레임이 멈출 수 있음. 페이드 아웃 상태라 티가 나지 않음.
            SceneManager.LoadScene("LoadScene");

            // 4. 임무 완료 후 자폭
            Destroy(this.gameObject);
        }
    }
}

public struct SceneName
{
    
}
