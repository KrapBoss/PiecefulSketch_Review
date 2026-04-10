using DG.Tweening;
using System;
using UnityEngine;
using UnityEngine.UI;

public class RewardObject : MonoBehaviour
{
    private RectTransform _rectTransform;
    private Action<RewardObject> _onComplete; // int 파라미터 제거
    // private int _amount; // 불필요하므로 제거

    private Sequence _moveSequence;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
    }

    /// <summary>
    /// 아이템 이동 및 크기 축소 애니메이션을 실행합니다.
    /// </summary>
    /// <param name="icon">표시할 아이콘</param>
    /// <param name="startWorldPos">시작 월드 좌표</param>
    /// <param name="targetWorldPos">도착 월드 좌표</param>
    /// <param name="onComplete">완료 콜백</param>
    public void FlyTo(Sprite icon, Vector3 startWorldPos, Vector3 targetWorldPos, Action<RewardObject> onComplete)
    {
        GetComponent<Image>().sprite = icon;
        _onComplete = onComplete;

        // 시작 위치 설정 (월드 좌표 기준)
        transform.position = startWorldPos;
        // 크기 초기화 (풀링 고려)
        transform.localScale = Vector3.one;

        // 랜덤한 중간 지점 계산 (Local 기준)
        float spreadRadius = 444.0f;
        Vector2 randomCircle = _rectTransform.anchoredPosition + UnityEngine.Random.insideUnitCircle * spreadRadius;

        float initialMoveDuration = UnityEngine.Random.Range(0.2f, 0.4f);
        float mainMoveDuration = UnityEngine.Random.Range(0.5f, 0.8f);

        // 회전 설정
        float rotationSpeed = 360f / mainMoveDuration;
        Vector3 randomRotationDir = new Vector3(0, 0, UnityEngine.Random.Range(0, 2) == 0 ? -rotationSpeed : rotationSpeed);

        _moveSequence = DOTween.Sequence();

        // 1. 중앙에서 랜덤 위치로 퍼지기
        _moveSequence.Append(_rectTransform.DOAnchorPos(randomCircle, initialMoveDuration).SetEase(Ease.OutQuad));

        // 2. 목표 지점으로 이동하면서 크기 줄이기
        // DOMove와 DOScale을 Join으로 묶어 동시에 실행되도록 설정
        _moveSequence.Append(transform.DOMove(targetWorldPos, mainMoveDuration).SetEase(Ease.InCubic));
        _moveSequence.Join(_rectTransform.DOScale(new Vector3(0.5f,0.5f,0.5f), mainMoveDuration).SetEase(Ease.InQuad));

        // 이동하는 동안 계속 회전
        _rectTransform.DOLocalRotate(randomRotationDir, mainMoveDuration, RotateMode.FastBeyond360)
            .SetEase(Ease.Linear)
            .SetLoops(-1, LoopType.Incremental);

        _moveSequence.OnComplete(OnArrived);
    }

    private void OnArrived()
    {
        // 도착 시 회전 멈춤
        _rectTransform.DOKill(true); // true to complete the tween immediately
        _onComplete?.Invoke(this); // amount 파라미터 제거
    }
    
    /// <summary>
    /// 애니메이션 조기 종료
    /// </summary>
    public void Complete()
    {
        _moveSequence?.Complete();
    }

    /// <summary>
    /// 애니메이션 즉시 중지
    /// </summary>
    public void Stop()
    {
        // OnComplete 콜백이 호출되지 않도록 false 전달
        _moveSequence?.Kill(false); 
        // 이 오브젝트에 연결된 다른 트윈(예: 회전)도 모두 중지
        _rectTransform?.DOKill();
    }
}