using System.Collections.Generic;
using UnityEngine;

public enum TutorialPlayerInputLockReason
{
    Intro = 0,
    MiniWaveResult = 2,
}

/// <summary>
/// TutorialStage에서만 사용하는 gameplay 입력 잠금입니다.
/// TutorialInput과 EventSystem은 건드리지 않아 Tab, Esc, UI 버튼 입력은 유지합니다.
/// </summary>
public sealed class TutorialPlayerInputGate : MonoBehaviour
{
    [Header("Explicit player references")]
    [SerializeField] private Professor _professor;
    [SerializeField] private FirstPersonController _firstPersonController;
    [SerializeField] private PlayerInteraction _playerInteraction;

    private readonly HashSet<TutorialPlayerInputLockReason> _reasons = new();
    private bool _professorWasEnabled;
    private bool _firstPersonControllerWasEnabled;
    private bool _playerInteractionWasEnabled;
    private bool _hasEnabledStateSnapshot;
    private bool _isInitialized;

    public bool IsInitialized => _isInitialized;
    public bool IsBlocked => _reasons.Count > 0;



    public bool InitializeGate(Professor expectedProfessor)
    {
        if (_isInitialized) return true;
        if (_professor == null
            || _firstPersonController == null
            || _playerInteraction == null
            || expectedProfessor == null
            || _professor != expectedProfessor
            || _firstPersonController.gameObject != _professor.gameObject
            || _playerInteraction.gameObject != _professor.gameObject)
        {
            Debug.LogError("TutorialPlayerInputGate 플레이어 입력 컴포넌트 참조가 누락되거나 서로 다른 오브젝트를 가리킵니다.", this);
            return false;
        }

        _isInitialized = true;
        return true;
    }



    public bool Acquire(TutorialPlayerInputLockReason reason)
    {
        if (!_isInitialized) return false;
        if (!_reasons.Add(reason)) return true;
        if (_reasons.Count == 1)
            ApplyBlockedState();
        return true;
    }



    public void Release(TutorialPlayerInputLockReason reason)
    {
        if (!_reasons.Remove(reason) || _reasons.Count > 0) return;
        RestoreEnabledState();
    }



    public bool HasReason(TutorialPlayerInputLockReason reason) => _reasons.Contains(reason);



    private void ApplyBlockedState()
    {
        _professorWasEnabled = _professor.enabled;
        _firstPersonControllerWasEnabled = _firstPersonController.enabled;
        _playerInteractionWasEnabled = _playerInteraction.enabled;
        _hasEnabledStateSnapshot = true;

        _playerInteraction.CancelActiveInteraction();
        _playerInteraction.enabled = false;
        _firstPersonController.enabled = false;
        _professor.enabled = false;
    }



    private void RestoreEnabledState()
    {
        if (!_hasEnabledStateSnapshot) return;
        if (_professor != null) _professor.enabled = _professorWasEnabled;
        if (_firstPersonController != null) _firstPersonController.enabled = _firstPersonControllerWasEnabled;
        if (_playerInteraction != null) _playerInteraction.enabled = _playerInteractionWasEnabled;
        _hasEnabledStateSnapshot = false;
    }



    private void OnDisable()
    {
        _reasons.Clear();
        RestoreEnabledState();
    }



    private void OnDestroy()
    {
        _reasons.Clear();
        RestoreEnabledState();
    }
}
