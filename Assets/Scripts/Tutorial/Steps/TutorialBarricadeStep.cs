using System;
using System.Collections.Generic;
using UnityEngine;

public class TutorialBarricadeStep : TutorialStepBase
{
    [Tooltip("TutorialStageFacade에 등록된 각 ExitGate와 고정 Marker Anchor를 정확히 한 번씩 연결합니다.")]
    [SerializeField] private TutorialGateMarkerBinding[] _gateMarkerBindings = Array.Empty<TutorialGateMarkerBinding>();
    private readonly Dictionary<ExitGate, Transform> _markerAnchorByGate = new();

    public override TutorialStepId StepId => TutorialStepId.Barricades;

    protected override bool OnEnter()
    {
        if (!BuildMarkerBindings()) return false;
        Context.facade.SetPlayerDeathAllowed(false);
        Context.facade.ApplyPolicy(TutorialStagePolicy.Stopped);
        Context.actors.SetAllStandby();
        Context.facade.BarricadeStateChanged += OnBarricadeStateChanged;
        if (!Context.highlighter.StartBarricadeHighlights(Context.facade.ExitGates))
            return false;
        return RefreshGateMarkers();
    }

    protected override void OnActivated() => RefreshProgress();

    private void OnBarricadeStateChanged(ExitGate gate, bool isPlaced)
    {
        if (!_markerAnchorByGate.TryGetValue(gate, out Transform anchor))
        {
            Debug.LogError("바리케이드 상태가 변경된 ExitGate의 목표 마커 binding이 없습니다.", gate);
            return;
        }

        if (isPlaced)
        {
            Context.objectiveMarkers.HideWorldTargetMarker(gate);
        }
        else if (!Context.objectiveMarkers.ShowWorldTargetMarker(gate, anchor))
        {
            Debug.LogError($"[{gate.name}] 탈출구 목표 마커를 다시 표시하지 못했습니다.", gate);
        }
        RefreshProgress();
    }

    private void RefreshProgress()
    {
        int placed = 0;
        foreach (ExitGate gate in Context.facade.ExitGates)
            if (gate != null && gate.IsBarricadePlaced) placed++;
        Context.hud.SetNumericProgress(placed, Context.facade.ExitGates.Count);
        if (placed == Context.facade.ExitGates.Count)
            CompleteOnce();
    }

    private bool BuildMarkerBindings()
    {
        _markerAnchorByGate.Clear();
        IReadOnlyList<ExitGate> gates = Context.facade.ExitGates;
        if (_gateMarkerBindings == null || _gateMarkerBindings.Length != gates.Count)
        {
            Debug.LogError("2단계 ExitGate 수와 Gate Marker Binding 수가 일치해야 합니다.", this);
            return false;
        }

        HashSet<ExitGate> registeredGates = new(gates);
        foreach (TutorialGateMarkerBinding binding in _gateMarkerBindings)
        {
            if (binding.gate == null
                || binding.markerAnchor == null
                || !registeredGates.Contains(binding.gate)
                || !_markerAnchorByGate.TryAdd(binding.gate, binding.markerAnchor))
            {
                Debug.LogError("2단계 Gate Marker Binding에 누락·중복되거나 Facade에 등록되지 않은 ExitGate가 있습니다.", this);
                _markerAnchorByGate.Clear();
                return false;
            }
        }
        return true;
    }



    private bool RefreshGateMarkers()
    {
        foreach (KeyValuePair<ExitGate, Transform> pair in _markerAnchorByGate)
        {
            if (pair.Key.IsBarricadePlaced)
            {
                Context.objectiveMarkers.HideWorldTargetMarker(pair.Key);
                continue;
            }
            if (!Context.objectiveMarkers.ShowWorldTargetMarker(pair.Key, pair.Value))
            {
                Debug.LogError($"[{pair.Key.name}] 탈출구 목표 마커를 표시하지 못했습니다.", pair.Key);
                return false;
            }
        }
        return true;
    }



    protected override void OnExit()
    {
        Context.facade.BarricadeStateChanged -= OnBarricadeStateChanged;
        foreach (ExitGate gate in _markerAnchorByGate.Keys)
            Context.objectiveMarkers.HideWorldTargetMarker(gate);
        _markerAnchorByGate.Clear();
    }
}
