using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class TutorialMicrowaveManagementStep : TutorialStepBase
{
    [Header("Hazard microwave selection")]
    [Tooltip("끄면 두 전자레인지 중 위험 음식 전자레인지를 진입할 때마다 무작위로 정합니다.")]
    [SerializeField] private bool _useFixedHazardMicrowave;
    [Tooltip("Use Fixed Hazard Microwave가 켜졌을 때 위험 음식을 돌릴 전자레인지입니다. Facade Microwaves에 등록된 대상이어야 합니다.")]
    [SerializeField] private Microwave _fixedHazardMicrowave;

    [Header("Hazard microwave guidance")]
    [Tooltip("TutorialStageFacade에 등록된 두 Microwave와 각 고정 Marker Anchor를 정확히 한 번씩 연결합니다.")]
    [SerializeField] private TutorialMicrowaveMarkerBinding[] _microwaveMarkerBindings =
        Array.Empty<TutorialMicrowaveMarkerBinding>();

    private readonly Dictionary<Microwave, Transform> _markerAnchorByMicrowave = new();
    private Microwave _firstMicrowave;
    private Microwave _secondMicrowave;
    private Microwave _hazardMicrowave;
    private bool _actorDisplayStarted;
    private bool _microwaveSetupAttempted;
    private bool _cleanedUp;

    public override TutorialStepId StepId => TutorialStepId.MicrowaveManagement;



    protected override bool OnEnter()
    {
        _actorDisplayStarted = false;
        _microwaveSetupAttempted = false;
        _cleanedUp = false;
        _hazardMicrowave = null;

        if (!Context.actors.TryGetMicrowaveFoodCatalog(out FoodInfo[] foodSamples)
            || !TryValidateAndCollectFoods(
            foodSamples,
            out List<GameObject> allFoods,
            out List<GameObject> normalFoods,
            out List<GameObject> hazardFoods))
            return false;

        IReadOnlyList<Microwave> microwaves = Context.facade.Microwaves;
        if (microwaves == null
            || microwaves.Count != 2
            || microwaves[0] == null
            || microwaves[1] == null
            || microwaves[0] == microwaves[1])
        {
            Debug.LogError("3-3에는 서로 다른 전자레인지 두 개를 TutorialStageFacade Microwaves에 연결해야 합니다.", this);
            return false;
        }
        if (!BuildMicrowaveMarkerBindings(microwaves))
            return false;
        _firstMicrowave = microwaves[0];
        _secondMicrowave = microwaves[1];
        if (!TrySelectMicrowaves(out Microwave normalMicrowave))
            return false;

        Context.facade.SetPlayerDeathAllowed(false);
        if (!Context.facade.ApplyPolicy(new TutorialStagePolicy
        {
            allowNormalFoodRemovedChaos = true,
        }))
            return false;

        Context.facade.MicrowaveFoodRemoved += OnMicrowaveFoodRemoved;
        Context.hud.SetNumericProgress(0, 1);

        if (!Context.actors.BeginMicrowaveFoodDisplay(allFoods))
            return false;
        _actorDisplayStarted = true;

        GameObject normalFood = normalFoods[UnityEngine.Random.Range(0, normalFoods.Count)];
        GameObject hazardFood = hazardFoods[UnityEngine.Random.Range(0, hazardFoods.Count)];

        _microwaveSetupAttempted = true;
        if (!Context.facade.BeginMicrowaveManagementTraining(
            normalMicrowave,
            normalFood,
            _hazardMicrowave,
            hazardFood))
            return false;

        if (!Context.objectiveMarkers.ShowWorldTargetMarker(
            _hazardMicrowave,
            _markerAnchorByMicrowave[_hazardMicrowave]))
        {
            Debug.LogError("3-3 위험 음식 전자레인지에 공용 목표 마커를 표시하지 못했습니다.", _hazardMicrowave);
            return false;
        }
        return true;
    }



    private bool TrySelectMicrowaves(out Microwave normalMicrowave)
    {
        normalMicrowave = null;
        if (_useFixedHazardMicrowave)
        {
            if (_fixedHazardMicrowave != _firstMicrowave
                && _fixedHazardMicrowave != _secondMicrowave)
            {
                Debug.LogError("3-3 Fixed Hazard Microwave는 TutorialStageFacade에 등록된 두 전자레인지 중 하나여야 합니다.", this);
                return false;
            }
            _hazardMicrowave = _fixedHazardMicrowave;
        }
        else
        {
            _hazardMicrowave = UnityEngine.Random.value < 0.5f
                ? _firstMicrowave
                : _secondMicrowave;
        }

        normalMicrowave = _hazardMicrowave == _firstMicrowave
            ? _secondMicrowave
            : _firstMicrowave;
        return true;
    }



    private bool BuildMicrowaveMarkerBindings(IReadOnlyList<Microwave> microwaves)
    {
        _markerAnchorByMicrowave.Clear();
        if (_microwaveMarkerBindings == null
            || _microwaveMarkerBindings.Length != microwaves.Count)
        {
            Debug.LogError("3-3 Microwave 수와 Microwave Marker Binding 수가 일치해야 합니다.", this);
            return false;
        }

        HashSet<Microwave> registeredMicrowaves = new(microwaves);
        foreach (TutorialMicrowaveMarkerBinding binding in _microwaveMarkerBindings)
        {
            if (binding.microwave == null
                || binding.markerAnchor == null
                || !registeredMicrowaves.Contains(binding.microwave)
                || !_markerAnchorByMicrowave.TryAdd(binding.microwave, binding.markerAnchor))
            {
                Debug.LogError("3-3 Microwave Marker Binding에 누락·중복되거나 Facade에 등록되지 않은 Microwave가 있습니다.", this);
                _markerAnchorByMicrowave.Clear();
                return false;
            }
        }
        return true;
    }



    private bool TryValidateAndCollectFoods(
        IReadOnlyList<FoodInfo> foodSamples,
        out List<GameObject> allFoods,
        out List<GameObject> normalFoods,
        out List<GameObject> hazardFoods)
    {
        allFoods = new List<GameObject>();
        normalFoods = new List<GameObject>();
        hazardFoods = new List<GameObject>();
        if (foodSamples == null || foodSamples.Count == 0)
        {
            Debug.LogError("3-3 StudentDB index 0 PlateAttacher 음식 목록이 비어 있습니다.", this);
            return false;
        }

        HashSet<GameObject> uniqueFoods = new();
        for (int i = 0; i < foodSamples.Count; i++)
        {
            FoodInfo sample = foodSamples[i];
            if (sample == null || sample.gameObj == null || !uniqueFoods.Add(sample.gameObj))
            {
                Debug.LogError($"3-3 PlateAttacher 음식 목록 [{i}]가 null이거나 중복됐습니다.", this);
                return false;
            }

            allFoods.Add(sample.gameObj);
            (sample.isCauseFire ? hazardFoods : normalFoods).Add(sample.gameObj);
        }

        if (normalFoods.Count == 0 || hazardFoods.Count == 0)
        {
            Debug.LogError("3-3 PlateAttacher에는 정상 음식과 위험 음식이 각각 하나 이상 필요합니다.", this);
            return false;
        }
        return true;
    }



    private void OnMicrowaveFoodRemoved(Microwave microwave, bool wasHazard)
    {
        if (!wasHazard || (microwave != _firstMicrowave && microwave != _secondMicrowave))
            return;

        Context.objectiveMarkers.HideWorldTargetMarker(microwave);
        Context.hud.SetNumericProgress(1, 1);
        CompleteOnce();
    }



    protected override void OnCompleting()
    {
        CleanupTraining();
    }



    protected override void OnExit()
    {
        CleanupTraining();
    }



    private void CleanupTraining()
    {
        if (_cleanedUp) return;
        _cleanedUp = true;
        Context.facade.MicrowaveFoodRemoved -= OnMicrowaveFoodRemoved;
        if (_hazardMicrowave != null)
            Context.objectiveMarkers.HideWorldTargetMarker(_hazardMicrowave);
        if (_microwaveSetupAttempted)
            Context.facade.EndMicrowaveManagementTraining(_firstMicrowave, _secondMicrowave);
        if (_actorDisplayStarted)
            Context.actors.EndMicrowaveFoodDisplay();
        _actorDisplayStarted = false;
        _microwaveSetupAttempted = false;
        _hazardMicrowave = null;
        _markerAnchorByMicrowave.Clear();
    }
}
