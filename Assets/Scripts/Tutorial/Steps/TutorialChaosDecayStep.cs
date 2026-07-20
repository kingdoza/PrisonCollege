public class TutorialChaosDecayStep : TutorialStepBase
{
    public override TutorialStepId StepId => TutorialStepId.ChaosDecay;

    protected override bool OnEnter()
    {
        Context.facade.SetPlayerDeathAllowed(false);
        Context.facade.ApplyPolicy(new TutorialStagePolicy
        {
            allowChaosDecay = true,
        });
        Context.facade.ChaosChanged += OnChaosChanged;
        Context.input.AdvancePressed += OnAdvance;
        Context.hud.ShowChaosDecayHighlight(false);
        return true;
    }

    protected override void OnActivated()
    {
        if (Context.facade.Chaos <= 0f)
            CompleteOnce();
    }

    private void OnChaosChanged(ChaosChangedData data)
    {
        if (data.reason == ChaosChangeReason.NaturalDecay && data.delta < 0f)
        {
            Context.hud.NotifyChaosRendered();
            Context.hud.ShowChaosDecayHighlight(true);
        }
        if (data.current <= 0f)
            CompleteOnce();
    }

    private void OnAdvance() => CompleteOnce();

    protected override void OnExit()
    {
        Context.facade.ChaosChanged -= OnChaosChanged;
        Context.input.AdvancePressed -= OnAdvance;
        Context.hud.ShowChaosDecayHighlight(false);
    }
}
