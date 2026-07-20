using UnityEngine;

public class TutorialMovementStep : TutorialStepBase
{
    public override TutorialStepId StepId => TutorialStepId.Movement;

    protected override bool OnEnter()
    {
        TutorialMovementMarker marker = Context?.movementMarker;
        if (marker == null)
        {
            Debug.LogError("1단계 TutorialMovementMarker 참조가 없거나 파괴됐습니다.", this);
            return false;
        }

        Context.facade.SetPlayerDeathAllowed(false);
        Context.facade.ApplyPolicy(TutorialStagePolicy.Stopped);
        Context.actors.SetAllLoyalty();
        Context.hud.SetNumericProgress(0, 1);
        marker.Reached += OnReached;
        return marker.TryActivateMarker();
    }

    private void OnReached()
    {
        Context.hud.SetNumericProgress(1, 1);
        CompleteOnce();
    }

    protected override void OnExit()
    {
        TutorialMovementMarker marker = Context?.movementMarker;
        if (marker == null) return;
        marker.Reached -= OnReached;
        marker.DeactivateMarker();
    }
}
