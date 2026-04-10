using System;
using System.Threading.Tasks;
using Global;
using Managers;
using TMPro;
using UI.Core;
using UnityEngine.UI;

namespace UI.PopUp
{
    public class PopupInfoUI : UIBase
    {
        // 제목과 내용 Text 컴포넌트
        public TextMeshProUGUI TitleText;
        public TextMeshProUGUI ContentText;
        
        // 닫기 버튼
        public Button CloseButton;
        
        // 닫기 버튼 클릭 시 실행될 액션
        private Action _closeAction;

        protected override void Awake()
        {
            base.Awake();
            CloseButton.onClick.AddListener(OnCloseButtonClicked);
        }

        /// <summary>
        /// 팝업을 초기화하고 내용을 설정합니다.
        /// </summary>
        /// <param name="title">제목 텍스트</param>
        /// <param name="content">내용 텍스트</param>
        /// <param name="closeAction">닫기 버튼 클릭 시 실행될 콜백</param>
        public void Initialize(string title, string content, Action closeAction = null)
        {
            TitleText.text = Localization.Localize(title);
            ContentText.text = Localization.Localize(content);
            _closeAction = closeAction;
        }

        /// <summary>
        /// 닫기 버튼 클릭 시 호출되는 함수입니다.
        /// </summary>
        private void OnCloseButtonClicked()
        {
            _closeAction?.Invoke();
            UIScreenManager.Instance.CloseUI(UIName.POPUP_INFO_UI);
        }

        private void OnDestroy()
        {
            CloseButton.onClick.RemoveListener(OnCloseButtonClicked);
        }
        
        /// <summary>
        /// 정보 팝업을 생성하고 즉시 표시합니다.
        /// </summary>
        /// <param name="title">제목 텍스트</param>
        /// <param name="content">내용 텍스트</param>
        /// <param name="closeAction">닫기 버튼 클릭 시 실행될 콜백</param>
        /// <returns>생성된 팝업의 인스턴스</returns>
        public static async void Show(string title, string content, Action closeAction = null)
        {
            await UIScreenManager.Instance.ShowUI(UIName.POPUP_INFO_UI);
            var uiBase = UIScreenManager.Instance.GetUI(UIName.POPUP_INFO_UI);
            if (uiBase is PopupInfoUI popup)
            {
                popup.Initialize(title, content, closeAction);
            }
        }
    }
}
