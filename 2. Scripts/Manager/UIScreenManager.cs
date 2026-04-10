using Custom;
using Global;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using TMPro;
using UI.Core;
using UI.PopUp;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Managers
{
    /// <summary>
    /// 모든 UI의 생명주기를 관리하는 중앙 관리자입니다.
    /// UI는 Stack 자료구조와 유사한 List로 관리되며, 가장 마지막에 생성된 UI가 최상단에 노출됩니다.
    /// </summary>
    public class UIScreenManager : MonoBehaviour
    {
        private static UIScreenManager _instance;
        public static UIScreenManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject(nameof(UIScreenManager));
                    _instance = go.AddComponent<UIScreenManager>();
                }
                return _instance;
            }
        }

        // 생성된 모든 UI를 캐싱하여 중복 생성을 방지합니다. Key: UIName, Value: UIBase
        private readonly Dictionary<string, UIBase> _cachedUIs = new Dictionary<string, UIBase>();

        // 현재 활성화된(스택에 쌓인) UI의 순서를 관리합니다.
        private readonly List<UIBase> _uiStack = new List<UIBase>();
        private readonly HashSet<string> _pendingUIs = new HashSet<string>();

        
        /// <summary> 씬 처음입장 시 베이스가 제일 하단에 생성되는 UI 이름 </summary>
        [SerializeField] private string BaseUIName;

        // [추가] 글로벌 폰트 에셋
        private static TMP_FontAsset _globalFont = null;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Debug.LogError("UIScreenManager가 이미 존재하여 새로 생성된 객체를 파괴합니다.");
                Destroy(gameObject);
                return;
            }

            if (_instance == null)
            {
                _instance = this;
            }

            if (!string.IsNullOrEmpty(BaseUIName))
            {
                // async 메서드를 Awake에서 호출할 때는 await 할 수 없으므로,
                // Task를 무시하는 방식으로 호출합니다.
                _ = ShowUI(BaseUIName);
            }

            this.gameObject.name = "UIScreenManager";
        }

        private void Start()
        {
        }

        private void Update()
        {
            // 뒤로가기 버튼(Android: Back, Editor/PC: Escape) 감지
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                var confirmUI = GetUI(UIName.POPUP_CONFIRM_UI);

                // 종료 확인 팝업이 이미 활성화되어 있다면 닫기
                if (confirmUI != null && confirmUI.gameObject.activeInHierarchy)
                {
                    CloseUI(UIName.POPUP_CONFIRM_UI);
                }
                else
                {
                    SoundManager.Instance.PlayClickUI();
                    if (SceneLoader.SceneInformation != null && SceneLoader.SceneInformation.SceneName.Equals(GameConfig.SingleSceneName))
                    {   // 타이틀 씬인 경우
                        PopupConfirmUI.Show(
                            "Return",
                            "Go to Gallery?",
                            () => SceneLoader.LoadLoaderScene(SceneName.Title),
                            null
                        );
                    }
                    else
                    {   // 타이틀 씬인 경우
                        PopupConfirmUI.Show(
                            "Game Quit",
                            "Quit the game?",
                            QuitApplication,
                            null
                        );
                    }
                    
                }
            }
        }

        /// <summary> 앱 종료 전 데이터 저장 판단 진행 </summary>
        async void QuitApplication()
        {
            if (SaveDataChecker.Instance.CheckSaving())
            {
                CustomDebug.PrintW("[Save] 데이터 저장 필요.");

                await LoadingUI.Show("Quit after Save");

                await SaveDataChecker.Instance.SaveDataOutter(QuitApp);
            }
            else
            {
                CustomDebug.PrintW("[Save] 데이터 저장 불필요");
                
                QuitApp();
            }
        }

        void QuitApp()
        {

#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void OnDestroy()
        {
            if(_instance == this)
                _instance = null;
        }

        /// <summary>
        /// 지정된 이름의 UI를 화면에 표시합니다.
        /// </summary>
        /// <param name="uiName">UIName 구조체에 정의된 UI 이름</param>
        public async Task<UIBase> ShowUI(string uiName)
        {
            Debug.Log($"[UIScreenManager] ShowUI 요청: {uiName}");

            if (string.IsNullOrEmpty(uiName))
            {
                Debug.LogError("[UIScreenManager] UI 이름이 유효하지 않습니다.");
                return null;
            }

            // [수정] UI가 이미 로딩 중인지 확인하여 중복 생성을 방지합니다.
            if (_pendingUIs.Contains(uiName))
            {
                Debug.LogWarning($"[UIScreenManager] {uiName} UI가 이미 생성 중입니다. 중복 요청을 무시합니다.");
                return null;
            }

            // 1. UI가 이미 캐시에 있는지 확인
            if (_cachedUIs.TryGetValue(uiName, out UIBase cachedUI))
            {
                Debug.Log($"[UIScreenManager] 캐시된 UI를 사용합니다: {uiName}");
                if (cachedUI.IsOnlyOne)
                {
                    // 자기 자신을 제외한 모든 UI를 닫는다.
                    await CloseAllUIs(cachedUI);
                }

                MoveToTop(cachedUI);
                UpdateUIStack();
                return cachedUI;
            }

            try
            {
                _pendingUIs.Add(uiName);

                // 2. 캐시에 없다면 새로 생성
                GameObject prefab = await ResourceManager.Instance.LoadAsset<GameObject>(uiName, true);
                if (prefab == null)
                {
                    Debug.LogError($"[UIScreenManager] {uiName} 경로에서 프리팹을 찾을 수 없습니다.");
                    return null;
                }

                var uiObject = Instantiate(prefab, transform);
                uiObject.name = uiName;

                UIBase newUI = uiObject.GetComponent<UIBase>();
                if (newUI == null)
                {
                    Debug.LogError($"[UIScreenManager] {uiName} 프리팹에 UIBase를 상속받는 컴포넌트가 없습니다.");
                    Destroy(uiObject);
                    return null;
                }


                try
                {
                    // [추가] 생성된 UI의 폰트 일괄 적용
                    ApplyFontToUI(newUI);
                    //첫 애니메이션을 위한 호출
                    newUI.OnFirstShowUI();
                    // UI가 처음 생성되었을 때 단 한번 호출
                    newUI.Init();
                }
                catch(System.Exception e)
                {
                    Debug.LogError($"{uiName} : UI 초기화 문제 발생 : {e.InnerException.Message}");
                }

                if (newUI.IsOnlyOne)
                {
                    await CloseAllUIs();
                }

                // 3. 생성된 UI를 캐시와 스택에 추가
                _cachedUIs.Add(uiName, newUI);

                Debug.Log($"[UIScreenManager] stack 등록 1 {uiName} : {(_uiStack.Count > 0 ? _uiStack.Last().gameObject.name : "NULL")}");
                if (!_uiStack.Contains(newUI))
                {
                    _uiStack.Add(newUI);
                    Debug.Log($"[UIScreenManager] stack 등록 2 {uiName} : {_uiStack.Last().gameObject.name}");
                }


                UpdateUIStack();
                return newUI;
            }
            catch (System.Exception e)
            {
                // 최상위 catch에서도 안전하게 전체 정보를 출력
                Debug.LogError($"[UIScreenManager] {uiName} 생성 중 치명적 실패: {e}");
                return null;
            }
            finally
            {
                // 작업이 성공하든 실패하든, 로딩 상태를 해제합니다.
                _pendingUIs.Remove(uiName);
            }
        }

        /// <summary>
        /// 지정된 이름의 UI를 닫습니다. BaseUI는 닫을 수 없습니다.
        /// </summary>
        /// <param name="uiName">닫고자 하는 UI의 이름</param>
        /// <param name="immediately"> 애니메이션이 있어도 즉시 UI를 제거합니다. </param>
        public void CloseUI(string uiName, bool immediately = false)
        {
            Debug.Log($"[UIScreenManager] CloseUI {uiName}");
            // BaseUIName은 닫기/제거 불가
            if (!string.IsNullOrEmpty(BaseUIName) && uiName == BaseUIName)
            {
                Debug.LogWarning($"{uiName}은 BaseUI이므로 닫을 수 없습니다.");
                return;
            }

            if (_cachedUIs.TryGetValue(uiName, out UIBase uiToClose))
            {
                uiToClose.OnClose(() => CloseUIAction(uiName), immediately);
            }
        }

        /// <summary>
        /// UI가 닫히면서 실행할 애니메이션
        /// </summary>
        /// <param name="uiName"></param>
        private void CloseUIAction(string uiName)
        {
            if (_cachedUIs.TryGetValue(uiName, out UIBase uiToClose))
            {
                _uiStack.Remove(uiToClose);
                _cachedUIs.Remove(uiName);

                Destroy(uiToClose.gameObject);

                ResourceManager.Instance.ReleaseAsset(uiName);

                UpdateUIStack();
            }
        }
        
        /// <summary>
        /// 현재 활성화된 최상단 UI를 닫습니다. (ex. 뒤로가기 버튼)
        /// </summary>
        public void CloseTopUI()
        {
            Debug.Log($"[UIScreenManager] CloseTopUI");
            if (_uiStack.Count > 0)
            {
                UIBase topUI = _uiStack.Last();
                Debug.Log($"[UIScreenManager] CloseTopUI {topUI.name}");
                CloseUI(topUI.name, false);
            }
        }


        /// <summary>
        /// 현재 스택에 있는 모든 UI를 순차적으로 닫습니다. (BaseUI 제외)
        /// </summary>
        /// <param name="excludeUI">닫기에서 제외할 UI</param>
        public async Task CloseAllUIs(UIBase excludeUI = null)
        {
            // 뒤에서부터 순회하며 안전하게 제거
            for (int i = _uiStack.Count - 1; i >= 0; i--)
            {
                // 제외할 UI는 건너뛰기
                if (excludeUI != null && _uiStack[i] == excludeUI)
                {
                    continue;
                }
                CloseUI(_uiStack[i].name, true);
            }
            await Task.Yield(); // 한 프레임 대기하여 파괴 로직이 완료되도록 보장
        }

        /// <summary>
        /// 현재 캐시된(생성된) UI 중에서 특정 이름의 UI를 찾습니다.
        /// </summary>
        /// <param name="uiName">찾고자 하는 UI의 이름</param>
        /// <returns>찾은 UI, 없을 경우 null</returns>
        public UIBase GetUI(string uiName)
        {
            if (string.IsNullOrEmpty(uiName) || !_cachedUIs.ContainsKey(uiName))
            {
                return null;
            }
            return _cachedUIs[uiName];
        }

        /// <summary>
        /// 최상단 UI 하나를 닫습니다.
        /// </summary>
        public void OnBack()
        {
            CloseTopUI();
        }

        /// <summary>
        /// 현재 열려있는 모든 UI를 닫습니다.
        /// </summary>
        public async Task OnBackAll()
        {
            await CloseAllUIs();
        }

        /// <summary>
        /// 이미 생성된 UI를 스택의 최상단으로 이동시킵니다.
        /// </summary>
        private void MoveToTop(UIBase ui)
        {
            if (_uiStack.Contains(ui))
            {
                _uiStack.Remove(ui);
            }
            _uiStack.Add(ui);
            ui.transform.SetAsLastSibling(); // Hierarchy 뷰에서도 최상단으로 변경
        }

        /// <summary>
        /// UI 스택을 순회하며 각 UI의 상태(활성/비활성)를 업데이트합니다.
        /// </summary>
        private void UpdateUIStack()
        {
            if (_uiStack.Count == 0) return;

            for (int i = 0; i < _uiStack.Count; i++)
            {
                // 스택의 가장 마지막 요소가 최상단 UI
                if (i == _uiStack.Count - 1)
                {
                    _uiStack[i].OnShow();
                }
                else
                {
                    _uiStack[i].OnHide();
                }
            }
        }

        // =================================================================
        // [추가] 폰트 관리 로직
        // =================================================================

        /// <summary>
        /// ResourceNames에 정의된 글로벌 폰트를 어드레서블로 로드합니다.
        /// 로드 완료 시 캐시된 모든 UI에 폰트를 적용합니다.
        /// </summary>
        public async Task LoadGlobalFontAsync()
        {
            try
            {
                // 폰트 로드
                _globalFont = await ResourceManager.Instance.LoadAsset<TMP_FontAsset>(ResourceNames.FONT_DEFAULT);

                if (_globalFont != null)
                {
                    Debug.Log($"[UIScreenManager] 글로벌 폰트 로드 완료: {ResourceNames.FONT_DEFAULT}");
                    ApplyFontToAllCachedUIs();
                }
                else
                {
                    Debug.LogError($"[UIScreenManager] 글로벌 폰트 로드 실패: {ResourceNames.FONT_DEFAULT}");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[UIScreenManager] 폰트 로딩 중 예외 발생: {e.Message}");
            }
        }

        /// <summary>
        /// 현재 캐시된 모든 UI에 글로벌 폰트를 적용합니다.
        /// </summary>
        private void ApplyFontToAllCachedUIs()
        {
            // 나중에 사용할 예정이므로 로직은 보존
            if (_globalFont == null) return;

            foreach (var ui in _cachedUIs.Values)
            {
                ApplyFontToUI(ui);
            }
        }

        /// <summary>
        /// 특정 UI의 하위 TextMeshProUGUI 컴포넌트들의 폰트를 변경합니다.
        /// </summary>
        private void ApplyFontToUI(UIBase ui)
        {
            if (_globalFont == null || ui == null) return;

            // 비활성화된 객체까지 포함하여 모든 텍스트 컴포넌트 검색
            var texts = ui.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var text in texts)
            {
                // 로컬라이징 적용 및 폰트 설정
                text.text = Localization.Localize(text.text);
                text.font = _globalFont;
            }
        }
    }
}