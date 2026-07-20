using UnityEngine;

public class TutorialCourseSummaryStep : TutorialStepBase
{
    public override TutorialStepId StepId => TutorialStepId.CourseSummary;

    protected override bool OnEnter()
    {
        Context.facade.StopAllStageSimulation();
        Context.facade.SetPlayerDeathAllowed(false);
        Context.actors.StopAllActors();
        Context.hud.ShowCourseSummaryButtons(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        GameManager.Instance.MarkTutorialCompleted();
        return true;
    }

    protected override void OnExit() => Context.hud.ShowCourseSummaryButtons(false);
}
