public class TutorialIntroStep : TutorialStepBase
{
    public override TutorialStepId StepId => TutorialStepId.Intro;

    protected override bool OnEnter()
    {
        Context.facade.SetPlayerDeathAllowed(false);
        Context.facade.ApplyPolicy(TutorialStagePolicy.Stopped);
        Context.actors.SetAllLoyalty();
        if (!Context.playerInputGate.Acquire(TutorialPlayerInputLockReason.Intro))
            return false;
        Context.hud.SetNumericProgress(0, 1);
        Context.input.AdvancePressed += OnAdvance;
        return true;
    }

    private void OnAdvance()
    {
        Context.hud.SetNumericProgress(1, 1);
        CompleteOnce();
    }

    protected override void OnExit()
    {
        Context.input.AdvancePressed -= OnAdvance;
        Context.playerInputGate.Release(TutorialPlayerInputLockReason.Intro);
    }
}
