using System;
using System.Collections.Generic;
using UnityEngine;

public class TutorialStageFacade : MonoBehaviour
{
    [Header("Required scene references")]
    [SerializeField] private StageController _stageController;
    [SerializeField] private Professor _player;
    [SerializeField] private WeaponController _weaponController;
    [SerializeField] private ExitGate[] _exitGates = Array.Empty<ExitGate>();
    [SerializeField] private ProfessorTask[] _professorTasks = Array.Empty<ProfessorTask>();

    [Header("Checkpoint facilities")]
    [SerializeField] private Microwave[] _microwaves = Array.Empty<Microwave>();
    [SerializeField] private Fire[] _fires = Array.Empty<Fire>();
    [SerializeField] private FireSuppressionSystem _fireSuppressionSystem;
    [SerializeField] private LabLightSystem _labLightSystem;

    [Header("P-28 - Unity Editor에서 명시적으로 설정")]
    [Tooltip("보충 시설 사용 여부와 세션 비용 설정을 완료한 뒤 체크합니다.")]
    [SerializeField] private bool _p28RechargerConfigurationConfirmed;
    [SerializeField] private TutorialRechargerBinding[] _rechargers = Array.Empty<TutorialRechargerBinding>();

    private bool _isInitialized;

    public float TimerRemaining => _stageController.TimerRemaining;
    public float Chaos => _stageController.Chaos;
    public float ChaosRate => _stageController.ChaosRate;
    public int EscapeCount => _stageController.EscapeCount;
    public int EscapeFailureThreshold => _stageController.EscapeFailureThreshold;
    public float ProjectProgress => _stageController.ProjectProgress;
    public int WorkingStudentCount => _stageController.WorkingStudentCount;
    public bool IsProfessorWorking => _stageController.IsProfessorWorking;
    public int SessionMoney => _stageController.SessionMoney;
    public IReadOnlyList<PostStudent> Students => _stageController.Students;
    public IReadOnlyList<ExitGate> ExitGates => _exitGates;
    public IReadOnlyList<ProfessorTask> ProfessorTasks => _professorTasks;
    public IReadOnlyList<Microwave> Microwaves => _microwaves;
    public IReadOnlyList<TutorialRechargerBinding> RechargerBindings => _rechargers;
    public StageRuntimeConfig RuntimeConfig => _stageController.RuntimeConfig;
    public Professor Player => _player;
    public WeaponController WeaponController => _weaponController;
    public bool IsInitialized => _isInitialized;

    public event Action<ChaosChangedData> ChaosChanged;
    public event Action<int, int> EscapeCountChanged;
    public event Action<float, float> ProjectProgressChanged;
    public event Action<int, ProjectContributor> ProjectCompleted;
    public event Action<int> WorkingStudentCountChanged;
    public event Action<PostStudent, HitInfo, bool> StudentDowned;
    public event Action<PostStudent> StudentEscaped;
    public event Action<StageFinishResult> StageFinished;
    public event Action<ExitGate, bool> BarricadeStateChanged;
    public event Action<ProfessorTask> ProfessorTaskStarted;
    public event Action<ProfessorTask, ProfessorTaskStopReason> ProfessorTaskStopped;
    public event Action LabLightsRestored;
    public event Action<Microwave, bool> MicrowaveFoodRemoved;



    public bool InitializeFacade()
    {
        if (_isInitialized) return true;
        if (_stageController == null
            || !_stageController.IsTutorialRuntime
            || _player == null
            || _weaponController == null
            || _fireSuppressionSystem == null
            || _labLightSystem == null
            || _exitGates == null
            || _exitGates.Length == 0)
        {
            Debug.LogError("TutorialStageFacade 필수 참조 또는 명시적 Tutorial runtime config가 누락됐습니다.", this);
            return false;
        }
        if (!_p28RechargerConfigurationConfirmed)
        {
            Debug.LogError("P-28 보충 시설과 세션 비용 설정을 완료하고 확인 체크를 켜야 합니다.", this);
            return false;
        }
        foreach (TutorialRechargerBinding binding in _rechargers)
        {
            if (binding.recharger == null)
            {
                Debug.LogError("P-28 Recharger binding 참조가 누락됐습니다.", this);
                return false;
            }
        }
        HashSet<Microwave> registeredMicrowaves = new();
        for (int i = 0; i < _microwaves.Length; i++)
        {
            Microwave microwave = _microwaves[i];
            if (microwave != null && !registeredMicrowaves.Add(microwave))
            {
                Debug.LogError($"TutorialStageFacade microwaves[{i}] 참조가 중복됐습니다.", this);
                return false;
            }
        }

        for (int i = 0; i < _exitGates.Length; i++)
        {
            ExitGate gate = _exitGates[i];
            if (gate == null)
            {
                Debug.LogError($"TutorialStageFacade exitGates[{i}] 참조가 없습니다.", this);
                return false;
            }
            gate.BarricadePlacedEvent += OnBarricadePlaced;
            gate.BarricadeBrokenEvent += OnBarricadeBroken;
            // P-07: 씬 최초 진입 때만 모든 출구를 미설치 상태로 구성한다.
            gate.SetBarricadeStateForSetup(false);
        }

        foreach (ProfessorTask task in _professorTasks)
        {
            if (task == null) continue;
            task.TaskStartedEvent += OnProfessorTaskStarted;
            task.TaskStoppedEvent += OnProfessorTaskStopped;
        }

        for (int i = 0; i < _microwaves.Length; i++)
        {
            Microwave microwave = _microwaves[i];
            if (microwave == null) continue;
            microwave.FoodRemovedEvent += OnMicrowaveFoodRemoved;
        }

        foreach (TutorialRechargerBinding binding in _rechargers)
        {
            binding.recharger.ConfigureTutorialRuntime(binding.enabled, binding.sessionCost);
        }

        _stageController.ChaosChanged += ForwardChaosChanged;
        _stageController.EscapeCountChanged += ForwardEscapeCountChanged;
        _stageController.ProjectProgressChanged += ForwardProjectProgressChanged;
        _stageController.ProjectCompleted += ForwardProjectCompleted;
        _stageController.WorkingStudentCountChanged += ForwardWorkingStudentCountChanged;
        _stageController.StudentDowned += ForwardStudentDowned;
        _stageController.StudentEscaped += ForwardStudentEscaped;
        _stageController.StageFinished += ForwardStageFinished;
        _labLightSystem.LightsOnEvent.AddListener(ForwardLabLightsRestored);
        _isInitialized = true;
        return true;
    }



    public bool ApplyPolicy(TutorialStagePolicy policy)
    {
        foreach (ProfessorTask task in _professorTasks)
            task?.SetTutorialInteractionAllowed(policy.allowProfessorTask);
        return _stageController.ApplyTutorialPolicy(policy);
    }
    public bool SetChaos(float value) => _stageController.SetChaosForTutorial(value);
    public bool SetProjectProgress(float ratio) => _stageController.SetProjectProgressForTutorial(ratio);
    public bool SetEscapeCount(int value, int threshold) => _stageController.SetEscapeCountForTutorial(value, threshold);
    public bool SetTimer(float seconds) => _stageController.SetTimerForTutorial(seconds);
    public bool SetSessionMoney(int money) => _stageController.SetSessionMoneyForTutorial(money);
    public bool StopAllStageSimulation() => _stageController.StopAllStageSimulationForTutorial();
    public bool ResumeStageSimulation() => _stageController.ResumeStageSimulationForTutorial();



    public bool BeginPowerRecoveryTraining()
    {
        if (!_isInitialized
            || _stageController == null
            || !_stageController.IsTutorialRuntime
            || _labLightSystem == null)
        {
            Debug.LogError("정전 복구 연수는 초기화된 튜토리얼 runtime에서만 시작할 수 있습니다.", this);
            return false;
        }

        if (_labLightSystem.IsLightsOn)
            _stageController.Hacked();

        _labLightSystem.TurnOff();
        return !_labLightSystem.IsLightsOn;
    }



    public bool BeginMicrowaveManagementTraining(
        Microwave normalMicrowave,
        GameObject normalFood,
        Microwave hazardMicrowave,
        GameObject hazardFood)
    {
        if (!_isInitialized
            || _stageController == null
            || !_stageController.IsTutorialRuntime
            || normalMicrowave == null
            || hazardMicrowave == null
            || normalMicrowave == hazardMicrowave
            || normalFood == null
            || hazardFood == null
            || !IsRegisteredMicrowave(normalMicrowave)
            || !IsRegisteredMicrowave(hazardMicrowave))
        {
            Debug.LogError("3-3 전자레인지·음식 설정이 누락됐거나 등록된 튜토리얼 시설이 아닙니다.", this);
            return false;
        }

        normalMicrowave.Quit();
        hazardMicrowave.Quit();
        if (!normalMicrowave.SetTutorialExplosionSuppressed(true))
            return false;
        if (!hazardMicrowave.SetTutorialExplosionSuppressed(true))
        {
            normalMicrowave.SetTutorialExplosionSuppressed(false);
            return false;
        }

        normalMicrowave.PutFood(new FoodInfo
        {
            gameObj = normalFood,
            isCauseFire = false,
        });
        hazardMicrowave.PutFood(new FoodInfo
        {
            gameObj = hazardFood,
            isCauseFire = true,
        });
        normalMicrowave.Operate();
        hazardMicrowave.Operate();
        return normalMicrowave.IsOperating
            && hazardMicrowave.IsOperating
            && hazardMicrowave.HasHazardFood;
    }



    public void EndMicrowaveManagementTraining(Microwave first, Microwave second)
    {
        EndMicrowaveManagementTraining(first);
        if (second != first)
            EndMicrowaveManagementTraining(second);
    }



    public void SetPlayerDeathAllowed(bool isAllowed)
    {
        _player.SetTutorialInvincible(!isAllowed);
    }



    public void ForceStopProfessorTasks(ProfessorTaskStopReason reason)
    {
        foreach (ProfessorTask task in _professorTasks)
            task?.ForceStopTask(reason);
    }



    public void CancelPlayerInteraction()
    {
        if (_player != null && _player.TryGetComponent(out PlayerInteraction interaction))
            interaction.CancelActiveInteraction();
    }



    public bool IsAnyProfessorTaskActive()
    {
        foreach (ProfessorTask task in _professorTasks)
            if (task != null && task.IsTasking) return true;
        return false;
    }



    public TutorialGateState[] CaptureGateStates()
    {
        TutorialGateState[] states = new TutorialGateState[_exitGates.Length];
        for (int i = 0; i < states.Length; i++)
        {
            states[i] = new TutorialGateState
            {
                gate = _exitGates[i],
                isBarricadePlaced = _exitGates[i].IsBarricadePlaced,
                health = _exitGates[i].CurrentHealth,
            };
        }
        return states;
    }



    public void RestoreGateStates(TutorialGateState[] states)
    {
        if (states == null) return;
        foreach (TutorialGateState state in states)
            state.gate?.SetBarricadeStateForSetup(state.isBarricadePlaced, state.health);
    }



    public TutorialMicrowaveState[] CaptureMicrowaveStates()
    {
        TutorialMicrowaveState[] states = new TutorialMicrowaveState[_microwaves.Length];
        for (int i = 0; i < states.Length; i++)
            states[i] = _microwaves[i] != null ? _microwaves[i].CaptureTutorialState() : default;
        return states;
    }



    public TutorialFireState[] CaptureFireStates()
    {
        TutorialFireState[] states = new TutorialFireState[_fires.Length];
        for (int i = 0; i < states.Length; i++)
        {
            states[i] = new TutorialFireState
            {
                fire = _fires[i],
                isBurning = _fires[i] != null && _fires[i].IsBurning,
            };
        }
        return states;
    }



    public TutorialRechargerState[] CaptureRechargerStates()
    {
        TutorialRechargerState[] states = new TutorialRechargerState[_rechargers.Length];
        for (int i = 0; i < states.Length; i++)
            states[i] = _rechargers[i].recharger.CaptureTutorialState();
        return states;
    }



    public TutorialFireSuppressionState CaptureFireSuppressionState()
    {
        return _fireSuppressionSystem.CaptureTutorialState();
    }



    public void RestoreFacilityStates(
        TutorialMicrowaveState[] microwaveStates,
        TutorialFireState[] fireStates,
        TutorialFireSuppressionState fireSuppressionState,
        TutorialRechargerState[] rechargerStates,
        bool lightsOn)
    {
        ForceStopProfessorTasks(ProfessorTaskStopReason.StepExit);
        if (microwaveStates != null)
        {
            foreach (TutorialMicrowaveState state in microwaveStates)
                state.microwave?.RestoreTutorialState(state);
        }
        if (fireStates != null)
        {
            foreach (TutorialFireState state in fireStates)
                state.fire?.SetBurningStateForTutorialSetup(state.isBurning);
        }
        fireSuppressionState.system?.RestoreTutorialState(fireSuppressionState);
        if (rechargerStates != null)
        {
            foreach (TutorialRechargerState state in rechargerStates)
                state.recharger?.RestoreTutorialState(state);
        }
        if (_labLightSystem != null)
        {
            if (lightsOn) _labLightSystem.TurnOn();
            else _labLightSystem.TurnOff();
        }
    }



    public bool AreLightsOn => _labLightSystem == null || _labLightSystem.IsLightsOn;



    private void OnBarricadePlaced(ExitGate gate) => BarricadeStateChanged?.Invoke(gate, true);
    private void OnBarricadeBroken(ExitGate gate) => BarricadeStateChanged?.Invoke(gate, false);
    private void OnProfessorTaskStarted(ProfessorTask task) => ProfessorTaskStarted?.Invoke(task);
    private void OnProfessorTaskStopped(ProfessorTask task, ProfessorTaskStopReason reason) => ProfessorTaskStopped?.Invoke(task, reason);
    private void OnMicrowaveFoodRemoved(Microwave microwave, bool wasHazard) => MicrowaveFoodRemoved?.Invoke(microwave, wasHazard);
    private void ForwardChaosChanged(ChaosChangedData data) => ChaosChanged?.Invoke(data);
    private void ForwardEscapeCountChanged(int current, int threshold) => EscapeCountChanged?.Invoke(current, threshold);
    private void ForwardProjectProgressChanged(float previous, float current) => ProjectProgressChanged?.Invoke(previous, current);
    private void ForwardProjectCompleted(int id, ProjectContributor contributors) => ProjectCompleted?.Invoke(id, contributors);
    private void ForwardWorkingStudentCountChanged(int count) => WorkingStudentCountChanged?.Invoke(count);
    private void ForwardStudentDowned(PostStudent student, HitInfo hitInfo, bool wasHazardous) => StudentDowned?.Invoke(student, hitInfo, wasHazardous);
    private void ForwardStudentEscaped(PostStudent student) => StudentEscaped?.Invoke(student);
    private void ForwardStageFinished(StageFinishResult result) => StageFinished?.Invoke(result);
    private void ForwardLabLightsRestored() => LabLightsRestored?.Invoke();



    private bool IsRegisteredMicrowave(Microwave microwave)
    {
        if (microwave == null || _microwaves == null) return false;
        for (int i = 0; i < _microwaves.Length; i++)
            if (_microwaves[i] == microwave) return true;
        return false;
    }



    private void EndMicrowaveManagementTraining(Microwave microwave)
    {
        if (microwave == null || !IsRegisteredMicrowave(microwave)) return;
        microwave.Quit();
        microwave.SetTutorialExplosionSuppressed(false);
    }



    private void OnDestroy()
    {
        if (_stageController != null)
        {
            _stageController.ChaosChanged -= ForwardChaosChanged;
            _stageController.EscapeCountChanged -= ForwardEscapeCountChanged;
            _stageController.ProjectProgressChanged -= ForwardProjectProgressChanged;
            _stageController.ProjectCompleted -= ForwardProjectCompleted;
            _stageController.WorkingStudentCountChanged -= ForwardWorkingStudentCountChanged;
            _stageController.StudentDowned -= ForwardStudentDowned;
            _stageController.StudentEscaped -= ForwardStudentEscaped;
            _stageController.StageFinished -= ForwardStageFinished;
        }
        if (_labLightSystem != null)
            _labLightSystem.LightsOnEvent.RemoveListener(ForwardLabLightsRestored);
        foreach (ExitGate gate in _exitGates)
        {
            if (gate == null) continue;
            gate.BarricadePlacedEvent -= OnBarricadePlaced;
            gate.BarricadeBrokenEvent -= OnBarricadeBroken;
        }
        foreach (ProfessorTask task in _professorTasks)
        {
            if (task == null) continue;
            task.TaskStartedEvent -= OnProfessorTaskStarted;
            task.TaskStoppedEvent -= OnProfessorTaskStopped;
        }
        foreach (Microwave microwave in _microwaves)
        {
            if (microwave == null) continue;
            microwave.FoodRemovedEvent -= OnMicrowaveFoodRemoved;
            if (_stageController != null && _stageController.IsTutorialRuntime)
                microwave.SetTutorialExplosionSuppressed(false);
        }
    }
}
