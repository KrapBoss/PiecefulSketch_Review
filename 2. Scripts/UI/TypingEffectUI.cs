using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using Managers;
using System;

/// <summary>
/// 텍스트에 타이핑 효과를 적용하고, 순차적인 문장 표시 및 사용자 상호작용을 관리합니다.
/// 이 컴포넌트가 부착된 GameObject에 반드시 클릭을 감지할 수 있는 UI 요소(예: 투명한 Image)가 있어야 합니다.
/// </summary>
public class TypingEffectUI : MonoBehaviour, IPointerClickHandler
{
    [Header("컴포넌트 설정")]
    [SerializeField]
    [Tooltip("텍스트를 표시할 TextMeshProUGUI 컴포넌트입니다.")]
    private TextMeshProUGUI textMeshPro;

    [SerializeField]
    [Tooltip("글자가 한 글자씩 나타나는 속도입니다. (초 단위)")]
    private float typingSpeed = 0.05f;

    [Header("완료 이벤트")]
    [Tooltip("모든 문장 표시가 완료되었을 때 호출될 이벤트입니다.")]
    public Action OnCompleted;

    private List<string> sentences;
    private int currentIndex = 0;
    private Coroutine typingCoroutine;
    private bool isTyping = false;

    private TutorialGuide _guide;

    public void Set(TutorialGuide guide) => _guide = guide;

    public void Stop()
    {
        StopAllCoroutines();
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 지정된 문장들로 새로운 설명 나레이션을 시작합니다.
    /// </summary>
    /// <param name="newSentences">표시할 문장들의 리스트</param>
    public void StartNarration(List<string> newSentences)
    {
        // 표시할 문장이 없으면 즉시 비활성화하고 종료합니다.
        if (newSentences == null || newSentences.Count == 0)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);
        this.sentences = newSentences;
        this.currentIndex = 0;
        
        StartTyping();
    }

    /// <summary>
    /// 현재 인덱스의 문장에 대한 타이핑 효과를 시작합니다.
    /// </summary>
    private void StartTyping()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }
        _guide?.ShowMascott();
        typingCoroutine = StartCoroutine(TypeText(Localization.Localize(sentences[currentIndex])));
    }

    /// <summary>
    /// 한 문장에 대해 타이핑 효과를 적용하는 코루틴입니다.
    /// HTML 태그와 '\\n'과 같은 이스케이프 시퀀스는 타이핑 효과 없이 즉시 적용합니다.
    /// </summary>
    /// <param name="sentence">타이핑할 문장</param>
    private IEnumerator TypeText(string sentence)
    {
        SoundManager.Instance.PlayEffect($"Talk{UnityEngine.Random.Range(1, 3)}");

        isTyping = true;
        textMeshPro.text = "";
        int i = 0;
        while (i < sentence.Length)
        {
            // 1. 리치 텍스트 태그 처리
            if (sentence[i] == '<')
            {
                int endIndex = sentence.IndexOf('>', i);
                if (endIndex != -1)
                {
                    textMeshPro.text += sentence.Substring(i, endIndex - i + 1);
                    i = endIndex + 1;
                    continue; // 딜레이 없이 다음 루프 실행
                }
            }

            // 2. '\n' 이스케이프 시퀀스 처리
            if (sentence[i] == '\\' && i + 1 < sentence.Length)
            {
                if (sentence[i + 1] == 'n')
                {
                    textMeshPro.text += '\n';
                    i += 2; // '\'와 'n' 두 문자를 건너뜁니다.
                    continue; // 딜레이 없이 다음 루프 실행
                }
            }

            // 3. 일반 문자 처리
            textMeshPro.text += sentence[i];
            i++;
            yield return new WaitForSeconds(typingSpeed);
        }
        isTyping = false;
        typingCoroutine = null;
    }

    /// <summary>
    /// UI가 클릭되었을 때 호출됩니다.
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        SoundManager.Instance.PlayClickUI();

        if (isTyping)
        {
            // 1. 타이핑 중이면 즉시 문장 전체를 표시합니다.
            if (typingCoroutine != null)
            {
                StopCoroutine(typingCoroutine);
                typingCoroutine = null;
            }
            textMeshPro.text = Localization.Localize(sentences[currentIndex]);
            isTyping = false;
        }
        else
        {
            // 2. 타이핑이 끝난 상태면 다음 문장으로 넘어갑니다.
            currentIndex++;
            if (currentIndex < sentences.Count)
            {
                StartTyping();
            }
            else
            {
                // 3. 모든 문장이 끝났으면 완료 이벤트를 호출하고 비활성화합니다.
                OnCompleted?.Invoke();
                gameObject.SetActive(false);
            }
        }
    }
}
