using Custom;
using Managers;
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;

/// <summary>
/// 오브젝트를 순차적으로 하나씩 이동시키며 회전값 복구를 포함하는 UI 조립 클래스
/// </summary>
public class UIObjectAssembler : MonoBehaviour
{
    [Header("UI References")]
    public RectTransform container;
    public RectTransform innerBoundary;
    public RectTransform[] targetObjects;
    public TextMeshProUGUI statusText;

    [Header("Settings")]
    public float totalDuration = 5.0f; // 전체 완료 시간 (N)
    public float dotInterval = 0.5f;

    private Vector2[] _savedPositions;
    private Quaternion[] _savedRotations;
    private bool _isInitialized = false;
    private Coroutine _masterRoutine;

    //동작을 알림
    bool done = false;

    private void OnDisable() => StopAllRunningProcesses();

    public void StartAssembly(string processMsg, string completeMsg, Action onComplete)
    {
        gameObject.SetActive(true);

        if (!_isInitialized) SaveOriginalStates();

        StopAllRunningProcesses();
        ScatterObjects();

        _masterRoutine = StartCoroutine(AssemblyProcessRoutine(Localization.Localize(processMsg), Localization.Localize(completeMsg), onComplete));
    }

    private void StopAllRunningProcesses()
    {
        if (_masterRoutine != null) StopCoroutine(_masterRoutine);
        StopAllCoroutines();
    }

    /// <summary>
    /// 순차적 이동을 관리하는 마스터 루틴
    /// </summary>
    private IEnumerator AssemblyProcessRoutine(string pMsg, string cMsg, Action onComplete)
    {
        done = false;

        StartCoroutine(UpdateTextRoutine(pMsg));

        // 개별 오브젝트에게 할당된 시간 (N / m)
        float timePerObject = totalDuration / targetObjects.Length;

        for (int i = 0; i < targetObjects.Length; i++)
        {
            // 각 오브젝트의 이동이 끝날 때까지 대기 (순차 이동)
            yield return StartCoroutine(MoveSingleObject(i, timePerObject));
        }

        StopAllCoroutines(); // 텍스트 루틴 중지
        statusText.text = cMsg;
        onComplete?.Invoke();
        _masterRoutine = null;

        done = true;
    }

    /// <summary>
    /// 초기 위치와 회전값을 1회 저장
    /// </summary>
    private void SaveOriginalStates()
    {
        int count = targetObjects.Length;
        _savedPositions = new Vector2[count];
        _savedRotations = new Quaternion[count];

        for (int i = 0; i < count; i++)
        {
            _savedPositions[i] = targetObjects[i].anchoredPosition;
            _savedRotations[i] = targetObjects[i].localRotation;
        }
        _isInitialized = true;
    }

    /// <summary>
    /// 위치 분산 및 랜덤 회전 부여
    /// </summary>
    private void ScatterObjects()
    {
        for (int i = 0; i < targetObjects.Length; i++)
        {
            targetObjects[i].anchoredPosition = GetRandomPositionOutsideCenter();
            // Z축 기준 0~360도 랜덤 회전
            targetObjects[i].localRotation = Quaternion.Euler(0, 0, UnityEngine.Random.Range(0f, 360f));
        }
    }

    /// <summary>
    /// 특정 인덱스의 오브젝트를 할당된 시간 동안 이동 및 회전 복구
    /// </summary>
    private IEnumerator MoveSingleObject(int index, float duration)
    {
        float elapsed = 0;
        RectTransform target = targetObjects[index];
        Vector2 startPos = target.anchoredPosition;
        Quaternion startRot = target.localRotation;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsed / duration);

            target.anchoredPosition = Vector2.Lerp(startPos, _savedPositions[index], t);
            target.localRotation = Quaternion.Lerp(startRot, _savedRotations[index], t);

            yield return null;
        }

        // 최종 값 보정
        target.anchoredPosition = _savedPositions[index];
        target.localRotation = _savedRotations[index];

        SoundManager.Instance.PlayEffect(SoundNames.STAMP);
    }

    private Vector2 GetRandomPositionOutsideCenter()
    {
        Vector2 size = container.rect.size;
        Vector2 pos;
        int safety = 0;
        do
        {
            pos = new Vector2(UnityEngine.Random.Range(-size.x / 2, size.x / 2),
                             UnityEngine.Random.Range(-size.y / 2, size.y / 2));
            safety++;
        } while (innerBoundary.rect.Contains(pos) && safety < 50);
        return pos;
    }

    private IEnumerator UpdateTextRoutine(string msg)
    {
        int count = 0;
        while (true)
        {
            statusText.text = msg + new string('.', count % 4);
            count++;
            yield return new WaitForSeconds(dotInterval);
        }
    }

    public void OnClickClose()
    {
        SoundManager.Instance.PlayClickUI();
        if (!done)
        {
            CustomDebug.PrintW("아직 애니메이션이 종료되지 않았습니다.");
            return;
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}