using UnityEngine;

[System.Flags]
public enum StageTopHUDVisibility
{
    None = 0,
    Chaos = 1 << 0,
    Center = 1 << 1,
    Project = 1 << 2,
    All = Chaos | Center | Project,
}

public class StageHUDPresenter : MonoBehaviour
{
    [Header("Read-only runtime sources")]
    [SerializeField] private StageController _stageController;
    [SerializeField] private Professor _professor;
    [SerializeField] private WeaponController _weaponController;

    [Header("HUD views")]
    [SerializeField] private StageTimerHUDView _timerView;
    [SerializeField] private StageEscapeHUDView _escapeView;
    [SerializeField] private StageProjectHUDView _projectView;
    [SerializeField] private StageChaosHUDView _chaosView;
    [SerializeField] private StageStaminaHUDView _staminaView;
    [SerializeField] private StageWeaponHUDView _weaponView;

    [Header("Top HUD visibility")]
    [SerializeField] private CanvasGroup _chaosPanelCanvasGroup;
    [SerializeField] private CanvasGroup _centerPanelCanvasGroup;
    [SerializeField] private CanvasGroup _projectPanelCanvasGroup;
    [Tooltip("튜토리얼에서 현재 단계에 필요하지 않은 상단 패널의 Alpha입니다. 정규 스테이지에는 적용되지 않습니다.")]
    [SerializeField, Range(0f, 1f)] private float _tutorialInactivePanelAlpha = 0.2f;

    private bool _started;
    private bool _initialized;
    private bool _hasTutorialVisibility;
    private StageTopHUDVisibility _tutorialVisibility = StageTopHUDVisibility.None;

    private void Start()
    {
        _started = true;
        TryInitialize();
    }

    private void OnEnable()
    {
        if (_started)
            TryInitialize();
    }

    private void OnDisable()
    {
        Shutdown();
    }

    private void OnDestroy()
    {
        Shutdown();
    }

    private void TryInitialize()
    {
        if (_initialized) return;
        if (!ValidateReferences())
        {
            Debug.LogError(
                "StageHUDPresenter 참조가 누락되어 신규 HUD만 비활성화합니다. 기존 StageController HUD에는 영향을 주지 않습니다.",
                this);
            enabled = false;
            return;
        }

        bool initialized =
            _timerView.Initialize(_stageController)
            && _escapeView.Initialize(_stageController)
            && _projectView.Initialize(_stageController)
            && _chaosView.Initialize(_stageController)
            && _staminaView.Initialize(_professor)
            && _weaponView.Initialize(_weaponController);

        if (!initialized)
        {
            Shutdown();
            Debug.LogError(
                "신규 스테이지 HUD 초기화에 실패했습니다. 각 View의 Inspector 참조를 확인하세요.",
                this);
            enabled = false;
            return;
        }

        _initialized = true;
        if (_stageController.IsTutorialRuntime)
        {
            ApplyRuntimeTopHUDVisibility();
        }
    }

    public bool ApplyTutorialTopHUDVisibility(StageTopHUDVisibility visibility)
    {
        if (_stageController == null || !_stageController.IsTutorialRuntime)
        {
            Debug.LogError(
                "튜토리얼 상단 HUD 표시 정책은 튜토리얼 runtime에서만 적용할 수 있습니다.",
                this);
            return false;
        }

        _tutorialVisibility = visibility & StageTopHUDVisibility.All;
        _hasTutorialVisibility = true;
        ApplyTopHUDVisibility(_tutorialVisibility, _tutorialInactivePanelAlpha);
        return true;
    }

    private bool ValidateReferences()
    {
        if (_stageController == null
            || _professor == null
            || _weaponController == null
            || _timerView == null
            || _escapeView == null
            || _projectView == null
            || _chaosView == null
            || _staminaView == null
            || _weaponView == null)
        {
            return false;
        }

        return !_stageController.IsTutorialRuntime
            || (_chaosPanelCanvasGroup != null
                && _centerPanelCanvasGroup != null
                && _projectPanelCanvasGroup != null);
    }

    private void ApplyRuntimeTopHUDVisibility()
    {
        if (!_stageController.IsTutorialRuntime)
        {
            return;
        }

        StageTopHUDVisibility visibility = _hasTutorialVisibility
            ? _tutorialVisibility
            : StageTopHUDVisibility.None;
        ApplyTopHUDVisibility(visibility, _tutorialInactivePanelAlpha);
    }

    private void ApplyTopHUDVisibility(
        StageTopHUDVisibility visibility,
        float inactiveAlpha)
    {
        float hiddenAlpha = Mathf.Clamp01(inactiveAlpha);
        _chaosPanelCanvasGroup.alpha =
            (visibility & StageTopHUDVisibility.Chaos) != 0 ? 1f : hiddenAlpha;
        _centerPanelCanvasGroup.alpha =
            (visibility & StageTopHUDVisibility.Center) != 0 ? 1f : hiddenAlpha;
        _projectPanelCanvasGroup.alpha =
            (visibility & StageTopHUDVisibility.Project) != 0 ? 1f : hiddenAlpha;
    }

    private void Shutdown()
    {
        if (_timerView != null) _timerView.Shutdown();
        if (_escapeView != null) _escapeView.Shutdown();
        if (_projectView != null) _projectView.Shutdown();
        if (_chaosView != null) _chaosView.Shutdown();
        if (_staminaView != null) _staminaView.Shutdown();
        if (_weaponView != null) _weaponView.Shutdown();
        _initialized = false;
    }
}
