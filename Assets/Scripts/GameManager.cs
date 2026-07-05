using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using static SoundUtils;

public class GameManager : PersistentSingleton<GameManager>
{
    [Header("Dev Only")]
    [SerializeField] private int _stageNumber;
    [SerializeField] private StageInfo[] _stageEntries;
    [SerializeField] private AudioSetting audioSetting;
    [Header("Scene Names")]
    [SerializeField] private string _mainScreen;
    [SerializeField] private string _stagePrepare;
    [SerializeField] private string _stagePrefix;
    [SerializeField] private string _store;
    [SerializeField] private string _arena = "Arena";
    [SerializeField] private string _testStart = "TestStore";
    private StageInfo _currentStage;
    [SerializeField] private DifficultyLevel _currentDifficulty;
    [Header("Scene Datas")]
    [SerializeField] private BGMPlaylistData _mainPD;
    [SerializeField] private BGMPlaylistData _wavePD;
    [SerializeField] private BGMPlaylistData _arenaPD;
    public bool hasToStageSelect = false;


    public UnityEvent ControlSettingChangeEvent = new();
    public StageInfo[] StageEntries => _stageEntries;
    public string StageTitle => $"{_currentStage.number}. {_currentStage.name}";
    public DifficultyLevel Difficulty => _currentDifficulty;
    public int CurrentStageNum => _currentStage.number;
    private SoundEmitter _bgmEmitter;
    private Coroutine _bgmChangeCoroutine;
    private string _previousSceneName;
    private string _currentSceneName;



    protected override void Awake()
    {
        base.Awake();
        if (_stageEntries == null)
        {
            //ApplyVolumes();
            _currentStage = new StageInfo();
            _currentStage.number = StageController.Instance.StageNumber;
        }
        if (_stageNumber > 0)
        {
            audioSetting.ApplyVolumes();
            _currentStage = _stageEntries[_stageNumber - 1];
            InventorySystem.Instance.ResetInventory(false);
            WaveSystem.Instance.ResetWave();
        }
        else
        {
            LoadStageProgress();
        }
        //ShowMainScreen();
    }


    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }



    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _previousSceneName = _currentSceneName;
        _currentSceneName = scene.name;
        ResetGlobalControlStats();
        if (scene.name.Equals(_mainScreen))
        {
            ChangeBGM(_mainPD);
        }
        else if (scene.name.Equals(_stagePrepare))
        {
            ChangeBGM(_wavePD);
        }
        else if (scene.name.StartsWith(_stagePrefix))
        {
            if (_bgmEmitter)
                _bgmEmitter.FadeVolumeMultiplier(0.4f, 1f);
            else
                ChangeBGM(_wavePD);
        }
        else if (scene.name.Equals(_store))
        {
            if (scene.name.StartsWith(_stagePrefix))
            {
                _bgmEmitter.FadeVolumeMultiplier(1f, 1f);
            }
            else
            {
                ChangeBGM(_wavePD);
            }
        }
        else if (scene.name.Equals(_arena))
        {
            ChangeBGM(_arenaPD);
        }
        else if (scene.name.Equals(_testStart))
        {
            ChangeBGM(_wavePD);
        }
        else
        {
            ChangeBGM(null);
        }
    }



    public void ChangeBGM(BGMPlaylistData nextPlaylist, float fadeDuration = 1f)
    {
        if (_bgmChangeCoroutine != null) StopCoroutine(_bgmChangeCoroutine);
        _bgmChangeCoroutine = StartCoroutine(Co_ChangeBGM(nextPlaylist, fadeDuration));
    }

    private IEnumerator Co_ChangeBGM(BGMPlaylistData nextPlaylist, float fadeDuration)
    {
        // 1. 기존 곡 페이드아웃 및 대기
        if (_bgmEmitter != null)
        {
            _bgmEmitter.FadeVolumeMultiplier(0f, fadeDuration);
            _bgmEmitter = null;
            yield return new WaitForSecondsRealtime(fadeDuration);
        }

        // 2. 새로운 플레이리스트 설정
        BGMPlaylistData currentPlaylist = nextPlaylist;
        if (currentPlaylist != null)
        {
            currentPlaylist.ResetShuffle();

            // 3. 무한 루프: 플레이리스트가 있는 동안 계속 다음 곡 재생
            while (currentPlaylist != null)
            {
                // 다음 곡 즉시 재생
                _bgmEmitter = PlayBGM(currentPlaylist, 1f, false);
                if (SceneManager.GetActiveScene().name.StartsWith(_stagePrefix))
                {
                    _bgmEmitter.SetVolumeRate(0.4f);
                }

                if (_bgmEmitter == null) yield break;

                // 해당 에미터의 오디오 소스 가져오기
                AudioSource source = _bgmEmitter.GetComponent<AudioSource>();

                // [핵심] 현재 재생 중인 곡이 끝날 때까지 대기
                // 소리가 멈췄거나(끝남), 에미터가 풀로 돌아가서 비활성화될 때까지 기다림
                yield return new WaitUntil(() => source == null || !source.isPlaying);

                // 곡 사이 0.5초 정도 짧은 공백 (너무 바로 나오면 어색하니까)
                yield return new WaitForSecondsRealtime(0.5f);
            }
        }

        _bgmChangeCoroutine = null;
    }



    private void PlayNextBGM()
    {

    }



    public void StartStage()
    {
        SceneManager.LoadScene($"{_stagePrefix}{_currentStage.number}");
    }



    private void LoadStageProgress()
    {
        int lastClearedStageNum = PlayerPrefs.GetInt("MaxClearStage", 0);
        int[] stageDifficulties = new int[_stageEntries.Length];
        for (int i = 0; i < stageDifficulties.Length; i++)
        {
            stageDifficulties[i] = PlayerPrefs.GetInt("StageDifficulty_" + (i + 1), 0);
        }

        for(int i = 0; i < _stageEntries.Length; ++i)
        {
            _stageEntries[i].maxClearDifficulty = (DifficultyLevel)stageDifficulties[i];
            _stageEntries[i].isLocked = _stageEntries[i].number > lastClearedStageNum + 1;
        }
    }



    private void SaveStageProgress(int stageNum, DifficultyLevel difficultyLevel)
    {
        int maxClearStage = Mathf.Max(stageNum, PlayerPrefs.GetInt("MaxClearStage", 0));
        PlayerPrefs.SetInt("MaxClearStage", maxClearStage);
        PlayerPrefs.SetInt("StageDifficulty_" + stageNum, (int)difficultyLevel);
        PlayerPrefs.Save();
    }




    public void StageCleared()
    {
        DifficultyLevel maxDifficulty = (DifficultyLevel)Mathf.Max((int)_currentDifficulty, (int)_currentStage.maxClearDifficulty);
        _currentStage.maxClearDifficulty = maxDifficulty;
        if (_currentStage.number < _stageEntries.Length)
            _stageEntries[_currentStage.number].isLocked = false;
        SaveStageProgress(_currentStage.number, _currentStage.maxClearDifficulty);
    }



    public void PrepareStage(int stageNum, DifficultyLevel difficultyLevel)
    {
        WaveSystem.Instance.ResetWave();
        InventorySystem.Instance.ResetInventory();
        _currentStage = _stageEntries[stageNum - 1];
        _currentDifficulty = difficultyLevel;
        SceneManager.LoadScene(_stagePrepare);
    }



    public void Restart()
    {
        PrepareStage(_currentStage.number, _currentDifficulty);
    }



    public void GoStore()
    {
        SceneManager.LoadScene(_store);
    }



    public void ShowMainScreen()
    {
        SceneManager.LoadScene(_mainScreen);
        _currentStage = null;
        _currentDifficulty = DifficultyLevel.None;
    }



    public void ShowStageSelect()
    {
        hasToStageSelect = true;
        ShowMainScreen();
    }



    private void ResetGlobalControlStats()
    {
        Time.timeScale = 1;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        //Cursor.lockState = CursorLockMode.Locked;
    }



    public void GoArena()
    {
        SceneManager.LoadScene(_arena);
    }



    public void ControlSettingChanged()
    {
        ControlSettingChangeEvent?.Invoke();
    }



    public void ExitGame()
    {
        Application.Quit();
    }
}



[System.Serializable]
public class StageInfo
{
    public int number;
    public string name;
    public DifficultyLevel maxClearDifficulty;
    public bool isLocked;
    public Sprite sprite;
}