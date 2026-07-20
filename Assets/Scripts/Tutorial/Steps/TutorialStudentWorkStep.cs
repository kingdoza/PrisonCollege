using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class TutorialStudentWorkStep : TutorialStepBase
{
    private float _continuousWorkTime;
    private BoostReceiver _workTargetBoostReceiver;
    private UnityEvent _originalFrenzyTriggerEvent;
    private bool _workEffectAccepted;
    public override TutorialStepId StepId => TutorialStepId.StudentWork;

    protected override bool OnEnter()
    {
        _continuousWorkTime = 0f;
        _workEffectAccepted = false;
        _workTargetBoostReceiver = null;
        Context.facade.SetPlayerDeathAllowed(false);
        Context.facade.SetProjectProgress(0f);
        Context.facade.ApplyPolicy(new TutorialStagePolicy
        {
            runProject = true,
        });
        Context.actors.SetAllBoostBlocked(false);
        Context.actors.TrainingDestinationReached += OnTrainingDestinationReached;
        if (!StartVendingMachineHighlights()) return false;
        if (!Context.actors.BeginStudentWorkTraining()) return false;

        PostStudent workStudent = Context.actors.StudentWorkStudent;
        _workTargetBoostReceiver = workStudent != null ? workStudent.GetComponent<BoostReceiver>() : null;
        if (_workTargetBoostReceiver == null)
        {
            Debug.LogError("6단계 작업 대상 학생의 BoostReceiver 참조가 없습니다.", workStudent);
            return false;
        }
        if (!BlockFrenzyEffect(_workTargetBoostReceiver)) return false;
        _workTargetBoostReceiver.WorkTriggerEvent.AddListener(OnWorkEffectAccepted);

        TutorialLoadoutEntry workBoost = Context.facade.RuntimeConfig.WorkTrainingBoost;
        if (!Context.facade.WeaponController.AddTutorialWeaponToFirstEmptySlot(workBoost, out _))
            return false;
        Context.hud.SetTimedProgress(0f, Context.courseDefinition.WorkConfirmationSeconds);
        return true;
    }

    private bool StartVendingMachineHighlights()
    {
        List<Transform> roots = new();
        foreach (TutorialRechargerBinding binding in Context.facade.RechargerBindings)
        {
            if (binding.enabled && binding.recharger is BoostRecharger)
                roots.Add(binding.recharger.transform);
        }

        if (roots.Count == 0)
        {
            Debug.LogError("6단계에 사용할 활성 BoostRecharger binding이 TutorialStageFacade에 없습니다.", this);
            return false;
        }
        return Context.highlighter.StartHighlights(roots);
    }

    private void OnTrainingDestinationReached(PostStudent student)
    {
        if (!_workEffectAccepted && student == Context.actors.StudentWorkStudent)
            Context.objectiveMarkers.ShowStudentMarker(student);
    }

    private void OnWorkEffectAccepted()
    {
        if (_workEffectAccepted) return;
        _workEffectAccepted = true;
        Context.objectiveMarkers.HideStudentMarker(Context.actors.StudentWorkStudent);
    }

    private bool BlockFrenzyEffect(BoostReceiver receiver)
    {
        if (receiver.FrenzyTriggerEvent == null)
        {
            Debug.LogError("6단계 작업 대상 학생의 FrenzyTriggerEvent가 없습니다.", receiver);
            return false;
        }

        _originalFrenzyTriggerEvent = receiver.FrenzyTriggerEvent;
        receiver.FrenzyTriggerEvent = new UnityEvent();
        return true;
    }

    private void RestoreFrenzyEffect()
    {
        if (_workTargetBoostReceiver != null && _originalFrenzyTriggerEvent != null)
            _workTargetBoostReceiver.FrenzyTriggerEvent = _originalFrenzyTriggerEvent;
        _originalFrenzyTriggerEvent = null;
    }

    protected override void OnTick()
    {
        PostStudent student = Context.actors.StudentWorkStudent;
        if (student != null && student.IsWorking)
            _continuousWorkTime += Time.deltaTime;
        else
            _continuousWorkTime = 0f;

        float target = Context.courseDefinition.WorkConfirmationSeconds;
        Context.hud.SetTimedProgress(Mathf.Min(_continuousWorkTime, target), target);
        if (_continuousWorkTime >= target)
            CompleteOnce();
    }

    protected override void OnCompleting() => Context.actors.EndStudentWorkTraining();

    protected override void OnExit()
    {
        Context.actors.TrainingDestinationReached -= OnTrainingDestinationReached;
        if (_workTargetBoostReceiver != null)
            _workTargetBoostReceiver.WorkTriggerEvent.RemoveListener(OnWorkEffectAccepted);
        RestoreFrenzyEffect();
        _workTargetBoostReceiver = null;
        if (_continuousWorkTime < Context.courseDefinition.WorkConfirmationSeconds)
            Context.actors.EndStudentWorkTraining();
    }
}
