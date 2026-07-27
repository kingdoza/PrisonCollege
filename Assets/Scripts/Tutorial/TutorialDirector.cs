using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialDirector : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private TutorialCourseDefinition _courseDefinition;

    [Header("Explicit references")]
    [SerializeField] private TutorialStageFacade _facade;
    [SerializeField] private TutorialActorDirector _actorDirector;
    [SerializeField] private TutorialCheckpointService _checkpointService;
    [SerializeField] private TutorialHUDPresenter _hud;
    [SerializeField] private StageHUDPresenter _stageHUDPresenter;
    [SerializeField] private TutorialHighlighter _highlighter;
    [SerializeField] private TutorialInput _input;
    [SerializeField] private TutorialPlayerInputGate _playerInputGate;
    [SerializeField] private TutorialMovementMarker _movementMarker;
    [SerializeField] private TutorialObjectiveMarkerPresenter _objectiveMarkerPresenter;
    [SerializeField] private TutorialStudentFocusSource _studentFocusSource;
    [SerializeField] private TutorialRiskInfoBubblePresenter _riskInfoBubblePresenter;
    [Tooltip("TutorialStepId 순서와 무관하게 직접 연결합니다. 각 ID는 정확히 한 번만 있어야 합니다.")]
    [SerializeField] private TutorialStepBase[] _steps = Array.Empty<TutorialStepBase>();

    private readonly Dictionary<TutorialStepId, TutorialStepBase> _stepMap = new();
    private readonly TutorialStepId[] _order =
    {
        TutorialStepId.Intro,
        TutorialStepId.Movement,
        TutorialStepId.Barricades,
        TutorialStepId.RiskResponse,
        TutorialStepId.PowerRecovery,
        TutorialStepId.MicrowaveManagement,
        TutorialStepId.InnocentStudent,
        TutorialStepId.ChaosDecay,
        TutorialStepId.StudentWork,
        TutorialStepId.ProfessorWork,
        TutorialStepId.MiniWavePreparation,
        TutorialStepId.MiniWave,
    };
    private TutorialStepBase _currentStep;
    private TutorialStepBase _queuedCompletedStep;
    private int _currentIndex = -1;
    private bool _isTransitioning;
    private bool _isInitialized;

    public TutorialStepId CurrentStepId => _currentStep != null ? _currentStep.StepId : TutorialStepId.Intro;



    public bool InitializeDirector()
    {
        if (_isInitialized) return true;
        if (_courseDefinition == null
            || _facade == null || !_facade.IsInitialized
            || _actorDirector == null || !_actorDirector.IsInitialized
            || _checkpointService == null
            || _hud == null
            || _stageHUDPresenter == null
            || _highlighter == null
            || _input == null
            || _playerInputGate == null
            || _movementMarker == null
            || _objectiveMarkerPresenter == null
            || _studentFocusSource == null
            || _riskInfoBubblePresenter == null)
        {
            Debug.LogError("TutorialDirector 필수 참조 또는 선행 초기화가 누락됐습니다.", this);
            return false;
        }

        IReadOnlyList<string> definitionErrors = _courseDefinition.ValidateDefinition();
        foreach (string error in definitionErrors)
            Debug.LogError($"TutorialCourseDefinition: {error}", _courseDefinition);
        if (definitionErrors.Count > 0) return false;

        StageRuntimeConfig runtimeConfig = _facade.RuntimeConfig;
        if (runtimeConfig == null || !runtimeConfig.IsTutorial)
        {
            Debug.LogError("Tutorial StageRuntimeConfig가 명시적으로 연결되지 않았습니다.", this);
            return false;
        }
        if (runtimeConfig.UsePreparation
            || runtimeConfig.AutoSpawnStudents
            || runtimeConfig.UseInventoryLoadout
            || runtimeConfig.UseWavePresentation
            || runtimeConfig.FinishPolicy != StageFinishPolicy.ReportOnly)
        {
            Debug.LogError("Tutorial StageRuntimeConfig는 준비/auto spawn/정규 inventory/wave presentation을 끄고 FinishPolicy를 ReportOnly로 설정해야 합니다.", runtimeConfig);
            return false;
        }
        if (string.IsNullOrWhiteSpace(runtimeConfig.TutorialStageTitle))
        {
            Debug.LogError("튜토리얼 메뉴에 표시할 Tutorial Stage Title을 Inspector에서 설정해야 합니다.", runtimeConfig);
            return false;
        }
        if (runtimeConfig.TutorialBehaviorWeightSet == null)
        {
            Debug.LogError("P-29 BehaviorWeightSet asset을 Inspector에서 연결해야 합니다.", runtimeConfig);
            return false;
        }
        if (runtimeConfig.MiniWaveLoadout == null || runtimeConfig.MiniWaveLoadout.Length == 0)
        {
            Debug.LogError("P-28 미니웨이브 고정 loadout을 Inspector에서 설정해야 합니다.", runtimeConfig);
            return false;
        }
        if (runtimeConfig.TrainingLoadout == null || runtimeConfig.TrainingLoadout.Length == 0)
        {
            Debug.LogError("튜토리얼 0~7단계용 고정 training loadout을 Inspector에서 설정해야 합니다.", runtimeConfig);
            return false;
        }
        bool hasTrainingEmptySlot = false;
        foreach (TutorialLoadoutEntry entry in runtimeConfig.TrainingLoadout)
        {
            if (!_facade.WeaponController.TryResolveTutorialWeapon(entry, out _))
            {
                Debug.LogError("training loadout의 WeaponItem 또는 빈 슬롯 설정을 런타임 무기로 해석할 수 없습니다.", runtimeConfig);
                return false;
            }
            if (entry.isEmptySlot) hasTrainingEmptySlot = true;
        }
        if (!hasTrainingEmptySlot)
        {
            Debug.LogError("P-21 연수용 부스터를 지급할 isEmptySlot 슬롯이 training loadout에 필요합니다.", runtimeConfig);
            return false;
        }
        foreach (TutorialLoadoutEntry entry in runtimeConfig.MiniWaveLoadout)
        {
            if (!_facade.WeaponController.TryResolveTutorialWeapon(entry, out _))
            {
                Debug.LogError("P-28 미니웨이브 loadout의 WeaponItem 또는 빈 슬롯 설정을 런타임 무기로 해석할 수 없습니다.", runtimeConfig);
                return false;
            }
        }
        TutorialLoadoutEntry workTrainingBoostEntry = runtimeConfig.WorkTrainingBoost;
        if (workTrainingBoostEntry.isEmptySlot
            || !workTrainingBoostEntry.fillToMaximum
            || !_facade.WeaponController.TryResolveTutorialWeapon(workTrainingBoostEntry, out WeaponBase workTrainingBoostWeapon))
        {
            Debug.LogError("P-21/P-23 6단계 연수용 부스터 WeaponItem을 연결하고 Fill To Maximum을 켜야 합니다.", runtimeConfig);
            return false;
        }
        if (!(workTrainingBoostWeapon.EffectData is BoostData))
        {
            Debug.LogError("6단계 Work Training Boost에는 부스터 WeaponItem을 연결해야 합니다.", workTrainingBoostWeapon);
            return false;
        }

        if (!_playerInputGate.InitializeGate(_facade.Player)
            || !_objectiveMarkerPresenter.InitializePresenter()
            || !_studentFocusSource.InitializeSource()
            || !_riskInfoBubblePresenter.InitializePresenter()
            || !_movementMarker.InitializeMarker(_objectiveMarkerPresenter))
            return false;

        _stepMap.Clear();
        foreach (TutorialStepBase step in _steps)
        {
            if (step == null || _stepMap.ContainsKey(step.StepId))
            {
                Debug.LogError("TutorialStep 참조가 null이거나 ID가 중복됐습니다.", this);
                return false;
            }
            _stepMap.Add(step.StepId, step);
        }
        foreach (TutorialStepId id in _order)
        {
            if (!_stepMap.ContainsKey(id))
            {
                Debug.LogError($"{id} TutorialStep 컴포넌트가 연결되지 않았습니다.", this);
                return false;
            }
            if (!_courseDefinition.TryGetContent(id, out _))
            {
                Debug.LogError($"{id} 문구가 TutorialCourseDefinition에 등록되지 않았습니다.", _courseDefinition);
                return false;
            }
        }

        TutorialStepContext context = new()
        {
            courseDefinition = _courseDefinition,
            facade = _facade,
            actors = _actorDirector,
            checkpoint = _checkpointService,
            hud = _hud,
            highlighter = _highlighter,
            input = _input,
            playerInputGate = _playerInputGate,
            movementMarker = _movementMarker,
            objectiveMarkers = _objectiveMarkerPresenter,
            studentFocus = _studentFocusSource,
            riskInfoBubble = _riskInfoBubblePresenter,
        };
        foreach (TutorialStepBase step in _steps)
            step.InitializeStep(this, context);

        if (!_hud.InitializeButtons(RestartMiniWave, SkipMiniWave, Reenroll, ExitToMainMenu))
            return false;
        if (!_facade.SetEscapeCount(0, _courseDefinition.MiniWaveEscapeFailureThreshold))
            return false;

        _isInitialized = true;
        _isTransitioning = true;
        if (!EnterIndex(0))
        {
            _isTransitioning = false;
            return false;
        }
        StartCoroutine(PlayInitialStepPanelUnfold());
        return true;
    }



    private IEnumerator PlayInitialStepPanelUnfold()
    {
        // Let the initialized minimum-height state render once so the first step
        // has the same visible unfold transition as every later step.
        yield return null;
        if (!_isInitialized || _currentStep == null) yield break;
        _hud.PlayStepPanelUnfold(FinishStepPanelUnfold);
    }



    private void Update()
    {
        _currentStep?.TickStep();
    }



    public void OnStepCompleted(TutorialStepBase step)
    {
        if (step == null || step != _currentStep) return;
        if (_isTransitioning)
        {
            _queuedCompletedStep = step;
            return;
        }

        _queuedCompletedStep = null;
        _isTransitioning = true;
        _hud.PlayObjectiveCompletionFeedback(() => BeginStepPanelFold(step));
    }



    private void BeginStepPanelFold(TutorialStepBase completedStep)
    {
        if (completedStep == null || completedStep != _currentStep)
        {
            _isTransitioning = false;
            return;
        }

        _hud.PlayStepPanelFold(() => FinishStepTransition(completedStep));
    }



    private void FinishStepTransition(TutorialStepBase completedStep)
    {
        if (completedStep == null || completedStep != _currentStep)
        {
            _isTransitioning = false;
            return;
        }

        _hud.HideStep();
        completedStep.ExitStep();
        _currentStep = null;
        _queuedCompletedStep = null;
        int nextIndex = _currentIndex + 1;
        if (nextIndex < _order.Length)
        {
            if (!EnterIndex(nextIndex))
            {
                _isTransitioning = false;
                return;
            }
            _hud.PlayStepPanelUnfold(FinishStepPanelUnfold);
            return;
        }

        _isTransitioning = false;
        CompleteCourse();
    }



    private void CompleteCourse()
    {
        _facade.StopAllStageSimulation();
        _facade.ForceStopProfessorTasks(ProfessorTaskStopReason.StepExit);
        _facade.SetPlayerDeathAllowed(false);
        _actorDirector.StopAllActors();
        _playerInputGate.Acquire(TutorialPlayerInputLockReason.MiniWaveResult);
        _hud.ShowCourseSummaryButtons(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 0f;
        GameManager.Instance.MarkTutorialCompleted();
    }



    private void FinishStepPanelUnfold()
    {
        _isTransitioning = false;
        TutorialStepBase queuedStep = _queuedCompletedStep;
        _queuedCompletedStep = null;
        if (queuedStep != null && queuedStep == _currentStep)
            OnStepCompleted(queuedStep);
    }



    public void RestartMiniWave()
    {
        if (_currentStep is TutorialMiniWaveStep miniWaveStep)
            miniWaveStep.RestartFromCheckpoint();
    }



    public void SkipMiniWave()
    {
        if (!(_currentStep is TutorialMiniWaveStep miniWaveStep)) return;
        GameManager.Instance.MarkTutorialCompleted();
        miniWaveStep.CompleteFromDirector();
    }



    public void Reenroll()
    {
        Time.timeScale = 1f;
        GameManager.Instance.StartTutorial();
    }



    public void ExitToMainMenu()
    {
        Time.timeScale = 1f;
        GameManager gameManager = GameManager.Instance;
        gameManager.MarkTutorialCompleted();
        gameManager.hasToStageSelect = false;
        gameManager.ShowMainScreen();
    }



    public void AbortToMainMenu()
    {
        Time.timeScale = 1f;
        GameManager gameManager = GameManager.Instance;
        gameManager.hasToStageSelect = false;
        gameManager.ShowMainScreen();
    }



    private bool EnterIndex(int index)
    {
        if (index < 0 || index >= _order.Length) return false;
        TutorialStepId stepId = _order[index];
        if (!_stageHUDPresenter.ApplyTutorialTopHUDVisibility(
            GetTopHUDVisibility(stepId)))
        {
            Debug.LogError($"{stepId} 단계의 상단 HUD 표시 정책 적용에 실패했습니다.", this);
            return false;
        }

        _currentIndex = index;
        _currentStep = _stepMap[stepId];
        if (!_currentStep.EnterStep())
        {
            _stageHUDPresenter.ApplyTutorialTopHUDVisibility(StageTopHUDVisibility.None);
            Debug.LogError($"{stepId} 단계 진입에 실패해 튜토리얼을 중단합니다.", this);
            _currentStep = null;
            return false;
        }
        return true;
    }



    private static StageTopHUDVisibility GetTopHUDVisibility(TutorialStepId stepId)
    {
        switch (stepId)
        {
            case TutorialStepId.InnocentStudent:
            case TutorialStepId.ChaosDecay:
                return StageTopHUDVisibility.Chaos;

            case TutorialStepId.StudentWork:
            case TutorialStepId.ProfessorWork:
                return StageTopHUDVisibility.Project;

            case TutorialStepId.MiniWavePreparation:
            case TutorialStepId.MiniWave:
            case TutorialStepId.CourseSummary:
                return StageTopHUDVisibility.All;

            default:
                return StageTopHUDVisibility.None;
        }
    }



    private void OnDestroy()
    {
        // Tutorial scene unload와 Editor Play 종료 모두 파괴 순서와 무관하게 일시정지를 해제한다.
        Time.timeScale = 1f;
        _currentStep?.ExitStep();
        _objectiveMarkerPresenter?.ClearAll();
        _highlighter?.ClearAllHighlights();
    }
}
