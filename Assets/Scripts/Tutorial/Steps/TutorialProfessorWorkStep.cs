using System;
using System.Collections.Generic;
using UnityEngine;

public class TutorialProfessorWorkStep : TutorialStepBase
{
    [Header("Professor computer guidance")]
    [Tooltip("TutorialStageFacade에 등록된 각 ProfessorTask와 고정 Marker Anchor를 정확히 한 번씩 연결합니다.")]
    [SerializeField] private TutorialProfessorTaskMarkerBinding[] _taskMarkerBindings =
        Array.Empty<TutorialProfessorTaskMarkerBinding>();

    private readonly Dictionary<ProfessorTask, Transform> _markerAnchorByTask = new();

    public override TutorialStepId StepId => TutorialStepId.ProfessorWork;

    protected override bool OnEnter()
    {
        if (!BuildTaskBindings()) return false;
        Context.facade.SetPlayerDeathAllowed(false);
        Context.actors.SetAllBoostBlocked(true);
        Context.facade.ForceStopProfessorTasks(ProfessorTaskStopReason.StepExit);
        Context.facade.SetProjectProgress(0f);
        Context.facade.ApplyPolicy(new TutorialStagePolicy
        {
            runProject = true,
            allowProfessorTask = true,
        });
        Context.facade.ProjectCompleted += OnProjectCompleted;
        Context.facade.ProfessorTaskStarted += OnProfessorTaskStarted;
        Context.facade.ProfessorTaskStopped += OnProfessorTaskStopped;
        Context.hud.SetNumericProgress(0, 1);
        if (!Context.highlighter.StartHighlights(GetProfessorTaskRoots()))
            return false;
        return RefreshProfessorTaskMarkers();
    }

    private bool BuildTaskBindings()
    {
        _markerAnchorByTask.Clear();
        IReadOnlyList<ProfessorTask> tasks = Context.facade.ProfessorTasks;
        if (tasks == null
            || tasks.Count == 0
            || _taskMarkerBindings == null
            || _taskMarkerBindings.Length != tasks.Count)
        {
            Debug.LogError("7단계 ProfessorTask 수와 Task Marker Binding 수가 일치해야 합니다.", this);
            return false;
        }

        HashSet<ProfessorTask> registeredTasks = new(tasks);
        foreach (TutorialProfessorTaskMarkerBinding binding in _taskMarkerBindings)
        {
            if (binding.task == null
                || binding.markerAnchor == null
                || !registeredTasks.Contains(binding.task)
                || !_markerAnchorByTask.TryAdd(binding.task, binding.markerAnchor))
            {
                Debug.LogError("7단계 Task Marker Binding에 누락·중복되거나 Facade에 등록되지 않은 ProfessorTask가 있습니다.", this);
                _markerAnchorByTask.Clear();
                return false;
            }
        }
        return true;
    }

    private List<Transform> GetProfessorTaskRoots()
    {
        List<Transform> roots = new(_markerAnchorByTask.Count);
        foreach (ProfessorTask task in _markerAnchorByTask.Keys)
            roots.Add(task.transform);
        return roots;
    }

    private bool RefreshProfessorTaskMarkers()
    {
        if (Context.facade.IsAnyProfessorTaskActive())
        {
            HideProfessorTaskMarkers();
            return true;
        }

        foreach (KeyValuePair<ProfessorTask, Transform> pair in _markerAnchorByTask)
        {
            if (!Context.objectiveMarkers.ShowWorldTargetMarker(pair.Key, pair.Value))
            {
                Debug.LogError($"[{pair.Key.name}] 교수 작업 목표 마커를 표시하지 못했습니다.", pair.Key);
                return false;
            }
        }
        return true;
    }

    private void HideProfessorTaskMarkers()
    {
        foreach (ProfessorTask task in _markerAnchorByTask.Keys)
            Context.objectiveMarkers.HideWorldTargetMarker(task);
    }

    private void OnProfessorTaskStarted(ProfessorTask task)
    {
        HideProfessorTaskMarkers();
    }

    private void OnProfessorTaskStopped(ProfessorTask task, ProfessorTaskStopReason reason)
    {
        if (State == TutorialStepState.Active)
            RefreshProfessorTaskMarkers();
    }

    private void OnProjectCompleted(int completionId, ProjectContributor contributors)
    {
        if ((contributors & ProjectContributor.Professor) == 0
            || (contributors & ProjectContributor.Student) != 0
            || !Context.facade.IsProfessorWorking)
            return;
        Context.hud.SetNumericProgress(1, 1);
        HideProfessorTaskMarkers();
        CompleteOnce();
    }

    protected override void OnExit()
    {
        Context.facade.ProjectCompleted -= OnProjectCompleted;
        Context.facade.ProfessorTaskStarted -= OnProfessorTaskStarted;
        Context.facade.ProfessorTaskStopped -= OnProfessorTaskStopped;
        HideProfessorTaskMarkers();
        _markerAnchorByTask.Clear();
        Context.facade.ForceStopProfessorTasks(ProfessorTaskStopReason.StepExit);
    }
}
