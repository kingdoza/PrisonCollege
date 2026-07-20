using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StageController : SceneSingleton<StageController>
{
    [Header("Dev Only")]
    [SerializeField] private int _stageNumber = 0;
    [Header("Runtime Config")]
    [Tooltip("비어 있거나 Mode가 Normal이면 기존 정규 스테이지 경로를 그대로 사용합니다.")]
    [SerializeField] private StageRuntimeConfig _runtimeConfig;
    [Header("UI Bindings")]
    [SerializeField] private TextMeshProUGUI _waveTmp;
    [SerializeField] private TextMeshProUGUI _timerTmp;
    [SerializeField] private TextMeshProUGUI _chaosTmp;
    [SerializeField] private TextMeshProUGUI _escapeTmp;
    [SerializeField] private TextMeshProUGUI _moneyTmp;
    [SerializeField] private TextMeshProUGUI _workingTmp;
    [SerializeField] private Image _projectProgressBar;
    [SerializeField] private List<ItemSlot> _equipSlotList;
    [SerializeField] private MenuPanel _menuPanel;
    [SerializeField] private TextMeshProUGUI _prepareTimerTmp;
    [SerializeField] private CanvasGroup _preparePanelGroup;
    [SerializeField] private CanvasGroup _topPanelGroup;
    [Header("Stats")]
    [SerializeField] private Stat _timerStat;
    [SerializeField] private Stat _prepareTimeStat;
    [SerializeField] private Stat _chaosStat;
    [SerializeField] private Stat _escapeStat;
    [SerializeField] private Stat _projectStat;
    [Header("Chaos Settings")]
    [Tooltip("ㅄ짓 학생없는 동안 초당 감소량")]
    [SerializeField] private float _defaultReduction = 5;
    [Tooltip("ㅄ짓 학생당 초당 증가량")]
    [SerializeField] private float _increasePerStud = 3;
    [Tooltip("무고한 학생 때려눕혔을때 증가량")]
    [SerializeField] private float _innocentKillPenalty = 10;
    [Tooltip("학생 탈출했을때 증가량")]
    [SerializeField] private float _studEscapedPenalty = 30;
    [Tooltip("총기 발사 증가량")]
    [SerializeField] private float _gunShotPenalty = 10;
    [Tooltip("정상 음식 뺐을때 증가량")]
    [SerializeField] private float _normalFoodRemovedPenalty = 10;
    [Header("Task Settings")]
    [Tooltip("학생당 초당 진행량")]
    [SerializeField] private float _studTaskProgress = 5;
    [Tooltip("교수의 초당 진행량")]
    [SerializeField] private float _profTaskProgress = 20;
    [Tooltip("프로젝트 완수마다 보상량")]
    [SerializeField] private int _progectReward = 50;
    [Header("Behavior Func Settings")]
    [SerializeField] private float _minDelayFactor = 0.25f;
    [SerializeField] private float _delayFuncFactor = 0.5f;
    [Header("Professor Task Place")]
    [SerializeField] private ProfessorTask[] _professorTasks;
    [Header("ETC")]
    [SerializeField] private Professor _player;
    [SerializeField] private StageSpots _stageSpots;
    [SerializeField] private RandomStudentSpawner _studentSpawner;
    [SerializeField] private StageOver _stageOver;
    [SerializeField] private ChaosUI _chaosUi;
    [SerializeField] private bool _isTestMode = true;
    [SerializeField] private Transform _reflectionGroup;
    [Header("Directional Lights")]
    [SerializeField] private GameObject _sunLightObject;
    [SerializeField] private GameObject _moonLightObject;
    [Header("Sound Datas")]
    [SerializeField] private SoundData _moneyGainSD;

    private EquipInfo[] _equipInfos;
    private int _money = 0;
    private int _workingStudCount = 0;
    private bool _isProfWorking = false;
    private List<PostStudent> _studentList = new();
    private TutorialStagePolicy _tutorialPolicy;
    private float _currentChaosRate;
    private int _tutorialEscapeFailureThreshold = 3;
    private int _tutorialEscapeCount;
    private float _tutorialTimerRemaining;
    private bool _tutorialTimerFinishedReported;
    private int _projectCompletionId;
    private bool _tutorialSimulationStopped;

    public float ProjectProgress => _projectStat.Ratio;
    public float Chaos => _chaosStat.Current;
    public float ChaosRate => _currentChaosRate;
    public float TimerRemaining => IsTutorialRuntime ? _tutorialTimerRemaining : _timerStat.Current;
    public int EscapeCount => IsTutorialRuntime ? _tutorialEscapeCount : Mathf.RoundToInt(_escapeStat.Current);
    public int EscapeFailureThreshold => IsTutorialRuntime
        ? _tutorialEscapeFailureThreshold
        : Mathf.RoundToInt(_escapeStat.Max);
    public int WorkingStudentCount => _workingStudCount;
    public bool IsProfessorWorking => _isProfWorking;
    public int SessionMoney => _money;
    public Professor Player => _player;
    public StageSpots StageSpots => _stageSpots;
    public StageRuntimeConfig RuntimeConfig => _runtimeConfig;
    public bool IsTutorialRuntime => _runtimeConfig != null && _runtimeConfig.IsTutorial;
    public IReadOnlyList<PostStudent> Students => _studentList;

    public event Action<ChaosChangedData> ChaosChanged;
    public event Action<int, int> EscapeCountChanged;
    public event Action<float, float> ProjectProgressChanged;
    public event Action<int, ProjectContributor> ProjectCompleted;
    public event Action<int> WorkingStudentCountChanged;
    public event Action<PostStudent> StudentRegistered;
    public event Action<PostStudent> StudentUnregistered;
    public event Action<PostStudent, HitInfo, bool> StudentDowned;
    public event Action<PostStudent> StudentEscaped;
    public event Action<StageFinishResult> StageFinished;

    private AttributeModifier _studTaskModifier;
    private AttributeModifier _chaosDecreaseModifier;
    public int StageNumber => _stageNumber;
    private int _originMoney;
    private bool _isPreparing = true;
    public bool IsPreparing => _isPreparing;
    public UnityEvent StageStartEvent = new();

    private ReflectionProbe[] _reflectionProbes;
    private const float CHAOS_RECREASE_DELAY = 3;
    private float _remainedChaosDecreaseTime = 0;



    protected override void Awake()
    {
        base.Awake();
        _timerStat.Initialize();
        _prepareTimeStat.Initialize();
        _chaosStat.Initialize(true);
        _escapeStat.Initialize(true);
        _projectStat.Initialize(true);

        _reflectionProbes = _reflectionGroup != null
            ? _reflectionGroup.GetComponentsInChildren<ReflectionProbe>()
            : Array.Empty<ReflectionProbe>();

        _timerStat.DepletedEvent.AddListener(OnTimerDepleted);
        _prepareTimeStat.DepletedEvent.AddListener(InitStage);
        _escapeStat.MaxReachEvent.AddListener(OnEscapeLimitReached);
        _projectStat.MaxReachEvent.AddListener(OnProjectSuccessed);

        if (!IsTutorialRuntime)
        {
            _originMoney =_money = InventorySystem.Instance.Money;
            InventorySystem.Instance.ActivatePassiveItems();
        }
        else
        {
            _originMoney = _money = 0;
        }

        //SetStudentList();

        //foreach (var student in _studentList)
        //{
        //    student.DieEvent.AddListener(OnStudentDied);
        //    student.EscapeEvent.AddListener(OnStudentEscaped);
        //}
        _studTaskModifier = AttributeSystem.Instance.TaskEfficiencyMod;
        _chaosDecreaseModifier = AttributeSystem.Instance.ChaosDecreaseMod;

        _equipInfos = new EquipInfo[_equipSlotList.Count];
        for (int i = 0; i< _equipSlotList.Count; i++)
        {
            _equipInfos[i] = _equipSlotList[i].GetComponent<EquipInfo>();
        }
    }



    private void Start()
    {
        int studentLayer = LayerMask.NameToLayer(Global.STUDENT_LAYER_NAME);
        Physics.IgnoreLayerCollision(studentLayer, studentLayer, true);
        if (IsTutorialRuntime)
        {
            StartTutorialRuntime();
            return;
        }
        StartPrepare();
        RenderReflectionProbes();
        WaveSystem.Instance.ApplySkybox();
        bool isDay = WaveSystem.Instance.CurrentDayState == WaveSystem.DayState.Day;
        _sunLightObject.SetActive(isDay);
        _moonLightObject.SetActive(!isDay);
        if (GameManager.Instance.Difficulty == DifficultyLevel.Hard)
        {
            _chaosStat.Increase(100);
        }
        //if (WaveSystem.Instance.CurrentWave <= 0)
        //    WaveSystem.Instance.NewWaveEntered();
        _menuPanel.Init();
        _waveTmp.text = $"웨이브 {WaveSystem.Instance.CurrentWave}";
        InventorySystem.Instance.FillEquipSlots(_equipSlotList);

        _studentList = _studentSpawner.SpawnStudents(WaveSystem.Instance.BehaviorWeightSet);

        foreach (var student in _studentList)
        {
            HookStudent(student, false);
        }
    }



    private void Update()
    {
        if (IsTutorialRuntime)
        {
            UpdateTutorialRuntime();
            return;
        }

        if (_isPreparing && Input.GetKeyDown(KeyCode.Tab))
        {
            InitStage();
        }
        if (!_isPreparing)
        {
            CountWorkingStudents();
            CheckProfessorProgressing();
            ProgressProject();
            DecreaseTime();
        }
        else
        {
            _prepareTimeStat.Decrease(Time.deltaTime);
            _prepareTimerTmp.text = _prepareTimeStat.Current.ToString("F0");
        }
        float chaosChanged = IncreaseChaos();
        if (chaosChanged > 0)
        {
            _remainedChaosDecreaseTime = CHAOS_RECREASE_DELAY;
        }
        if (_remainedChaosDecreaseTime > 0)
        {
            _remainedChaosDecreaseTime -= Time.deltaTime;
        }
        UpdateUIs(chaosChanged);
    }



    private void StartTutorialRuntime()
    {
        _isPreparing = false;
        if (_preparePanelGroup != null) _preparePanelGroup.alpha = 0f;
        if (_topPanelGroup != null) _topPanelGroup.alpha = 1f;
        if (_waveTmp != null) _waveTmp.gameObject.SetActive(false);
        _menuPanel?.InitTutorial(_runtimeConfig.TutorialStageTitle);
        RenderReflectionProbes();
        UpdateUIs(0f);
    }



    private void UpdateTutorialRuntime()
    {
        if (_tutorialSimulationStopped)
        {
            _currentChaosRate = 0f;
            UpdateUIs(0f);
            return;
        }

        CountWorkingStudents();
        CheckProfessorProgressing();
        if (_tutorialPolicy.runProject)
            ProgressProject();
        if (_tutorialPolicy.runTimer)
            DecreaseTutorialTime();

        float chaosChanged = UpdateTutorialChaos();
        _currentChaosRate = chaosChanged;
        UpdateUIs(chaosChanged);
    }



    private void RenderReflectionProbes()
    {
        foreach (var reflection in _reflectionProbes)
        {
            reflection.RenderProbe();
        }
    }



    private void StartPrepare()
    {
        _isPreparing = true;
        _preparePanelGroup.alpha = 1;
        _topPanelGroup.alpha = 0.2f;
    }



    private void InitStage()
    {
        _isPreparing = false;
        _preparePanelGroup.alpha = 0;
        _topPanelGroup.alpha = 1f;
        StageStartEvent?.Invoke();
        RenderReflectionProbes();
    }



    public void Restart()
    {
        Time.timeScale = 1;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }



    public void GoStore()
    {
        if (IsTutorialRuntime)
        {
            Debug.LogError("튜토리얼 runtime에서는 정규 상점 흐름을 호출할 수 없습니다.", this);
            return;
        }
        Time.timeScale = 1;
        InventorySystem.Instance.SetMoney(_money); 
        SceneManager.LoadScene("Store");
    }



    private void SetStudentList()
    {
        GameObject[] studentTagObjects = GameObject.FindGameObjectsWithTag("Student");

        foreach (GameObject obj in studentTagObjects)
        {
            PostStudent student = obj.GetComponent<PostStudent>();
            if (student == null) continue;
            _studentList.Add(student);
        }
    }



    private void CountWorkingStudents()
    {
        int previousCount = _workingStudCount;
        _workingStudCount = 0;
        foreach (var student in _studentList)
        {
            if (student != null
                && (!IsTutorialRuntime || student.CountsForStageAggregation)
                && student.IsWorking)
            {
                _workingStudCount++;
            }
        }

        if (previousCount != _workingStudCount)
            WorkingStudentCountChanged?.Invoke(_workingStudCount);
    }



    private void CheckProfessorProgressing()
    {
        _isProfWorking = false;
        foreach (var profTask in _professorTasks)
        {
            if (profTask.IsTasking)
            {
                _isProfWorking = true;
                break;
            }
        }
    }



    private void ProgressProject()
    {
        float studTotalProgress = _workingStudCount * _studTaskProgress * Time.deltaTime * _studTaskModifier.GetFinalValue(1);
        float profTotalProgress = _isProfWorking ? _profTaskProgress * Time.deltaTime : 0;
        float projectFactor = IsTutorialRuntime
            ? _runtimeConfig.TutorialProjectFactor
            : WaveSystem.Instance.ProjectFactor;
        float finalProgress = (studTotalProgress + profTotalProgress) * projectFactor;
        float previousProgress = _projectStat.Ratio;
        _projectStat.Increase(finalProgress);
        if (!Mathf.Approximately(previousProgress, _projectStat.Ratio))
            ProjectProgressChanged?.Invoke(previousProgress, _projectStat.Ratio);
    }



    public void Earn(int money)
    {
        _money += money;
        _chaosUi.SpawnWarningPanel(new MutinyMoneyInfo(money));
    }



    private void ProgressProject_T()
    {
        _isProfWorking = Input.GetKey(KeyCode.Escape);
    }



    private void GameOver(bool isSuccess)
    {
        if (IsTutorialRuntime)
        {
            StageFinished?.Invoke(isSuccess ? StageFinishResult.TimerExpired : StageFinishResult.EscapeFailure);
            return;
        }

        Time.timeScale = 0;
        Player.DisableController();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        //Cursor.lockState = CursorLockMode.Locked;

        if (!isSuccess)
        {
            _stageOver.ShowStageOverPanel(false);
        }
        else if (WaveSystem.Instance.IsLastWave)
        {
            GameManager.Instance.StageCleared();
            _stageOver.ShowStageOverPanel(true);
        }
        else
        {
            InventorySystem.Instance.SetMoney(_money);
            _stageOver.ShowWaveOverPanel(_money - _originMoney);
        }
    }


    private void OnStudentEscaped(PostStudent student)
    {
        if (IsTutorialRuntime && (student == null || !student.CountsForStageAggregation)) return;

        float chaosFactor = IsTutorialRuntime
            ? _runtimeConfig.TutorialChaosFactor
            : WaveSystem.Instance.ChaosFactor;
        float chaosIncrease = _studEscapedPenalty * chaosFactor;
        if (!IsTutorialRuntime || _tutorialPolicy.allowEscapeChaos)
        {
            ApplyChaosIncrease(chaosIncrease, ChaosChangeReason.Escape);
            PopupChaosWarning(new EscapedChaos(chaosIncrease));
        }
        if (IsTutorialRuntime)
            _tutorialEscapeCount++;
        else
            _escapeStat.Increase(1);
        EscapeCountChanged?.Invoke(EscapeCount, EscapeFailureThreshold);
        StudentEscaped?.Invoke(student);

        if (IsTutorialRuntime
            && _tutorialPolicy.evaluateEscapeFailure
            && EscapeCount >= EscapeFailureThreshold)
        {
            GameOver(false);
        }
    }



    private void OnStudentDied(PostStudent student, HitInfo hitInfo)
    {
        if (IsTutorialRuntime && (student == null || !student.CountsForStageAggregation)) return;

        if (hitInfo.attacker == Player.gameObject)
        {
            HitMarkerUI.Instance?.PlayKill();
            KillFeedbackController.Instance.PlayKillFeedback();
        }
        bool wasHazardous = student.IsDoingHazardBehavior;
        StudentDowned?.Invoke(student, hitInfo, wasHazardous);
        if (wasHazardous == false
            && hitInfo.attacker == Player.gameObject
            && (!IsTutorialRuntime || _tutorialPolicy.allowInnocentDownChaos))
        {
            float chaosFactor = IsTutorialRuntime
                ? _runtimeConfig.TutorialChaosFactor
                : WaveSystem.Instance.ChaosFactor;
            float chaosIncrease = _innocentKillPenalty * chaosFactor;
            ApplyChaosIncrease(chaosIncrease, ChaosChangeReason.InnocentDown);
            PopupChaosWarning(new InnocentKillChaos(chaosIncrease));
        }
    }



    private void OnProjectSuccessed()
    {
        ProjectContributor contributors = ProjectContributor.None;
        if (_workingStudCount > 0) contributors |= ProjectContributor.Student;
        if (_isProfWorking) contributors |= ProjectContributor.Professor;
        _projectCompletionId++;
        _projectStat.Initialize(true);
        ProjectProgressChanged?.Invoke(1f, _projectStat.Ratio);
        ProjectCompleted?.Invoke(_projectCompletionId, contributors);
        _money += _progectReward;
        _chaosUi.SpawnWarningPanel(new ProjectMoneyInfo(_progectReward));
        //SoundUtils.PlayUISFX(_moneyGainSD);
    }



    public void HackBlocked()
    {
        _chaosUi.SpawnWarningPanel(new HackBlockInfo());
    }



    public void Hacked()
    {
        _chaosUi.SpawnWarningPanel(new HackInfo());
    }



    private void UpdateUIs(float chaosChanged)
    {
        int minutes = Mathf.FloorToInt(TimerRemaining / 60f);
        int seconds = Mathf.FloorToInt(TimerRemaining % 60f);
        //_timerTmp.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        _timerTmp.text = TimerRemaining.ToString("F0");
        if (TimerRemaining < 11)
        {
            _timerTmp.text = $"<color=red>{_timerTmp.text}</color>";
        }

        _chaosTmp.text = _chaosStat.Current.ToString("F0");

        _escapeTmp.text = $"{EscapeCount} / {EscapeFailureThreshold}";

        _moneyTmp.text = _money.ToString("N0");

        _workingTmp.text = $"{_workingStudCount.ToString()}명 작업중";

        _projectProgressBar.fillAmount = _projectStat.Ratio;

        string addChoasText;
        if (Mathf.Approximately(chaosChanged, 0))
        {
            addChoasText = $"<size=70%> <color=white>(--)</color></size>";
        }
        else if (chaosChanged > 0)
        {
            addChoasText = $"<size=70%> <color=red>(+{chaosChanged.ToString("F0")}/s)</color></size>";
        }
        else
        {
            addChoasText = $"<size=70%> <color=green>({chaosChanged.ToString("F0")}/s)</color></size>";
        }
        _chaosTmp.text += addChoasText;
    }



    private float IncreaseChaos()
    {
        int chaosCauseCount = 0;
        foreach (PostStudent student in _studentList)
        {
            if (student.IsCausingChaos)
            {
                Debug.Log($"[IsCausingChaos] : {student.gameObject.name}");
                chaosCauseCount++;
            }
        }
        float chaosChanged = 0;
        if (chaosCauseCount > 0)
        {
            chaosChanged = chaosCauseCount * _increasePerStud * WaveSystem.Instance.ChaosFactor;
            _remainedChaosDecreaseTime = CHAOS_RECREASE_DELAY;
            float delta = chaosChanged * Time.deltaTime;
            float previous = _chaosStat.Current;
            _chaosStat.Increase(delta);
            float actualDelta = _chaosStat.Current - previous;
            if (!Mathf.Approximately(actualDelta, 0f))
                ChaosChanged?.Invoke(new ChaosChangedData(_chaosStat.Current, actualDelta, chaosChanged, ChaosChangeReason.ContinuousHazard));
        }
        else if (!_chaosStat.IsDepleted && _remainedChaosDecreaseTime <= 0)
        {
            if (GameManager.Instance.Difficulty != DifficultyLevel.Hard || _chaosStat.Current > 100)
            {
                chaosChanged = _defaultReduction * _chaosDecreaseModifier.GetFinalValue();
                float rate = chaosChanged;
                float previous = _chaosStat.Current;
                _chaosStat.Decrease(rate * Time.deltaTime);
                chaosChanged = -rate;
                float actualDelta = _chaosStat.Current - previous;
                if (!Mathf.Approximately(actualDelta, 0f))
                    ChaosChanged?.Invoke(new ChaosChangedData(_chaosStat.Current, actualDelta, chaosChanged, ChaosChangeReason.NaturalDecay));
            }
        }
        _currentChaosRate = chaosChanged;
        return chaosChanged;
    }



    private float UpdateTutorialChaos()
    {
        int chaosCauseCount = 0;
        if (_tutorialPolicy.allowContinuousChaosSources)
        {
            foreach (PostStudent student in _studentList)
            {
                if (student != null && student.CountsForStageAggregation && student.IsCausingChaos)
                    chaosCauseCount++;
            }
        }

        float chaosChanged = 0f;
        if (chaosCauseCount > 0)
        {
            chaosChanged = chaosCauseCount * _increasePerStud * _runtimeConfig.TutorialChaosFactor;
            _remainedChaosDecreaseTime = CHAOS_RECREASE_DELAY;
            float previous = _chaosStat.Current;
            _chaosStat.Increase(chaosChanged * Time.deltaTime);
            float actualDelta = _chaosStat.Current - previous;
            if (!Mathf.Approximately(actualDelta, 0f))
                ChaosChanged?.Invoke(new ChaosChangedData(_chaosStat.Current, actualDelta, chaosChanged, ChaosChangeReason.ContinuousHazard));
        }
        else if (_tutorialPolicy.allowChaosDecay
            && !_chaosStat.IsDepleted
            && _remainedChaosDecreaseTime <= 0f)
        {
            if (GameManager.Instance.Difficulty != DifficultyLevel.Hard || _chaosStat.Current > 100f)
            {
                float rate = _defaultReduction * _chaosDecreaseModifier.GetFinalValue();
                float previous = _chaosStat.Current;
                _chaosStat.Decrease(rate * Time.deltaTime);
                chaosChanged = -rate;
                float actualDelta = _chaosStat.Current - previous;
                if (!Mathf.Approximately(actualDelta, 0f))
                    ChaosChanged?.Invoke(new ChaosChangedData(_chaosStat.Current, actualDelta, chaosChanged, ChaosChangeReason.NaturalDecay));
            }
        }

        if (_remainedChaosDecreaseTime > 0f)
            _remainedChaosDecreaseTime -= Time.deltaTime;

        return chaosChanged;
    }



    private void DecreaseTime()
    {
        _timerStat.Decrease(Time.deltaTime);
        //_chaosStat.Decrease(_defaultReduction * Time.deltaTime);
    }



    private void DecreaseTutorialTime()
    {
        if (_tutorialTimerRemaining <= 0f || _tutorialTimerFinishedReported) return;
        _tutorialTimerRemaining = Mathf.Max(0f, _tutorialTimerRemaining - Time.deltaTime);
        if (_tutorialTimerRemaining <= 0f)
        {
            _tutorialTimerFinishedReported = true;
            GameOver(true);
        }
    }



    public float GetChaosEffectedDelay(float delay)
    {
        float chaosRatio = _chaosStat.Ratio;
        float delayFactor = _delayFuncFactor * chaosRatio * chaosRatio + (_minDelayFactor - 1 - _delayFuncFactor) * chaosRatio + 1;
        return delayFactor * delay;
    }



    public float GetChaosEffectedWeight(float originWeight, float maxFactor)
    {
        float chaosRatio = _chaosStat.Ratio;
        float factor = (maxFactor - 1) / (1 - _minDelayFactor);
        float weightFactor = factor * (-_delayFuncFactor * chaosRatio * chaosRatio - (_minDelayFactor - 1 - _delayFuncFactor) * chaosRatio) + 1;
        return originWeight * weightFactor;
    }



    private void PopupChaosWarning(ChaosInfo choasInfo)
    {
        _chaosUi.SpawnWarningPanel(choasInfo);
    }



    private void ApplyChaosIncrease(float requestedDelta, ChaosChangeReason reason)
    {
        float previous = _chaosStat.Current;
        _chaosStat.Increase(requestedDelta);
        float actualDelta = _chaosStat.Current - previous;
        if (requestedDelta > 0f)
            _remainedChaosDecreaseTime = CHAOS_RECREASE_DELAY;
        if (actualDelta > 0f)
        {
            ChaosChanged?.Invoke(new ChaosChangedData(_chaosStat.Current, actualDelta, 0f, reason));
        }
    }



    private void OnTimerDepleted()
    {
        GameOver(true);
    }



    private void OnEscapeLimitReached()
    {
        if (!IsTutorialRuntime)
            GameOver(false);
    }



    public bool ApplyTutorialPolicy(TutorialStagePolicy policy)
    {
        if (!EnsureTutorialControl(nameof(ApplyTutorialPolicy))) return false;
        _tutorialPolicy = policy;
        _tutorialSimulationStopped = false;
        return true;
    }



    public bool SetChaosForTutorial(float value)
    {
        if (!EnsureTutorialControl(nameof(SetChaosForTutorial))) return false;
        float previous = _chaosStat.Current;
        _chaosStat.Initialize(true);
        _chaosStat.Increase(Mathf.Clamp(value, 0f, _chaosStat.Max));
        _remainedChaosDecreaseTime = 0f;
        _currentChaosRate = 0f;
        float delta = _chaosStat.Current - previous;
        if (!Mathf.Approximately(delta, 0f))
            ChaosChanged?.Invoke(new ChaosChangedData(_chaosStat.Current, delta, 0f, ChaosChangeReason.Reset));
        return true;
    }



    public bool SetProjectProgressForTutorial(float ratio)
    {
        if (!EnsureTutorialControl(nameof(SetProjectProgressForTutorial))) return false;
        float previous = _projectStat.Ratio;
        _projectStat.Initialize(true);
        _projectStat.Increase(Mathf.Clamp01(ratio) * _projectStat.Max);
        if (!Mathf.Approximately(previous, _projectStat.Ratio))
            ProjectProgressChanged?.Invoke(previous, _projectStat.Ratio);
        return true;
    }



    public bool SetEscapeCountForTutorial(int count, int failureThreshold)
    {
        if (!EnsureTutorialControl(nameof(SetEscapeCountForTutorial))) return false;
        _tutorialEscapeFailureThreshold = Mathf.Max(1, failureThreshold);
        _tutorialEscapeCount = Mathf.Max(0, count);
        _escapeStat.Initialize(true);
        _escapeStat.Increase(_tutorialEscapeCount);
        EscapeCountChanged?.Invoke(EscapeCount, EscapeFailureThreshold);
        return true;
    }



    public bool SetTimerForTutorial(float seconds)
    {
        if (!EnsureTutorialControl(nameof(SetTimerForTutorial))) return false;
        _tutorialTimerRemaining = Mathf.Max(0f, seconds);
        _tutorialTimerFinishedReported = false;
        _timerStat.Initialize(true);
        _timerStat.Increase(_tutorialTimerRemaining);
        return true;
    }



    public bool SetSessionMoneyForTutorial(int money)
    {
        if (!EnsureTutorialControl(nameof(SetSessionMoneyForTutorial))) return false;
        _money = Mathf.Max(0, money);
        return true;
    }



    public bool TrySpendTutorialSessionMoney(int cost)
    {
        if (!EnsureTutorialControl(nameof(TrySpendTutorialSessionMoney))) return false;
        cost = Mathf.Max(0, cost);
        if (_money < cost) return false;
        _money -= cost;
        return true;
    }



    public bool StopAllStageSimulationForTutorial()
    {
        if (!EnsureTutorialControl(nameof(StopAllStageSimulationForTutorial))) return false;
        _tutorialSimulationStopped = true;
        _currentChaosRate = 0f;
        return true;
    }



    public bool ResumeStageSimulationForTutorial()
    {
        if (!EnsureTutorialControl(nameof(ResumeStageSimulationForTutorial))) return false;
        _tutorialSimulationStopped = false;
        return true;
    }



    public bool RegisterStudent(PostStudent student)
    {
        if (student == null || _studentList.Contains(student)) return false;
        _studentList.Add(student);
        HookStudent(student, true);
        return true;
    }



    public bool UnregisterStudent(PostStudent student)
    {
        if (student == null || !_studentList.Remove(student)) return false;
        student.DieEvent.RemoveListener(OnStudentDied);
        student.EscapeEvent.RemoveListener(OnStudentEscaped);
        StudentUnregistered?.Invoke(student);
        return true;
    }



    private void HookStudent(PostStudent student, bool emitRegistration)
    {
        if (student == null) return;
        student.DieEvent.RemoveListener(OnStudentDied);
        student.EscapeEvent.RemoveListener(OnStudentEscaped);
        student.DieEvent.AddListener(OnStudentDied);
        student.EscapeEvent.AddListener(OnStudentEscaped);
        if (emitRegistration)
            StudentRegistered?.Invoke(student);
    }



    private bool EnsureTutorialControl(string api)
    {
        if (IsTutorialRuntime) return true;
        Debug.LogError($"{api}는 명시적인 Tutorial StageRuntimeConfig가 적용된 runtime에서만 호출할 수 있습니다.", this);
        return false;
    }



    protected override void OnDestroy()
    {
        foreach (PostStudent student in _studentList)
        {
            if (student == null) continue;
            student.DieEvent.RemoveListener(OnStudentDied);
            student.EscapeEvent.RemoveListener(OnStudentEscaped);
        }
        base.OnDestroy();
    }



    public void GunShoot()
    {
        if (IsTutorialRuntime && !_tutorialPolicy.allowGunshotChaos) return;
        float chaosFactor = IsTutorialRuntime
            ? _runtimeConfig.TutorialChaosFactor
            : WaveSystem.Instance.ChaosFactor;
        float chaosIncrease = _gunShotPenalty * chaosFactor;
        ApplyChaosIncrease(chaosIncrease, ChaosChangeReason.Gunshot);
    }



    public void NormalFoodRemoved()
    {
        if (IsTutorialRuntime && !_tutorialPolicy.allowNormalFoodRemovedChaos) return;
        float chaosFactor = IsTutorialRuntime
            ? _runtimeConfig.TutorialChaosFactor
            : WaveSystem.Instance.ChaosFactor;
        float chaosIncrease = _normalFoodRemovedPenalty * chaosFactor;
        ApplyChaosIncrease(chaosIncrease, ChaosChangeReason.NormalFoodRemoved);
        PopupChaosWarning(new NormalFoodRemovedChaos(chaosIncrease));
    }



    public bool SetTutorialEquipSlotItems(IReadOnlyList<WeaponItem> items)
    {
        if (!EnsureTutorialControl(nameof(SetTutorialEquipSlotItems))) return false;
        if (items == null)
        {
            Debug.LogError("Tutorial equip slot items cannot be null.", this);
            return false;
        }
        if (items.Count > _equipSlotList.Count)
        {
            Debug.LogError($"Tutorial loadout has {items.Count} slots, but the stage HUD has {_equipSlotList.Count}.", this);
            return false;
        }
        for (int i = 0; i < _equipSlotList.Count; i++)
        {
            if (_equipSlotList[i] != null) continue;
            Debug.LogError($"Tutorial equip UI slot {i} is missing.", this);
            return false;
        }

        for (int i = 0; i < _equipSlotList.Count; i++)
        {
            WeaponItem item = i < items.Count ? items[i] : null;
            if (item != null)
                _equipSlotList[i].SetItem(item);
            else
                _equipSlotList[i].ClearItem();
        }
        return true;
    }



    public void WeaponEquiped(int index)
    {
        if (index >= _equipInfos.Length) return;
        for (int i = 0; i < _equipInfos.Length; i++)
        {
            if (index == i) continue;
            _equipInfos[i].Unequiped();
        }
        _equipInfos[index].Equiped();
    }



    public void WeaponBulletFilled(int index)
    {
        _equipInfos[index].BulletFilled();
    }



    public void WeaponBulletDepleted(int index)
    {
        _equipInfos[index].BulletDepleted();
    }
}
