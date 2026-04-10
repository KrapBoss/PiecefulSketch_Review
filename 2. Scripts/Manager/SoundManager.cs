using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Managers
{
    public class SoundManager : MonoBehaviour
    {
        static SoundManager instance;
        public static SoundManager Instance
        {
            get => instance;
        }

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                if (instance != this)
                {
                    Destroy(gameObject);
                }
            }
        }

        #region Core Fields

        public AudioSource source_UI;
        public AudioSource source_Effect;
        [Header("BGM")]
        public AudioSource sourceBGM;

        #endregion
        
        #region --------------- SFX (UI & Effect) Management

        private const int MaxSfxCacheSize = 5;
        private readonly Dictionary<string, AudioClip> _sfxClips = new Dictionary<string, AudioClip>();
        private readonly Queue<string> _sfxQueue = new Queue<string>();
        
        // 현재 "로딩 중"인 SFX 작업을 추적합니다. (Key: Address, Value: Loading Task)
        private readonly Dictionary<string, Task<AudioClip>> _loadingSfxTasks = new Dictionary<string, Task<AudioClip>>();
        
        // 현재 재생 중인 SFX 주소를 추적하여 중복 재생을 방지합니다.
        private readonly HashSet<string> _activeSfx = new HashSet<string>();

        #endregion

        #region ------------- BGM Management

        private string _currentBgmAddress;
        private AudioClip _currentBgmClip;
        
        // 현재 "로딩 중"인 BGM 작업을 추적합니다.
        private Task _loadingBgmTask;

        #endregion


        #region --------------- Play Methods (New)

        /// <summary>
        /// UI 효과음을 재생합니다.
        /// </summary>
        public async void PlayClickUI()
        {
            // TODO: Addressable 키로 변경 필요
            string[] clips = new string[] { "Pop1", "Pop2", "Pop3" };
            string address = clips[UnityEngine.Random.Range(0, clips.Length)];
            await PlaySfx(address, source_UI);
        }

        public void CashCoinGet()
        {
            _ = PreCacheSfx(new string[] { SoundNames.COIN_GET1, SoundNames.COIN_GET2 });
        }

        /// <summary>
        /// UI 효과음을 재생합니다.
        /// </summary>
        public async void PlayCoinGet()
        {
            // TODO: Addressable 키로 변경 필요
            string[] clips = new string[] { SoundNames.COIN_GET1, SoundNames.COIN_GET2 };
            string address = clips[UnityEngine.Random.Range(0, clips.Length)];
            await PlaySfx(address, source_UI, true);
        }

        /// <summary>
        /// 범용 효과음을 재생합니다.
        /// </summary>
        public async void PlayEffect(string address, bool force = false)
        {
            await PlaySfx(address, source_Effect, force);
        }
        // <summary>
        /// SFX를 재생합니다. force가 true이면 중복 재생 방지를 무시하고 즉시 출력합니다.
        /// </summary>
        private async Task PlaySfx(string address, AudioSource source, bool force = false)
        {
            if (string.IsNullOrEmpty(address) || source == null) return;

            // 1. 중복 재생 체크 (force가 false일 때만)
            if (!force && _activeSfx.Contains(address)) return;

            AudioClip clipToPlay = null;

            // 2. 캐시 확인
            if (_sfxClips.TryGetValue(address, out clipToPlay))
            {
                PlayAndTrackSfx(address, source, clipToPlay, force);
                return;
            }

            // 3. 로딩 중인 작업 처리
            if (_loadingSfxTasks.TryGetValue(address, out var loadingTask))
            {
                return;
            }

            // 4. 신규 로딩 및 재생
            try
            {
                Task<AudioClip> newLoadTask = LoadAndCacheSfx(address);
                _loadingSfxTasks[address] = newLoadTask;
                clipToPlay = await newLoadTask;

                if (clipToPlay != null) PlayAndTrackSfx(address, source, clipToPlay, force);
            }
            catch (Exception e) { Debug.LogError($"[SoundManager] {address} 재생 실패: {e.Message}"); }
            finally { _loadingSfxTasks.Remove(address); }
        }

        /// <summary> 실제 재생 명령을 내리고 재생 중 상태를 관리합니다. </summary>
        private void PlayAndTrackSfx(string address, AudioSource source, AudioClip clip, bool force)
        {
            source.PlayOneShot(clip, GameSetting.Volume_Effect);

            // force가 아닐 때만 중복 재생 방지 리스트에 등록
            if (!force && _activeSfx.Add(address))
            {
                _ = RemoveSfxAfterDelay(address, clip.length);
            }
        }

        private async Task RemoveSfxAfterDelay(string address, float delay)
        {
            await Task.Delay(TimeSpan.FromSeconds(delay));
            _activeSfx.Remove(address);
        }

        /// <summary>
        /// SFX 리소스를 로드하고 캐시에 저장하는 실제 로직입니다.
        /// </summary>
        private async Task<AudioClip> LoadAndCacheSfx(string address)
        {
            // 캐시가 가득 찼다면 가장 오래된 항목을 제거합니다.
            if (_sfxClips.Count >= MaxSfxCacheSize)
            {
                string oldestSfxAddress = _sfxQueue.Dequeue();
                if (_sfxClips.Remove(oldestSfxAddress))
                {
                    ResourceManager.Instance.ReleaseAsset(oldestSfxAddress);
                    Debug.Log($"[SoundManager] SFX 캐시 초과: '{oldestSfxAddress}' 해제");
                }
            }

            // 리소스를 비동기로 로드합니다.
            AudioClip loadedClip = await ResourceManager.Instance.LoadAsset<AudioClip>(address);

            if (loadedClip != null)
            {
                _sfxClips[address] = loadedClip;
                _sfxQueue.Enqueue(address);
                Debug.Log($"[SoundManager] SFX 로드 및 캐시: '{address}'");
            }
            else
            {
                Debug.LogWarning($"[SoundManager] SFX 클립을 로드할 수 없습니다: '{address}'");
            }

            return loadedClip;
        }


        /// <summary>
        /// BGM을 재생합니다. 로딩 중 중복 호출을 방지합니다.
        /// </summary>
        public async void PlayBGM(string address)
        {
            if (string.IsNullOrEmpty(address)) return;
            if (_currentBgmAddress == address && sourceBGM.isPlaying) return;

            // 다른 BGM이 이미 로딩 중일 경우, 새로운 요청을 무시합니다.
            if (_loadingBgmTask != null && !_loadingBgmTask.IsCompleted)
            {
                Debug.LogWarning($"[SoundManager] BGM이 이미 로딩 중입니다. 새로운 요청 '{address}'은 무시됩니다.");
                return;
            }

            // 새로운 로딩 작업을 시작하고 추적합니다.
            _loadingBgmTask = PlayBGMAsync(address);
            await _loadingBgmTask;
        }

        /// <summary>
        /// BGM을 비동기로 로드하고 교체하는 실제 로직입니다.
        /// </summary>
        private async Task PlayBGMAsync(string address)
        {
            AudioClip newClip = await ResourceManager.Instance.LoadAsset<AudioClip>(address);

            if (newClip == null)
            {
                Debug.LogError($"[SoundManager] BGM 클립을 로드할 수 없습니다: {address}");
                return;
            }

            // 이전에 재생 중이던 BGM이 있다면 리소스를 해제합니다.
            if (!string.IsNullOrEmpty(_currentBgmAddress) && _currentBgmAddress != address)
            {
                ResourceManager.Instance.ReleaseAsset(_currentBgmAddress);
            }

            _currentBgmAddress = address;
            _currentBgmClip = newClip;

            StartCoroutine(ChangeBGMCoroutine(newClip));
        }

        IEnumerator ChangeBGMCoroutine(AudioClip newClip)
        {
            float time = 0;

            if (sourceBGM.isPlaying && sourceBGM.volume > 0.0f)
            {
                while (time < 1.0f)
                {
                    time += Time.deltaTime * 2.0f;
                    sourceBGM.volume = Mathf.Lerp(sourceBGM.volume, 0.0f, time);
                    yield return null;
                }
            }

            time = 0;
            sourceBGM.clip = newClip;
            sourceBGM.Play();
            sourceBGM.loop = true;

            while (time < 1.0f)
            {
                time += Time.deltaTime;
                sourceBGM.volume = Mathf.Lerp(0.0f, GameSetting.Volume, time);
                yield return null;
            }
        }

        public void StopBGM()
        {
            StopAllCoroutines();
            sourceBGM.Stop();
            if (!string.IsNullOrEmpty(_currentBgmAddress))
            {
                ResourceManager.Instance.ReleaseAsset(_currentBgmAddress);
                _currentBgmAddress = null;
                _currentBgmClip = null;
            }
        }

        #endregion

        //BGM 소스를 반환합니다.
        public AudioSource GetBgmSource()
        {
            return sourceBGM;
        }

        /// <summary> 볼륨을 세팅합니다. </summary>
        public void SettingVolume()
        {
            sourceBGM.volume = GameSetting.Volume;
            source_UI.volume = GameSetting.Volume_Effect;
            source_Effect.volume = GameSetting.Volume_Effect;
        }

        /// <summary>
        /// 특정 SFX 주소 리스트를 미리 로드하여 캐싱합니다.
        /// </summary>
        /// <param name="addresses">미리 로드할 주소 배열</param>
        public async Task PreCacheSfx(params string[] addresses)
        {
            if (addresses == null || addresses.Length == 0) return;

            List<Task> loadTasks = new List<Task>();

            foreach (var address in addresses)
            {
                if (string.IsNullOrEmpty(address)) continue;

                // 이미 캐시되어 있거나 로딩 중이면 건너뜁니다.
                if (_sfxClips.ContainsKey(address) || _loadingSfxTasks.ContainsKey(address))
                {
                    continue;
                }

                // 로딩 작업을 생성하고 추적 목록에 추가합니다.
                Task<AudioClip> loadTask = LoadAndCacheSfx(address);
                _loadingSfxTasks[address] = loadTask;
                loadTasks.Add(InternalLoadWrapper(address, loadTask));
            }

            // 모든 로딩 작업이 완료될 때까지 대기합니다.
            await Task.WhenAll(loadTasks);
            Debug.Log($"[SoundManager] {addresses.Length}개의 SFX 프리캐싱 완료");
        }

        /// <summary>
        /// 로딩 완료 후 Dictionary에서 작업을 제거하기 위한 내부 래퍼
        /// </summary>
        private async Task InternalLoadWrapper(string address, Task<AudioClip> task)
        {
            try
            {
                await task;
            }
            finally
            {
                _loadingSfxTasks.Remove(address);
            }
        }
    }
}