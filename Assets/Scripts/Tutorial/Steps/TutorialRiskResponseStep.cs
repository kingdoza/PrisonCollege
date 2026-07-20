using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TutorialRiskResponseStep : TutorialStepBase
{
    private readonly HashSet<PostStudent> _resolvedHazards = new();
    private readonly HashSet<PostStudent> _startedHazards = new();
    private readonly Dictionary<PostStudent, TutorialRiskBehaviorInfoId> _behaviorInfoByStudent = new();
    public override TutorialStepId StepId => TutorialStepId.RiskResponse;

    protected override bool OnEnter()
    {
        _resolvedHazards.Clear();
        _startedHazards.Clear();
        _behaviorInfoByStudent.Clear();
        Context.facade.SetPlayerDeathAllowed(false);
        if (!Context.facade.ApplyPolicy(new TutorialStagePolicy
        {
            allowInnocentDownChaos = true,
        }))
            return false;
        Context.facade.StudentDowned += OnStudentDowned;
        Context.actors.TrainingDestinationReached += OnTrainingDestinationReached;
        Context.studentFocus.FocusedStudentChanged += OnFocusedStudentChanged;
        Context.riskInfoBubble.ActivateForStep();
        Context.hud.SetNumericProgress(0, 4);
        if (!StartWeaponRechargerHighlights()) return false;
        if (!Context.actors.BeginRiskTraining()) return false;
        foreach (PostStudent student in Context.actors.RiskHazardStudents)
            student.ScriptedBehaviorTelemetryEvent += OnBehaviorTelemetry;
        return true;
    }

    private bool StartWeaponRechargerHighlights()
    {
        List<Transform> roots = new();
        foreach (TutorialRechargerBinding binding in Context.facade.RechargerBindings)
        {
            if (binding.enabled && binding.recharger is DamageRecharger)
                roots.Add(binding.recharger.transform);
        }

        if (roots.Count == 0)
        {
            Debug.LogError("3단계에 사용할 활성 DamageRecharger binding이 TutorialStageFacade에 없습니다.", this);
            return false;
        }
        return Context.highlighter.StartHighlights(roots);
    }

    private void OnTrainingDestinationReached(PostStudent student)
    {
        if (Context.actors.RiskHazardStudents.Contains(student))
            Context.objectiveMarkers.ShowStudentMarker(student);
    }

    private void OnBehaviorTelemetry(
        PostStudent student,
        ScriptedBehaviorRequest request,
        TutorialBehaviorTelemetry telemetry)
    {
        if (telemetry == TutorialBehaviorTelemetry.ActionStarted
            && Context.actors.RiskHazardStudents.Contains(student))
        {
            _startedHazards.Add(student);
            if (!TryGetBehaviorInfoId(request, out TutorialRiskBehaviorInfoId behaviorId))
            {
                Debug.LogError($"{request.behavior} scripted 행동을 3단계 위험 행동 추가 정보 ID로 변환할 수 없습니다.", student);
                return;
            }

            _behaviorInfoByStudent[student] = behaviorId;
            if (Context.studentFocus.CurrentStudent == student)
                ShowFocusedStudentInfo(student);
        }
    }

    private void OnFocusedStudentChanged(PostStudent previous, PostStudent current)
    {
        ShowFocusedStudentInfo(current);
    }

    private void ShowFocusedStudentInfo(PostStudent student)
    {
        if (Context.riskInfoBubble == null) return;
        if (student == null
            || !_behaviorInfoByStudent.TryGetValue(student, out TutorialRiskBehaviorInfoId behaviorId)
            || !Context.courseDefinition.TryGetRiskBehaviorContent(behaviorId, out TutorialRiskBehaviorContent content)
            || !Context.actors.TryGetBubbleAnchor(student, out Transform anchorBone))
        {
            Context.riskInfoBubble.HideTemporary();
            return;
        }

        Context.riskInfoBubble.Show(student, anchorBone, content);
    }

    private static bool TryGetBehaviorInfoId(
        ScriptedBehaviorRequest request,
        out TutorialRiskBehaviorInfoId behaviorId)
    {
        switch (request.behavior)
        {
            case BehaviorType.Escape:
                behaviorId = TutorialRiskBehaviorInfoId.ExitAttack;
                return true;
            case BehaviorType.Hack:
                behaviorId = TutorialRiskBehaviorInfoId.Hacking;
                return true;
            case BehaviorType.Sing when request.overrideSongQuality && request.useBadSong:
                behaviorId = TutorialRiskBehaviorInfoId.BadSinging;
                return true;
            case BehaviorType.Smoke:
                behaviorId = TutorialRiskBehaviorInfoId.Smoking;
                return true;
            default:
                behaviorId = default;
                return false;
        }
    }

    private void OnStudentDowned(PostStudent student, HitInfo hitInfo, bool wasHazardous)
    {
        if (!Context.actors.RiskHazardStudents.Contains(student) || !_startedHazards.Contains(student)) return;
        if (!_resolvedHazards.Add(student)) return;
        Context.objectiveMarkers.HideStudentMarker(student);
        Context.hud.SetNumericProgress(_resolvedHazards.Count, 4);
        if (_resolvedHazards.Count == 4)
            CompleteOnce();
    }

    protected override void OnCompleting()
    {
        if (Context.riskInfoBubble != null)
            Context.riskInfoBubble.DeactivateForStep();
        Context.actors.EndRiskTraining();
    }

    protected override void OnExit()
    {
        Context.facade.StudentDowned -= OnStudentDowned;
        Context.actors.TrainingDestinationReached -= OnTrainingDestinationReached;
        Context.studentFocus.FocusedStudentChanged -= OnFocusedStudentChanged;
        if (Context.riskInfoBubble != null)
            Context.riskInfoBubble.DeactivateForStep();
        foreach (PostStudent student in Context.actors.RiskHazardStudents)
            student.ScriptedBehaviorTelemetryEvent -= OnBehaviorTelemetry;
        if (_resolvedHazards.Count < 4)
            Context.actors.EndRiskTraining();
    }
}
