using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 싱글 게임에서 퍼즐의 완성도를 표시하는 역할을 합니다.
/// </summary>
public class PerfectionBar : MonoBehaviour
{
    public Image img_Perfection;

    float _target;

    IEnumerator croutine;

    private void Awake()
    {
        StateManager.Instance.InsertGameStateAction(LocalGameState.Starting, Init);
    }

    public void Init()
    {
        img_Perfection.fillAmount = 0;
        _target = 0;

        //생성된 퍼즐 컨테이너에 이벤트 등록
        FindObjectOfType<PuzzleContainer>().Action_Fit += Perfection;
    }

    //퍼즐의 완성로를 표시합니다.
    public void Perfection(float t)
    {
        _target = t;

        //기존 동작 코루틴이 존재하면, 목표치만 변경
        if(croutine != null)
        {
            time = 0;
            return;
        }

        croutine = PerfectionCroutine();
        StartCoroutine(croutine);
    }

    float time = 0;
    IEnumerator PerfectionCroutine()
    {
        //0~1 까지의 보간으로 진행률 적용
        time = 0;
        while (img_Perfection.fillAmount < _target)
        {
            time = Mathf.Clamp(time + Time.deltaTime, 0 ,1.0f);

            img_Perfection.fillAmount = Mathf.Lerp(img_Perfection.fillAmount, _target, time);

            yield return null;
        }
        croutine = null;
    }

    private void OnDestroy()
    {
        if(croutine != null) {  StopCoroutine(croutine); }

        if (StateManager.Instance != null) StateManager.Instance.DeleteGameStateAction(LocalGameState.Starting, Init);
    }
}
