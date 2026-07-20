using UnityEngine;

public sealed class TutorialPowerRecoveryStep : TutorialStepBase
{
    [Header("Existing main-stage components")]
    [Tooltip("정전 시 기존 F 길게 누르기 복구 동작을 수행할 FuseBox입니다.")]
    [SerializeField] private FuseBox _fuseBox;
    [Tooltip("LabLightSystem 이벤트에 따라 자동 표시·숨김되는 기존 화면 화살표 마커입니다.")]
    [SerializeField] private FuseBoxMarkerUI _fuseBoxMarkerUI;
    [Tooltip("튜토리얼 공용 월드 화살표를 표시할 전기박스 기준 위치입니다.")]
    [SerializeField] private Transform _markerAnchor;

    public override TutorialStepId StepId => TutorialStepId.PowerRecovery;



    protected override bool OnEnter()
    {
        if (_fuseBox == null || _fuseBoxMarkerUI == null || _markerAnchor == null)
        {
            Debug.LogError("3-2 정전 복구 단계의 FuseBox, FuseBoxMarkerUI 또는 Marker Anchor 참조가 누락됐습니다.", this);
            return false;
        }
        if (!_fuseBox.isActiveAndEnabled || !_fuseBoxMarkerUI.isActiveAndEnabled)
        {
            Debug.LogError("3-2 정전 복구 단계의 FuseBox와 FuseBoxMarkerUI는 활성 상태여야 합니다.", this);
            return false;
        }

        Context.facade.SetPlayerDeathAllowed(false);
        if (!Context.facade.ApplyPolicy(TutorialStagePolicy.Stopped))
            return false;

        Context.facade.LabLightsRestored += OnLabLightsRestored;
        Context.hud.SetNumericProgress(0, 1);
        if (!Context.objectiveMarkers.ShowWorldTargetMarker(_fuseBox, _markerAnchor))
        {
            Debug.LogError("3-2 정전 복구 단계의 공용 목표 마커를 전기박스에 표시하지 못했습니다.", this);
            return false;
        }

        return Context.facade.BeginPowerRecoveryTraining();
    }



    private void OnLabLightsRestored()
    {
        if (!Context.facade.AreLightsOn) return;
        Context.objectiveMarkers.HideWorldTargetMarker(_fuseBox);
        Context.hud.SetNumericProgress(1, 1);
        CompleteOnce();
    }



    protected override void OnExit()
    {
        Context.facade.LabLightsRestored -= OnLabLightsRestored;
        Context.objectiveMarkers.HideWorldTargetMarker(_fuseBox);
    }
}
