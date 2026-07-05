using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoorGate : ExitGate
{
    [SerializeField] private GameObject _doorPanel;
    [SerializeField] private bool _isOpened = false;
    [SerializeField] private float _openAngle = 90f;   // 열릴 때의 각도
    [SerializeField] private float _closeAngle = 0f;    // 닫힐 때의 각도 (보통 0)
    [SerializeField] private float _duration = 1.0f;    // 회전 시간
    [SerializeField] private float _closeDelay = 0.5f;
    private Vector3 _originalRotation;

    private Tween doorTween;

    public override ExitGateType GateType => ExitGateType.Door;



    protected override void Awake()
    {
        base.Awake();
        _originalRotation = _doorPanel.transform.localRotation.eulerAngles;
    }

    private void RotateDoor(bool open)
    {
        // 진행 중인 트윈이 있다면 즉시 중단하고 해당 지점에서 새로 시작
        if (doorTween != null && doorTween.IsActive())
            doorTween.Kill();

        _isOpened = open;

        float targetAngle = _isOpened ? _openAngle : _closeAngle;
        Vector3 targetRotation = _originalRotation;
        targetRotation.y = targetAngle;
        doorTween = _doorPanel.transform.DOLocalRotate(targetRotation, _duration)
            .SetEase(Ease.OutQuad); // 열릴 때는 OutQuad가 더 자연스럽습니다.
    }

    public override void Open()
    {
        // 닫히고 있는 도중에 Open이 들어올 수 있으므로 _isOpened 체크를 제거하거나, 
        // 혹은 목표 각도가 이미 openAngle인지 확인해야 합니다.

        // 이미 완전히 열려있고 닫기 대기 중인 경우 시간만 갱신
        if (_isOpened && (doorTween == null || !doorTween.IsActive()))
        {
            ResetCloseTimer();
            return;
        }

        RotateDoor(true);
        base.Open();
        ResetCloseTimer();
    }

    private void ResetCloseTimer()
    {
        CancelInvoke("Close");
        Invoke("Close", _closeDelay);
    }

    public override void Close()
    {
        // 닫으려고 할 때 이미 닫혀있으면 무시
        if (!_isOpened) return;

        RotateDoor(false);
        // base.Close(); // Close 시에는 base.Close()를 호출하는 것이 맞습니다 (오타 수정됨)
    }
}
