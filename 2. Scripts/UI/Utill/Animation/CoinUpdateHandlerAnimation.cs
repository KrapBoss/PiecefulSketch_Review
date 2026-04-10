using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(CanvasGroup), typeof(RectTransform))]
public class CoinUpdateHandlerAnimation : UIAnimationParent
{
    [Header("Animation Settings")]
    [SerializeField] private RectTransform startPosition;
    [SerializeField] private RectTransform endPosition;
    [SerializeField] private AnimationCurve moveCurve = new AnimationCurve(
        new Keyframe(0.0f, 0.0f, 2.5f, 2.5f), 
        new Keyframe(0.8f, 1.1f, 0, 0), 
        new Keyframe(1.0f, 1.0f, 0, 0));
    [SerializeField] private float hideDelay = 2.0f;

    [Header("Coin Update")]
    [SerializeField] private TextMeshProUGUI _textCoin;
    [SerializeField] private float countDuration = 0.5f;
    [SerializeField] private float _waitDuration = 0.5f;
    [SerializeField] private float _bounceSpeed = 15f;
    [SerializeField] private float _bounceScale = 0.2f;

    [Header("Component")]
    [SerializeField] CanvasGroup _canvasGroup;
    [SerializeField] RectTransform _rectTransform;
    [SerializeField] GameObject _button;
    
    private Coroutine _animationCoroutine;
    private Coroutine _updateCoinCoroutine;
    private int _currentCoin = 0;

    #region Unity Lifecycle

    private void Awake()
    {
        _rectTransform.anchoredPosition = startPosition.anchoredPosition;
    }

    private void Reset()
    {
        // Create a default overshoot curve for the inspector
        moveCurve = new AnimationCurve(
            new Keyframe(0.0f, 0.0f, 2.5f, 2.5f),
            new Keyframe(0.8f, 1.1f, 0, 0),
            new Keyframe(1.0f, 1.0f, 0, 0)
        );
    }

    #endregion

    #region Animation Control

    public override void Show()
    {        
        if (_animationCoroutine != null) StopAllCoroutines();
        _animationCoroutine = StartCoroutine(ShowAndHideCoroutine());
    }

    public override void Hide()
    {
        if (_animationCoroutine != null) StopCoroutine(_animationCoroutine);
        _animationCoroutine = StartCoroutine(HideAnimationCoroutine());
        Stop(); // Stop coin update coroutine as well
    }

    private IEnumerator ShowAndHideCoroutine()
    {
        // 1. Show
        _button?.SetActive(false);
        _canvasGroup.alpha = 1f;
        if (startPosition != null)
        {
            _rectTransform.anchoredPosition = startPosition.anchoredPosition;
        }

        float timer = 0f;
        Vector2 start = startPosition.anchoredPosition;
        Vector2 end = endPosition.anchoredPosition;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;
            _rectTransform.anchoredPosition = Vector2.LerpUnclamped(start, end, moveCurve.Evaluate(t));
            yield return null;
        }
        _rectTransform.anchoredPosition = end;

        // 2. Update Coin
        StartUpdate(_currentCoin, PlayerData.Coin);
        while (IsRunning())
        {
            yield return null;
        }

        // 3. Wait
        yield return new WaitForSeconds(hideDelay);

        // 4. Hide
        yield return StartCoroutine(HideAnimationCoroutine());
        _button?.SetActive(true);
    }

    private IEnumerator HideAnimationCoroutine()
    {
        float timer = 0f;
        Vector2 start = _rectTransform.anchoredPosition;
        Vector2 end = startPosition.anchoredPosition;
        float startAlpha = _canvasGroup.alpha;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;
            _rectTransform.anchoredPosition = Vector2.Lerp(start, end, t);
            _canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);
            yield return null;
        }

        _rectTransform.anchoredPosition = end;
        _canvasGroup.alpha = 0f;
        _animationCoroutine = null;
    }
    
    #endregion
    
    #region Coin Update Logic

    public void StartUpdate(int startCoin, int targetCoin)
    {
        if (_updateCoinCoroutine != null)
        {
            StopCoroutine(_updateCoinCoroutine);
            _textCoin.transform.localScale = Vector3.one;
        }
        _currentCoin = startCoin;
        _updateCoinCoroutine = StartCoroutine(UpdateCoinCoroutine(targetCoin));
    }

    private IEnumerator UpdateCoinCoroutine(int targetCoin)
    {
        int startCoin = _currentCoin;

        if (startCoin == targetCoin)
        {
            _updateCoinCoroutine = null;
            yield break;
        }

        float timer = 0f;

        while (timer < countDuration) // Use 'countDuration'
        {
            timer += Time.deltaTime;
            float t = timer / countDuration;

            int val = (int)Mathf.Lerp(startCoin, targetCoin, t);
            _currentCoin = val;
            UpdateText(_currentCoin);

            float scaleAdd = Mathf.Abs(Mathf.Sin(timer * _bounceSpeed)) * _bounceScale;
            _textCoin.transform.localScale = Vector3.one * (1.0f + scaleAdd);

            yield return null;
        }

        _currentCoin = targetCoin;
        UpdateText(_currentCoin);
        _textCoin.transform.localScale = Vector3.one;

        yield return new WaitForSeconds(_waitDuration);

        _updateCoinCoroutine = null;
    }

    private void UpdateText(int coin)
    {
        if (_textCoin != null)
        {
            _textCoin.text = Custom.CustomCalculator.TFCoinString(coin);
        }
    }

    public bool IsRunning()
    {
        return _updateCoinCoroutine != null;
    }

    public void Stop()
    {
        if (_updateCoinCoroutine != null)
        {
            StopCoroutine(_updateCoinCoroutine);
            if (_textCoin != null)
            {
                _textCoin.transform.localScale = Vector3.one;
            }
            _updateCoinCoroutine = null;
        }
    }

    public int GetCurrentCoin()
    {
        return _currentCoin;
    }
    
    #endregion
}