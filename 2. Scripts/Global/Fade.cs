using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Fade : MonoBehaviour
{
    public Image img_fade;

    static GameObject fadeObject;

    public void FadeStart(bool fadein, float time, Color c)
    {
        DontDestroyOnLoad(this.gameObject);
        StartCoroutine(FadeCroutine(fadein, time, c));
    }
    IEnumerator FadeCroutine(bool fadein, float time, Color c)
    {
        //페이드 시작을 알림
        b_faded = false;

        float from, to;
        if (fadein) { from = 0.0f; to = 1.0f; } else { from = 1.0f; to = 0.0f; }

        c.a = from;
        img_fade.color = c;

        float currTime = 0.0f;
        float ratio = 0.0f;

        while (ratio < 1.0f)
        {
            currTime += Time.unscaledDeltaTime;
            ratio = currTime / time;

            c.a = Mathf.Lerp(from, to, ratio);
            img_fade.color = c;

            yield return null;
        }

        //FadeOut일 경우 게임오브젝트를 제거한다.
        if (!fadein)
        {
            Destroy(this.gameObject);
        }

        //페이드 완료됨
        b_faded = true;

        //혹시 모를 경우를 대비
        //yield return new WaitForSeconds(2.0f);
        //if (_fadein) FadeSetting(false, 0.5f, _color);
        //else 
            //Destroy(this.gameObject);
    }
    private void OnDestroy()
    {
        StopAllCoroutines();
        Resources.UnloadUnusedAssets();
    }


    //페이드 동작 완료 여부
    public static bool b_faded;
    //이전 페이드 오브젝트를 담는다.
    static GameObject go_FadeIn = null;
    //이전 페이드
    static bool _fadein = false;
    //이전 컬러
    static Color _color = Color.white;

    //Fade 동작 실행을 위한 전역 함수
    public static void FadeSetting(bool fadein, float time, Color c)
    {
        //페이드 오브젝트 가져오기
        if (fadeObject == null) fadeObject = Resources.Load<GameObject>("Prefabs/FadeObject");

        //이전 페이드와 동작이 같다면 실행하지 않습니다.
        if (_fadein == fadein) return;

        _fadein = fadein;

        //페이드 객체가 있을 경우 제거합니다.
        if (go_FadeIn != null)
        {
            Destroy(go_FadeIn);
        }

        go_FadeIn = Instantiate(fadeObject);
        //객체 생성
        Fade fade = go_FadeIn.GetComponent<Fade>();

        //이전 컬러로 페이드 아웃을 진행
        if (fadein) _color = c;
        //실행
        fade.FadeStart(fadein, time, _color);
    }
}
