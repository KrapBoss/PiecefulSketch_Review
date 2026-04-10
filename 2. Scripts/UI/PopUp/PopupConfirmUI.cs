using System;
using System.Threading.Tasks;
using Global;
using Managers;
using TMPro;
using UI.Core;
using UnityEngine;
using UnityEngine.UI;

namespace UI.PopUp
{
    public class PopupConfirmUI : UIBase
    {
        // 제목과 내용 Text 컴포넌트
        public TextMeshProUGUI TitleText;
        public TextMeshProUGUI ContentText;

        // 버튼 컴포넌트
        public Button ConfirmButton;
        public Button CloseButton;
        
        // 버튼 클릭 시 실행될 액션
        private Action _confirmAction;
        private Action _closeAction;

        protected override void Awake()
        {
            base.Awake();
            ConfirmButton.onClick.AddListener(OnConfirmButtonClicked);
            CloseButton.onClick.AddListener(OnCloseButtonClicked);
        }

        /// <summary>
        /// 팝업을 초기화하고 내용을 설정합니다.
        /// </summary>
        /// <param name="title">제목 텍스트</param>
        /// <param name="content">내용 텍스트</param>
        /// <param name="confirmAction">확인 버튼 클릭 시 실행될 콜백</param>
        /// <param name="closeAction">닫기 버튼 클릭 시 실행될 콜백</param>
        public void Initialize(string title, string content, Action confirmAction = null, Action closeAction = null)
        {
            TitleText.text = Localization.Localize(title);
            ContentText.text = Localization.Localize(content);
            _confirmAction = confirmAction;
            _closeAction = closeAction;
        }

        /// <summary>
        /// 확인 버튼 클릭 시 호출되는 함수입니다.
        /// </summary>
        private void OnConfirmButtonClicked()
        {
            _confirmAction?.Invoke();
            UIScreenManager.Instance.CloseUI(UIName.POPUP_CONFIRM_UI);
            UIScreenManager.Instance.CloseUI(UIName.POPUP_CONFIRM_UI_LOCAL);
        }

        /// <summary>
        /// 닫기 버튼 클릭 시 호출되는 함수입니다.
        /// </summary>
        private void OnCloseButtonClicked()
        {
            _closeAction?.Invoke();
            UIScreenManager.Instance.CloseUI(UIName.POPUP_CONFIRM_UI);
            UIScreenManager.Instance.CloseUI(UIName.POPUP_CONFIRM_UI_LOCAL);
        }

        private void OnDestroy()
        {
            ConfirmButton.onClick.RemoveListener(OnConfirmButtonClicked);
            CloseButton.onClick.RemoveListener(OnCloseButtonClicked);
        }
        
        /// <summary>
        /// 확인/취소 팝업을 생성하고 즉시 표시합니다.
        /// </summary>
        /// <param name="title">제목 텍스트</param>
        /// <param name="content">내용 텍스트</param>
        /// <param name="confirmAction">확인 버튼 클릭 시 실행될 콜백</param>
        /// <param name="closeAction">닫기 버튼 클릭 시 실행될 콜백</param>
        /// <returns>생성된 팝업의 인스턴스</returns>
        public static async void Show(string title, string content, Action confirmAction = null, Action closeAction = null)
        {
            await UIScreenManager.Instance.ShowUI(UIName.POPUP_CONFIRM_UI);
            var uiBase = UIScreenManager.Instance.GetUI(UIName.POPUP_CONFIRM_UI);
            if (uiBase is PopupConfirmUI popup)
            {
                Debug.Log("[PopupConfirmUI] : Loaded");
                popup.Initialize(title, content, confirmAction, closeAction);
            }
        }

        /// <summary>
        /// 확인/취소 팝업을 생성하고 즉시 표시합니다.
        /// </summary>
        /// <param name="title">제목 텍스트</param>
        /// <param name="content">내용 텍스트</param>
        /// <param name="confirmAction">확인 버튼 클릭 시 실행될 콜백</param>
        /// <param name="closeAction">닫기 버튼 클릭 시 실행될 콜백</param>
        /// <returns>생성된 팝업의 인스턴스</returns>
        public static async void ShowLocal(string title, string content, Action confirmAction = null, Action closeAction = null)
        {
            await UIScreenManager.Instance.ShowUI(UIName.POPUP_CONFIRM_UI);
            var uiBase = UIScreenManager.Instance.GetUI(UIName.POPUP_CONFIRM_UI);
            if (uiBase is PopupConfirmUI popup)
            {
                Debug.Log("[PopupConfirmUI] : Loaded");
                popup.Initialize(title, content, confirmAction, closeAction);
            }
        }

    }
}
