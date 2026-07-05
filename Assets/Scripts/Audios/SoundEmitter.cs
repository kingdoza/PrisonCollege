using UnityEngine;
using System.Collections;
using UnityEngine.Events;
using UnityEngine.Audio;

public class SoundEmitter : MonoBehaviour
{
    [SerializeField] private AudioMixerGroup _sfxGroup;
    [SerializeField] private AudioMixerGroup _bgmGroup;
    private AudioSource _audioSource;
    private SoundManager _pool;
    private static bool _isAppQuitting = false;
    public UnityEvent ReturnEvent = new();
    private Coroutine _fadeCoroutine;
    private float _originalVolume;
    private bool _realTime;

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        _audioSource.playOnAwake = false;
    }

    private void OnApplicationQuit()
    {
        _isAppQuitting = true;
    }

    public void Initialize(SoundManager pool)
    {
        _pool = pool;
        SoundManager.Instance.OnPauseChanged -= HandlePauseChanged;
        SoundManager.Instance.OnPauseChanged += HandlePauseChanged;
        if (SoundManager.Instance.IsPaused)
        {
            _audioSource.Pause();
        }
    }



    private void HandlePauseChanged(bool isPaused)
    {
        if (_realTime || _audioSource == null) return;
        if (isPaused) _audioSource.Pause();
        else _audioSource.UnPause();
    }


    public void Play(AudioClip clip, float pitch, float volume, Vector3 position, bool is3D, bool persistBetweenScenes, bool isLoop, bool realTime = false, bool isLongDist = false, bool isBGM = false)
    {
        _realTime = realTime;
        transform.position = position;
        _audioSource.clip = clip;
        _audioSource.pitch = pitch;
        _audioSource.loop = isLoop;

        // 여기서 개별 볼륨을 설정합니다 (0.0 ~ 1.0)
        _audioSource.volume = volume;
        _originalVolume = volume;

        _audioSource.spatialBlend = is3D ? 1.0f : 0.0f;

        if (isLongDist)
        {
            _audioSource.minDistance = 2;
            _audioSource.maxDistance = 40;
        }
        else
        {
            _audioSource.minDistance = 1;
            _audioSource.maxDistance = 20;
        }

        if (isBGM)
        {
            _audioSource.outputAudioMixerGroup = _bgmGroup;
        }
        else
        {
            _audioSource.outputAudioMixerGroup = _sfxGroup;
        }

        if (persistBetweenScenes)
        {
            transform.SetParent(_pool.transform);
        }
        else
        {
            transform.SetParent(null);
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(gameObject, UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        }

        _audioSource.Play();
        if (SoundManager.Instance.IsPaused && !_realTime)
        {
            _audioSource.Pause();
        }
        if (!isLoop)
        {
            StartCoroutine(ReturnAfterFinish(clip.length));
        }
    }

    private IEnumerator ReturnAfterFinish(float duration)
    {
        // 타임스케일 영향 없이 실제 시간 기준으로 대기
        yield return new WaitForSecondsRealtime(duration);

        _audioSource.Stop();
        transform.SetParent(_pool.transform); // 풀로 돌아갈 땐 다시 매니저 자식으로
        _pool.ReturnToPool(this);
    }



    public void StopAndReturn()
    {
        if (_isAppQuitting || this == null || gameObject == null) return;
        //if (SoundManager.Instance != null)
        //    SoundManager.Instance.OnPauseChanged -= HandlePauseChanged;
        if (gameObject.activeInHierarchy)
        {
            StopAllCoroutines();
        }
        //StopAllCoroutines(); // 진행 중인 ReturnAfterFinish 코루틴 중단
        _audioSource.Stop();
        _audioSource.loop = false;
        _audioSource.clip = null;
        transform.SetParent(_pool.transform);
        _pool.ReturnToPool(this);
        ReturnEvent?.Invoke();
        ReturnEvent.RemoveAllListeners();
    }



    public void FadeVolumeMultiplier(float volumeMultiplier, float duration)
    {
        if (!gameObject.activeInHierarchy)
        {
            // 그냥 즉시 목표 볼륨 적용하고 끝냄
            float targetVolume = _audioSource.volume * volumeMultiplier;
            _audioSource.volume = targetVolume;

            if (targetVolume <= 0.001f) StopAndReturn();
            return;
        }

        if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
        _fadeCoroutine = StartCoroutine(Co_FadeVolumeMultiplier(volumeMultiplier, duration));
    }



    public void SetVolumeRate(float rate)
    {
        _audioSource.volume = _originalVolume * rate;
    }



    private IEnumerator Co_FadeVolumeMultiplier(float volumeMultiplier, float duration)
    {
        float startVolume = _audioSource.volume;
        float targetVolume = _originalVolume * volumeMultiplier;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            _audioSource.volume = Mathf.Lerp(startVolume, targetVolume, timer / duration);
            yield return null;
        }

        _audioSource.volume = targetVolume;
        _fadeCoroutine = null;

        if (targetVolume <= 0.001f)
        {
            StopAndReturn();
        }
    }
}