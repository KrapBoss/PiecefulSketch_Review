using System; // Action 사용을 위해 추가
using System.Collections;
using UnityEngine;

public class UiAnimationRoot : MonoBehaviour
{
    public bool isAwake;
    public bool isStart;
    public bool isOnEnable;

    public UIAnimationParent[] units;
    public float interval = 0.0f;

    private void Awake()
    {
        if (isAwake) Show();
    }

    void Start()
    {
        if (isStart) Show();
    }

    private void OnEnable()
    {
        if (isOnEnable) Show();
    }

    public void OnDisable()
    {
        StopAllCoroutines();
        // units가 null일 경우를 대비한 null 체크
        if (units != null)
        {
            foreach (var unit in units)
            {
                // unit이 파괴되었거나 null인 경우 방지
                if (unit != null) unit.StopAllCoroutines();
            }
        }
    }

    /// <summary>
    /// 애니메이션 실행
    /// </summary>
    /// <param name="onComplete">애니메이션이 모두 끝난 후 실행할 콜백</param>
    public void Show(Action onComplete = null)
    {
        StopAllCoroutines();
        StartCoroutine(ShowCor(onComplete));
    }

    IEnumerator ShowCor(Action onComplete)
    {
        WaitForSeconds waitTime = new WaitForSeconds(interval);

        if (units != null)
        {
            foreach (var unit in units)
            {
                if (unit == null) continue; // 방어 코드

                unit.Show();
                yield return waitTime;
            }
        }

        // 기존 GetDuration 활용하여 남은 시간 대기
        float totalDuration = GetDuration();

        yield return new WaitForSeconds(totalDuration);

        // 완료 보고
        onComplete?.Invoke();
    }

    public float GetDuration()
    {
        float maxEndTime = 0f;

        for (int i = 0; i < units.Length; i++)
        {
            // 시작 시간 = 순서(i) * 간격
            float startTime = i * interval;
            float endTime = startTime + units[i].duration;

            // 가장 늦게 끝나는 시간 갱신
            if (endTime > maxEndTime)
            {
                maxEndTime = endTime;
            }
        }

        return maxEndTime * 1.05f;
    }

}