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

    public float ProjectProgress => _projectStat.Ratio;
    public Professor Player => _player;
    public StageSpots StageSpots => _stageSpots;

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

        _reflectionProbes = _reflectionGroup.GetComponentsInChildren<ReflectionProbe>();

        _timerStat.DepletedEvent.AddListener(() => GameOver(true));
        _prepareTimeStat.DepletedEvent.AddListener(InitStage);
        _escapeStat.MaxReachEvent.AddListener(() => GameOver(false));
        _projectStat.MaxReachEvent.AddListener(OnProjectSuccessed);

        _originMoney =_money = InventorySystem.Instance.Money;
        InventorySystem.Instance.ActivatePassiveItems();

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
            student.DieEvent.AddListener(OnStudentDied);
            student.EscapeEvent.AddListener(OnStudentEscaped);
        }
    }



    private void Update()
    {
        if ((Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) && _isPreparing))
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
        _workingStudCount = 0;
        foreach (var student in _studentList)
        {
            if (student.IsWorking)
            {
                _workingStudCount++;
            }
        }
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
        float finalProgress = (studTotalProgress + profTotalProgress) * WaveSystem.Instance.ProjectFactor;
        _projectStat.Increase(finalProgress);
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
        float chaosIncrease = _studEscapedPenalty * WaveSystem.Instance.ChaosFactor;
        _chaosStat.Increase(chaosIncrease);
        _remainedChaosDecreaseTime = CHAOS_RECREASE_DELAY;
        PopupChaosWarning(new EscapedChaos(chaosIncrease));
        _escapeStat.Increase(1);
    }



    private void OnStudentDied(PostStudent student, HitInfo hitInfo)
    {
        if (hitInfo.attacker == Player.gameObject)
        {
            KillFeedbackController.Instance.PlayKillFeedback();
        }
        if (student.IsDoingHazardBehavior == false && hitInfo.attacker == Player.gameObject)
        {
            float chaosIncrease = _innocentKillPenalty * WaveSystem.Instance.ChaosFactor;
            _chaosStat.Increase(chaosIncrease);
            _remainedChaosDecreaseTime = CHAOS_RECREASE_DELAY;
            PopupChaosWarning(new InnocentKillChaos(chaosIncrease));
        }
    }



    private void OnProjectSuccessed()
    {
        _projectStat.Initialize(true);
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
        int minutes = Mathf.FloorToInt(_timerStat.Current / 60f);
        int seconds = Mathf.FloorToInt(_timerStat.Current % 60f);
        //_timerTmp.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        _timerTmp.text = _timerStat.Current.ToString("F0");
        if (_timerStat.Current < 11)
        {
            _timerTmp.text = $"<color=red>{_timerTmp.text}</color>";
        }

        _chaosTmp.text = _chaosStat.Current.ToString("F0");

        _escapeTmp.text = $"{_escapeStat.Current.ToString("F0")} / {_escapeStat.Max.ToString("F0")}";

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
            _chaosStat.Increase(chaosChanged * Time.deltaTime);
        }
        else if (!_chaosStat.IsDepleted && _remainedChaosDecreaseTime <= 0)
        {
            if (GameManager.Instance.Difficulty != DifficultyLevel.Hard || _chaosStat.Current > 100)
            {
                chaosChanged = _defaultReduction * _chaosDecreaseModifier.GetFinalValue();
                _chaosStat.Decrease(chaosChanged * Time.deltaTime);
                chaosChanged = -chaosChanged;
            }
        }
        return chaosChanged;
    }



    private void DecreaseTime()
    {
        _timerStat.Decrease(Time.deltaTime);
        //_chaosStat.Decrease(_defaultReduction * Time.deltaTime);
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



    public void GunShoot()
    {
        float chaosIncrease = _gunShotPenalty * WaveSystem.Instance.ChaosFactor;
        _remainedChaosDecreaseTime = CHAOS_RECREASE_DELAY;
        _chaosStat.Increase(chaosIncrease);
    }



    public void NormalFoodRemoved()
    {
        float chaosIncrease = _normalFoodRemovedPenalty * WaveSystem.Instance.ChaosFactor;
        _chaosStat.Increase(chaosIncrease);
        _remainedChaosDecreaseTime = CHAOS_RECREASE_DELAY;
        PopupChaosWarning(new NormalFoodRemovedChaos(chaosIncrease));
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
