using System.Collections;

public class TutorialInnocentStudentStep : TutorialStepBase
{
    private bool _targetDowned;
    private bool _chaosApplied;
    private bool _completionCheckStarted;
    private int _hudRevisionAtEnter;
    public override TutorialStepId StepId => TutorialStepId.InnocentStudent;

    protected override bool OnEnter()
    {
        _targetDowned = false;
        _chaosApplied = false;
        _completionCheckStarted = false;
        Context.facade.SetPlayerDeathAllowed(false);
        Context.facade.ApplyPolicy(new TutorialStagePolicy
        {
            allowInnocentDownChaos = true,
        });
        Context.facade.SetChaos(0f);
        Context.facade.StudentDowned += OnStudentDowned;
        Context.facade.ChaosChanged += OnChaosChanged;
        Context.actors.TrainingDestinationReached += OnTrainingDestinationReached;
        Context.hud.SetNumericProgress(0, 1);
        _hudRevisionAtEnter = Context.hud.StateRevision;
        return Context.actors.BeginInnocentStudentTraining();
    }

    private void OnTrainingDestinationReached(PostStudent student)
    {
        if (student == Context.actors.InnocentStudent)
            Context.objectiveMarkers.ShowStudentMarker(student);
    }

    private void OnStudentDowned(PostStudent student, HitInfo hitInfo, bool wasHazardous)
    {
        if (student != Context.actors.InnocentStudent
            || hitInfo.attacker != Context.facade.Player.gameObject)
            return;
        _targetDowned = true;
        Context.objectiveMarkers.HideStudentMarker(student);
        TryScheduleCompletion();
    }

    private void OnChaosChanged(ChaosChangedData data)
    {
        if (data.reason != ChaosChangeReason.InnocentDown || data.delta <= 0f) return;
        _chaosApplied = true;
        Context.hud.NotifyChaosRendered();
        TryScheduleCompletion();
    }

    private void TryScheduleCompletion()
    {
        if (!_targetDowned || !_chaosApplied || _completionCheckStarted) return;
        _completionCheckStarted = true;
        StartCoroutine(CompleteAfterHudRender());
    }

    private IEnumerator CompleteAfterHudRender()
    {
        yield return null;
        if (Context.hud.StateRevision > _hudRevisionAtEnter)
        {
            Context.hud.SetNumericProgress(1, 1);
            CompleteOnce();
        }
    }

    protected override void OnCompleting() => Context.actors.EndInnocentStudentTraining();

    protected override void OnExit()
    {
        Context.facade.StudentDowned -= OnStudentDowned;
        Context.facade.ChaosChanged -= OnChaosChanged;
        Context.actors.TrainingDestinationReached -= OnTrainingDestinationReached;
        if (!_targetDowned || !_chaosApplied)
            Context.actors.EndInnocentStudentTraining();
    }
}
