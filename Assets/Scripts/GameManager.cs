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
    [SerializeField] private string _tutorialScene = "TutorialStage";
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
    private const string TUTORIAL_COMPLETED_KEY = "TutorialCompleted";
    private const string MAX_CLEAR_STAGE_KEY = "MaxClearStage";
    private const string STAGE_DIFFICULTY_KEY_PREFIX = "StageDifficulty_";

    public bool IsTutorialCompleted => PlayerPrefs.GetInt(TUTORIAL_COMPLETED_KEY, 0) == 1;



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
        else if (scene.name.Equals(_tutorialScene))
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
        // 1. ±âÁ¸ °î ÆäÀÌµå¾Æ¿ô ¹× ´ë±â
        if (_bgmEmitter != null)
        {
            _bgmEmitter.FadeVolumeMultiplier(0f, fadeDuration);
            _bgmEmitter = null;
            yield return new WaitForSecondsRealtime(fadeDuration);
        }

        // 2. »õ·Î¿î ÇÃ·¹ÀÌ¸®½ºÆ® ¼³Á¤
        BGMPlaylistData currentPlaylist = nextPlaylist;
        if (currentPlaylist != null)
        {
            currentPlaylist.ResetShuffle();

            // 3. ¹«ÇÑ ·çÇÁ: ÇÃ·¹ÀÌ¸®½ºÆ®°¡ ÀÖ´Â µ¿¾È °è¼Ó ´ÙÀ½ °î Àç»ý
            while (currentPlaylist != null)
            {
                // ´ÙÀ½ °î Áï½Ã Àç»ý
                _bgmEmitter = PlayBGM(currentPlaylist, 1f, false);
                if (SceneManager.GetActiveScene().name.StartsWith(_stagePrefix)
                    || SceneManager.GetActiveScene().name.Equals(_tutorialScene))
                {
                    _bgmEmitter.SetVolumeRate(0.4f);
                }

                if (_bgmEmitter == null) yield break;

                // ÇØ´ç ¿¡¹ÌÅÍÀÇ ¿Àµð¿À ¼Ò½º °¡Á®¿À±â
                AudioSource source = _bgmEmitter.GetComponent<AudioSource>();

                // [ÇÙ½É] ÇöÀç Àç»ý ÁßÀÎ °îÀÌ ³¡³¯ ¶§±îÁö ´ë±â
                // ¼Ò¸®°¡ ¸ØÃè°Å³ª(³¡³²), ¿¡¹ÌÅÍ°¡ Ç®·Î µ¹¾Æ°¡¼­ ºñÈ°¼ºÈ­µÉ ¶§±îÁö ±â´Ù¸²
                yield return new WaitUntil(() => source == null || !source.isPlaying);

                // °î »çÀÌ 0.5ÃÊ Á¤µµ ÂªÀº °ø¹é (³Ê¹« ¹Ù·Î ³ª¿À¸é ¾î»öÇÏ´Ï±î)
                yield return new WaitForSecondsRealtime(0.5f);
            }
        }

        _bgmChangeCoroutine = null;
    }



    public void PlayNextBGM()
    {
        if (_bgmEmitter == null) return;

        SoundEmitter currentEmitter = _bgmEmitter;
        _bgmEmitter = null;
        currentEmitter.StopAndReturn();
    }



    public void StartStage()
    {
        SceneManager.LoadScene($"{_stagePrefix}{_currentStage.number}");
    }



    public void StartTutorial()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(_tutorialScene);
    }



    public void MarkTutorialCompleted()
    {
        PlayerPrefs.SetInt(TUTORIAL_COMPLETED_KEY, 1);
        PlayerPrefs.Save();
    }



    public void ExitTutorialToMain()
    {
        MarkTutorialCompleted();
        Time.timeScale = 1f;
        SceneManager.LoadScene(_mainScreen);
    }



    public void AbortTutorialToMain()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(_mainScreen);
    }



    private void LoadStageProgress()
    {
        int lastClearedStageNum = PlayerPrefs.GetInt(MAX_CLEAR_STAGE_KEY, 0);

        for(int i = 0; i < _stageEntries.Length; ++i)
        {
            int stageNumber = _stageEntries[i].number;
            int difficulty = PlayerPrefs.GetInt(GetStageDifficultyKey(stageNumber), 0);
            _stageEntries[i].maxClearDifficulty = (DifficultyLevel)difficulty;
            _stageEntries[i].isLocked = _stageEntries[i].number > lastClearedStageNum + 1;
        }
    }



    private void SaveStageProgress(int stageNum, DifficultyLevel difficultyLevel)
    {
        int maxClearStage = Mathf.Max(stageNum, PlayerPrefs.GetInt(MAX_CLEAR_STAGE_KEY, 0));
        PlayerPrefs.SetInt(MAX_CLEAR_STAGE_KEY, maxClearStage);
        PlayerPrefs.SetInt(GetStageDifficultyKey(stageNum), (int)difficultyLevel);
        PlayerPrefs.Save();
    }



    public void ResetStageProgress()
    {
        PlayerPrefs.DeleteKey(MAX_CLEAR_STAGE_KEY);
        for (int i = 0; i < _stageEntries.Length; ++i)
        {
            PlayerPrefs.DeleteKey(GetStageDifficultyKey(_stageEntries[i].number));
        }

        PlayerPrefs.Save();
        LoadStageProgress();
    }



    public void SetAllStagesCleared(DifficultyLevel difficultyLevel)
    {
        if (difficultyLevel != DifficultyLevel.Normal && difficultyLevel != DifficultyLevel.Hard)
        {
            Debug.LogError("ì „ì²´ ìŠ¤í…Œì´ì§€ í´ë¦¬ì–´ ì„¤ì •ì—ëŠ” Normal ë˜ëŠ” Hard ë‚œì´ë„ë§Œ ì‚¬ìš©í•  ìˆ˜ ìžˆìŠµë‹ˆë‹¤.", this);
            return;
        }

        int maxStageNumber = 0;
        for (int i = 0; i < _stageEntries.Length; ++i)
        {
            int stageNumber = _stageEntries[i].number;
            maxStageNumber = Mathf.Max(maxStageNumber, stageNumber);
            PlayerPrefs.SetInt(GetStageDifficultyKey(stageNumber), (int)difficultyLevel);
        }

        PlayerPrefs.SetInt(MAX_CLEAR_STAGE_KEY, maxStageNumber);
        PlayerPrefs.Save();
        LoadStageProgress();
    }



    private static string GetStageDifficultyKey(int stageNumber)
    {
        return STAGE_DIFFICULTY_KEY_PREFIX + stageNumber;
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
        if (SceneManager.GetActiveScene().name == _tutorialScene)
        {
            StartTutorial();
            return;
        }
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
