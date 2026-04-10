using System;
using System.Collections;
using UnityEngine;
using Managers;
using System.Collections.Generic;

namespace UI.Core
{
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class UIBase : MonoBehaviour
    {
        public bool IsOnlyOne = false;
        protected bool IsInit = false;

        /// <summary> 참조한 리소스 이름을 저장 </summary>
        protected List<string> ResourceKey = new();

        public CanvasGroup CanvasGroup { get; private set; }

        [Header("Animation Settings")]
        // 열릴 때 연출 (예: 팝업 커짐)
        public UiAnimationRoot OpenAnimation;
        // 닫힐 때 연출 (예: 팝업 작아짐 - 별도 오브젝트)
        public UiAnimationRoot CloseAnimation;

        protected virtual void Awake()
        {
            CanvasGroup = GetComponent<CanvasGroup>();
            if (CanvasGroup == null)
            {
                CanvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        public virtual void OnShow()
        {
            if (IsInit)
            {
                CanvasGroup.alpha = 1.0f;
                CanvasGroup.blocksRaycasts = true;
                CanvasGroup.interactable = true;
            }
        }

        public virtual void OnHide()
        {
            // 화면엔 보이지만 조작 불가능 상태
            CanvasGroup.blocksRaycasts = false;
            CanvasGroup.interactable = false;
        }

        public virtual void Init() { }

        public void CloseUI()
        {
            // Managers.UIScreenManager 접근은 유지
            UIScreenManager.Instance.OnBack();
        }

        /// <summary>
        /// UI가 처음 생성될 때 호출되는 함수입니다.
        /// 애니메이션을 위해 사용됩니다.
        /// </summary>
        public void OnFirstShowUI()
        {
            IsInit = false;
            OnHide(); // 초기화 중 터치 방지
            StopAllCoroutines();

            if (OpenAnimation != null)
            {
                // 열기 애니메이션 실행 후 -> 초기화 완료 처리
                OpenAnimation.Show(InitTrue);
            }
            else
            {
                InitTrue();
            }
        }

        void InitTrue()
        {
            Debug.Log($"[{gameObject.name}] InitTrue");
            IsInit = true;
            OnShow();
        }

        /// <summary>
        /// UI 닫기 프로세스
        /// </summary>
        /// <param name="destroyAction">애니메이션 종료 후 실행할 파괴 로직</param>
        /// <param name="immediately">즉시 종료 여부</param>
        public virtual void OnClose(Action destroyAction, bool immediately)
        {
            // 1. 즉시 상호작용 차단 (중복 클릭 방지)
            OnHide();

            foreach (var item in ResourceKey) ResourceManager.Instance.ReleaseAsset(item);

            // 2. 즉시 종료거나 애니메이션이 없으면 바로 파괴
            if (immediately || CloseAnimation == null)
            {
                destroyAction?.Invoke();
                return;
            }

            // 3. 닫기 애니메이션이 있다면 실행하고, 끝난 뒤 파괴
            // CloseAnimation을 'Show(재생)' 함으로써 닫히는 연출을 보여줌
            CloseAnimation.Show(() =>
            {
                destroyAction?.Invoke();
            });
        }
    }
}