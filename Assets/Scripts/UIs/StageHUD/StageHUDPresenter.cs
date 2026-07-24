using UnityEngine;

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

    private bool _started;
    private bool _initialized;

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
    }

    private bool ValidateReferences()
    {
        return _stageController != null
            && _professor != null
            && _weaponController != null
            && _timerView != null
            && _escapeView != null
            && _projectView != null
            && _chaosView != null
            && _staminaView != null
            && _weaponView != null;
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
