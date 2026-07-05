using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class SoundManager : PersistentSingleton<SoundManager>
{
    [SerializeField] private GameObject _emitterPrefab;
    [SerializeField] private GameObject _longDistancemitterPrefab;
    [SerializeField] private GameObject _bgmEitterPrefab;
    [SerializeField] private int _poolSize = 20;
    private Queue<SoundEmitter> _pool = new Queue<SoundEmitter>();
    private bool _isPaused;
    public bool IsPaused => _isPaused;
    public System.Action<bool> OnPauseChanged;


    protected override void Awake()
    {   
        base.Awake();
        for (int i = 0; i < _poolSize; i++) CreateNewEmitter();
    }



    private void Start()
    {
        StartCoroutine(CheckTimeScaleRoutine());
    }



    IEnumerator CheckTimeScaleRoutine()
    {
        while (true)
        {
            float currentTimeScale = Time.timeScale;
            bool shouldPause = currentTimeScale <= 0.1f;

            if (shouldPause != IsPaused)
            {
                _isPaused = shouldPause;
                SetPause(_isPaused);
            }
            yield return new WaitForSecondsRealtime(0f);
        }
    }



    public void SetPause(bool pause)
    {
        _isPaused = pause;
        OnPauseChanged?.Invoke(pause); // 모든 이미터에게 "상태 변했다!"고 한 번만 알림
    }



    private void CreateNewEmitter(bool isLongDistance = false, bool isBGM = false)
    {
        GameObject obj = Instantiate(_emitterPrefab, transform);
        SoundEmitter emitter = obj.GetComponent<SoundEmitter>();
        emitter.Initialize(this);
        obj.SetActive(false);
        _pool.Enqueue(emitter);
    }



    public SoundEmitter PlaySFX(AudioClip clip, Vector3 position, float volume = 1.0f, bool is3D = true, bool persist = false, bool isRandomPitch = true, bool isLoop = false, bool isLongDistance = false)
    {
        if (clip == null) return null;
        SoundEmitter emitter = null;

        // 풀에서 쓸 만한 녀석을 찾을 때까지 반복
        while (emitter == null)
        {
            if (_pool.Count > 0)
            {
                emitter = _pool.Dequeue();

                // 만약 꺼낸 녀석이 이미 파괴되었다면(MissingReference), 다시 null로 만들고 다음 시도
                if (emitter == null || emitter.gameObject == null)
                {
                    emitter = null;
                    continue;
                }
            }
            else
            {
                // 풀이 진짜로 비어있다면 새로 생성
                CreateNewEmitter(isLongDistance);

                // 생성 직후 큐에 들어갔을 테니 다시 Dequeue
                if (_pool.Count > 0)
                    emitter = _pool.Dequeue();
                else
                    return null; // 생성 실패 시 안전장치
            }
        }
        //if (_pool.Count == 0) CreateNewEmitter();

        //SoundEmitter emitter = _pool.Dequeue();
        emitter.gameObject.SetActive(true);

        float pitch = isRandomPitch ? Random.Range(0.9f, 1.1f) : 1f;
        emitter.Play(clip, pitch, volume, position, is3D, persist, isLoop, false, isLongDistance);
        return emitter; 
    }



    public SoundEmitter PlayBGM(AudioClip clip, float volume = 1.0f, bool persist = false, bool isLoop = false, bool isRealTime = false)
    {
        if (clip == null) return null;
        //if (_pool.Count == 0) CreateNewEmitter();
        SoundEmitter emitter = null;

        // 1. 유효한 에미터가 나올 때까지 반복
        while (emitter == null)
        {
            if (_pool.Count == 0)
            {
                CreateNewEmitter(); // 형님 말대로 여기서 큐를 채움
            }

            emitter = _pool.Dequeue();

            // 꺼낸 놈이 Destroy된 상태라면 null로 만들어서 다시 루프 돌게 함
            if (emitter == null || emitter.gameObject == null)
            {
                emitter = null;
                continue;
            }
        }

        //SoundEmitter emitter = _pool.Dequeue();
        emitter.gameObject.SetActive(true);

        emitter.Play(clip, 1, volume, Vector3.zero, false, persist, isLoop, isRealTime, false, true);
        return emitter;
    }



    public void ReturnToPool(SoundEmitter emitter)
    {
        // 씬 전환 도중에는 풀이 비어있을 수 있어 안전장치 추가
        if (this == null) return;

        emitter.transform.SetParent(transform);
        emitter.gameObject.SetActive(false);
        _pool.Enqueue(emitter);
    }



    // [핵심] 오브젝트가 직접 이미터를 빌려갈 때 쓰는 함수
    public SoundEmitter GetEmitter()
    {
        if (_pool.Count == 0)
        {
            CreateNewEmitter();
        }

        SoundEmitter emitter = _pool.Dequeue();
        emitter.gameObject.SetActive(true);
        return emitter;
    }
}



public static class SoundUtils
{
    public static void PlayScene3DSFX(AudioClip clip, Vector3 position, float volumeMultiplier = 1f, bool isLoop = false, bool isLongDistance = false)
    {
        SoundManager.Instance.PlaySFX(clip, position, volumeMultiplier, true, false, true, isLoop, isLongDistance);
    }



    public static void PlayScene3DSFX(SoundData soundData, Vector3 position, float volumeMultiplier = 1f, bool isLoop = false, bool isLongDistance = false)
    {
        SoundManager.Instance.PlaySFX(soundData.GetRandomClip(out float volume), position, volume * volumeMultiplier, true, false, true, isLoop, isLongDistance);
    }



    public static SoundEmitter PlayOwnedScene3DSFX(SoundData soundData, Vector3 position, bool isRandomPitch, float volumeMultiplier = 1f, bool isLoop = false, bool isLongDistance = false)
    {
        return SoundManager.Instance.PlaySFX(soundData.GetRandomClip(out float volume), position, volume * volumeMultiplier, true, false, isRandomPitch, isLoop, isLongDistance);
    }



    public static void PlayScene2DSFX(AudioClip clip, float volumeMultiplier = 1f, bool isLoop = false)
    {
        SoundManager.Instance.PlaySFX(clip, Vector3.zero, volumeMultiplier, false, false, true, isLoop);
    }



    public static SoundEmitter PlayOwnedScene2DSFX(SoundData soundData, bool isRandomPitch, float volumeMultiplier = 1f, bool isLoop = false)
    {
        return SoundManager.Instance.PlaySFX(soundData.GetRandomClip(out float volume), Vector3.zero, volume * volumeMultiplier, false, false, isRandomPitch, isLoop);
    }



    public static void PlayScene2DSFX(SoundData soundData, float volumeMultiplier = 1f, bool isLoop = false)
    {
        SoundManager.Instance.PlaySFX(soundData.GetRandomClip(out float volume), Vector3.zero, volume * volumeMultiplier, false, false, true, isLoop);
    }



    public static void PlayUISFX(AudioClip clip, float volumeMultiplier = 1f, bool isLoop = false)
    {
        SoundManager.Instance.PlaySFX(clip, Vector3.zero, volumeMultiplier, false, false, false, isLoop);
    }



    public static void PlayUISFX(SoundData soundData, float volumeMultiplier = 1f, bool isLoop = false)
    {
        SoundManager.Instance.PlaySFX(soundData.GetRandomClip(out float volume), Vector3.zero, volume * volumeMultiplier, false, false, false, isLoop);
    }



    public static SoundEmitter PlayBGM(BGMPlaylistData bGMPlaylistData, float volumeMultiplier = 1f, bool isShuffle = false)
    {
        if (isShuffle)
            bGMPlaylistData.ResetShuffle();
        return SoundManager.Instance.PlayBGM(bGMPlaylistData.GetNextShuffleClip(out float volume, out string title), volume * volumeMultiplier, true, false, true);
    }
}