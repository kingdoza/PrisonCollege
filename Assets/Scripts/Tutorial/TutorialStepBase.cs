using UnityEngine;

public abstract class TutorialStepBase : MonoBehaviour
{
    private TutorialDirector _director;
    protected TutorialStepContext Context { get; private set; }
    public TutorialStepState State { get; private set; } = TutorialStepState.Inactive;
    public abstract TutorialStepId StepId { get; }



    public void InitializeStep(TutorialDirector director, TutorialStepContext context)
    {
        _director = director;
        Context = context;
    }



    public bool EnterStep()
    {
        if (State != TutorialStepState.Inactive) return false;
        State = TutorialStepState.Enter;
        if (!Context.courseDefinition.TryGetContent(StepId, out TutorialStepContent content))
        {
            Debug.LogError($"{StepId} 단계 문구가 TutorialCourseDefinition에 없습니다.", this);
            return false;
        }
        Context.hud.ShowStep(content);
        if (!OnEnter())
        {
            Debug.LogError($"{StepId} 단계 Enter에 실패했습니다.", this);
            OnExit();
            StopAllCoroutines();
            Context.objectiveMarkers?.ClearAll();
            Context.highlighter.ClearAllHighlights();
            Context.hud.HideStep();
            State = TutorialStepState.Inactive;
            return false;
        }
        State = TutorialStepState.Active;
        OnActivated();
        return true;
    }



    public void TickStep()
    {
        if (State == TutorialStepState.Active)
            OnTick();
    }



    public bool CompleteFromDirector() => CompleteOnce();



    protected bool CompleteOnce()
    {
        if (State != TutorialStepState.Active) return false;
        State = TutorialStepState.Complete;
        OnCompleting();
        _director.OnStepCompleted(this);
        return true;
    }



    public void ExitStep()
    {
        if (State == TutorialStepState.Inactive) return;
        State = TutorialStepState.Exit;
        OnExit();
        StopAllCoroutines();
        Context.objectiveMarkers?.ClearAll();
        Context.highlighter.ClearAllHighlights();
        State = TutorialStepState.Inactive;
    }

    protected abstract bool OnEnter();
    protected virtual void OnActivated() { }
    protected virtual void OnTick() { }
    protected virtual void OnCompleting() { }
    protected abstract void OnExit();
}



public sealed class TutorialStepContext
{
    public TutorialCourseDefinition courseDefinition;
    public TutorialStageFacade facade;
    public TutorialActorDirector actors;
    public TutorialCheckpointService checkpoint;
    public TutorialHUDPresenter hud;
    public TutorialHighlighter highlighter;
    public TutorialInput input;
    public TutorialPlayerInputGate playerInputGate;
    public TutorialMovementMarker movementMarker;
    public TutorialObjectiveMarkerPresenter objectiveMarkers;
    public TutorialStudentFocusSource studentFocus;
    public TutorialRiskInfoBubblePresenter riskInfoBubble;
}
