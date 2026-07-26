using UnityEngine;

public sealed class TutorialMiniWavePreparationStep : TutorialStepBase
{
    private bool _checkpointCaptured;

    public override TutorialStepId StepId => TutorialStepId.MiniWavePreparation;



    protected override bool OnEnter()
    {
        _checkpointCaptured = false;
        Context.hud.HideMiniWaveFailure();
        Context.facade.SetPlayerDeathAllowed(false);
        Context.facade.ForceStopProfessorTasks(ProfessorTaskStopReason.StepExit);
        if (!Context.facade.ApplyPolicy(TutorialStagePolicy.Stopped))
            return false;

        StageRuntimeConfig runtime = Context.facade.RuntimeConfig;
        if (!Context.facade.WeaponController.InitializeTutorialLoadout(
            runtime.MiniWaveLoadout,
            Context.facade.Player.gameObject,
            0))
            return false;

        if (!Context.facade.SetChaos(0f)
            || !Context.facade.SetProjectProgress(0f)
            || !Context.facade.SetEscapeCount(
                0,
                Context.courseDefinition.MiniWaveEscapeFailureThreshold)
            || !Context.facade.SetTimer(Context.courseDefinition.MiniWaveDuration))
            return false;

        if (!Context.actors.PrepareMiniWaveRoster(
            Context.courseDefinition.MiniWaveStudentCount,
            runtime.TutorialBehaviorWeightSet))
            return false;

        Context.hud.SetNumericProgress(0, 1);
        Context.input.AdvancePressed += OnAdvance;
        return true;
    }



    private void OnAdvance()
    {
        if (State != TutorialStepState.Active || _checkpointCaptured) return;

        Context.facade.CancelPlayerInteraction();
        Context.facade.ForceStopProfessorTasks(ProfessorTaskStopReason.StepExit);
        if (!Context.checkpoint.CaptureMiniWaveCheckpoint()) return;

        _checkpointCaptured = true;
        Context.hud.SetNumericProgress(1, 1);
        CompleteOnce();
    }



    protected override void OnExit()
    {
        Context.input.AdvancePressed -= OnAdvance;
    }
}
