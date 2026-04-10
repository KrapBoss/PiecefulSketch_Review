using System;
using System.Collections;
using UnityEngine;
using Global;
using Google.Play.AppUpdate;
using Google.Play.Common;

namespace Managers
{
    public class AppUpdateManager
    {
        private Google.Play.AppUpdate.AppUpdateManager _googleAppUpdateManager;
        private AppUpdateInfo _latestAppUpdateInfo;

        public AppUpdateManager()
        {
            _googleAppUpdateManager = new Google.Play.AppUpdate.AppUpdateManager();
        }

        /// <summary>
        /// 구글 플레이 업데이트가 있는지 확인합니다.
        /// </summary>
        /// <param name="coroutineRunner">코루틴을 실행할 MonoBehaviour</param>
        /// <param name="onUpdateAvailable">업데이트 가능 여부 콜백 (true: 업데이트 있음)</param>
        public void CheckForUpdate(MonoBehaviour coroutineRunner, Action<bool> onUpdateAvailable)
        {
            // 네트워크 확인
            if (!NetworkMonitor.IsConnected)
            {
                onUpdateAvailable?.Invoke(false);
                return;
            }

            // 에디터에서는 동작하지 않으므로 패스
            if (Application.isEditor)
            {
                Debug.Log("[AppUpdateManager] Editor mode: Skipping update check.");
                onUpdateAvailable?.Invoke(false);
                return;
            }

            coroutineRunner.StartCoroutine(CheckForUpdateCoroutine(onUpdateAvailable));
        }

        private IEnumerator CheckForUpdateCoroutine(Action<bool> onUpdateAvailable)
        {
            PlayAsyncOperation<AppUpdateInfo, AppUpdateErrorCode> appUpdateInfoOperation = 
                _googleAppUpdateManager.GetAppUpdateInfo();

            yield return appUpdateInfoOperation;

            if (appUpdateInfoOperation.IsSuccessful)
            {
                _latestAppUpdateInfo = appUpdateInfoOperation.GetResult();
                
                // UpdateAvailability.UpdateAvailable == 2
                if (_latestAppUpdateInfo.UpdateAvailability == UpdateAvailability.UpdateAvailable)
                {
                    Debug.Log("[AppUpdateManager] Update is available.");
                    onUpdateAvailable?.Invoke(true);
                }
                else
                {
                    Debug.Log($"[AppUpdateManager] No update available. Status: {_latestAppUpdateInfo.UpdateAvailability}");
                    onUpdateAvailable?.Invoke(false);
                }
            }
            else
            {
                Debug.LogError($"[AppUpdateManager] Check failed: {appUpdateInfoOperation.Error}");
                onUpdateAvailable?.Invoke(false);
            }
        }

        /// <summary>
        /// 즉시 업데이트 흐름을 시작합니다.
        /// </summary>
        public void StartImmediateUpdate(MonoBehaviour coroutineRunner, Action onFlowFinished, Action onFlowFailed)
        {
            if (_latestAppUpdateInfo == null)
            {
                Debug.LogError("[AppUpdateManager] Cannot start update. AppUpdateInfo is null.");
                onFlowFinished?.Invoke();
                return;
            }

            coroutineRunner.StartCoroutine(StartImmediateUpdateCoroutine(onFlowFinished, onFlowFailed));
        }

        /// <summary>
        /// 업데이트 진행
        /// </summary>
        /// <param name="onFlowFinished"></param>
        /// <returns></returns>
        private IEnumerator StartImmediateUpdateCoroutine(Action onFlowFinished, Action onFlowFailed)
        {
            var options = AppUpdateOptions.ImmediateAppUpdateOptions();
            var startUpdateRequest = _googleAppUpdateManager.StartUpdate(_latestAppUpdateInfo, options);

            yield return startUpdateRequest;

            // Immediate update의 경우 성공 시 앱이 재시작되거나 종료되므로,
            // 여기에 도달했다면 사용자가 취소했거나 실패한 경우일 가능성이 큽니다.
            if (startUpdateRequest.Status == AppUpdateStatus.Failed || startUpdateRequest.Status == AppUpdateStatus.Canceled)
            {
                Debug.LogWarning($"[AppUpdateManager] Update flow finished with status: {startUpdateRequest.Status}");
                onFlowFailed.Invoke();
            }
            else
            {
                onFlowFinished?.Invoke();
            }
        }

        /// <summary>
        /// 구글 플레이 스토어 페이지로 이동합니다. (Fallback)
        /// </summary>
        public void OpenPlayStore()
        {
            string packageName = Application.identifier;
            try
            {
                Application.OpenURL($"market://details?id={packageName}");
            }
            catch (Exception)
            {
                Application.OpenURL($"https://play.google.com/store/apps/details?id={packageName}");
            }
        }
    }
}
