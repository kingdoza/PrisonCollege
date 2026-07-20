using System;
using System.Collections.Generic;
using UnityEngine;

public enum StageRuntimeMode
{
    Normal,
    Tutorial,
}

public enum StageFinishPolicy
{
    NormalStageFlow,
    ReportOnly,
}

public enum TutorialStepId
{
    Intro = 0,
    Movement = 1,
    Barricades = 2,
    RiskResponse = 3,
    InnocentStudent = 4,
    ChaosDecay = 5,
    StudentWork = 6,
    ProfessorWork = 7,
    MiniWave = 8,
    CourseSummary = 9,
    MiniWavePreparation = 10,
    PowerRecovery = 11,
    MicrowaveManagement = 12,
}

public enum TutorialStudentMode
{
    Loyalty,
    Standby,
    TrainingTransit,
    Training,
    ReturnTransit,
    Cheer,
    MiniWave,
}

public enum TutorialStepState
{
    Inactive,
    Enter,
    Active,
    Complete,
    Exit,
}

public enum ChaosChangeReason
{
    ContinuousHazard,
    InnocentDown,
    Escape,
    Gunshot,
    NormalFoodRemoved,
    NaturalDecay,
    Reset,
}

[Flags]
public enum ProjectContributor
{
    None = 0,
    Student = 1 << 0,
    Professor = 1 << 1,
}

public enum StageFinishResult
{
    TimerExpired,
    EscapeFailure,
}

public enum ProfessorTaskStopReason
{
    Movement,
    InteractToggle,
    Death,
    StepExit,
}

public enum TutorialBehaviorTelemetry
{
    Selected,
    ActionStarted,
    Interrupted,
    Completed,
}

public enum TutorialRiskBehaviorInfoId
{
    ExitAttack,
    Hacking,
    BadSinging,
    Smoking,
}

[Serializable]
public struct TutorialStagePolicy
{
    public bool runTimer;
    public bool runProject;
    public bool allowContinuousChaosSources;
    public bool allowInnocentDownChaos;
    public bool allowEscapeChaos;
    public bool allowGunshotChaos;
    public bool allowNormalFoodRemovedChaos;
    public bool allowChaosDecay;
    public bool evaluateEscapeFailure;
    public bool allowProfessorTask;
    public bool showFullStageHud;

    public static TutorialStagePolicy Stopped => default;
}

[Serializable]
public struct TutorialLoadoutEntry
{
    [Tooltip("기존 WeaponItem asset을 연결합니다. 런타임에는 WeaponItem.inStageIndex로 WeaponController의 실제 preset 인스턴스를 해석합니다.")]
    public WeaponItem weaponItem;

    [Tooltip("의도적인 빈 슬롯입니다. 켜면 weaponItem과 탄약 설정을 무시하고 런타임 EmptyWeapon preset을 사용합니다.")]
    public bool isEmptySlot;

    [Tooltip("켜면 ammunition을 무시하고 런타임 무기의 Stat.Max까지 채웁니다.")]
    public bool fillToMaximum;

    [Min(0)]
    [Tooltip("fillToMaximum이 꺼진 탄약형 장비의 시작 탄약입니다.")]
    public int ammunition;
}

[Serializable]
public struct ScriptedBehaviorRequest
{
    [Tooltip("단계 안에서 요청을 안정적으로 식별하는 ID입니다.")]
    public string scenarioId;
    public BehaviorType behavior;
    public BehaveSpot fixedSpot;
    public bool holdUntilResolved;

    [Tooltip("3단계 해킹·탈출구 공격처럼 연출만 허용할 때 사용합니다.")]
    public bool suppressWorldConsequences;

    [Tooltip("3단계 동안 학생의 모든 공격 피해를 0으로 만들 때 사용합니다.")]
    public bool suppressOutgoingDamage;

    [Tooltip("Sing 행동의 품질을 고정합니다. overrideSongQuality가 false면 정규 확률을 사용합니다.")]
    public bool overrideSongQuality;
    public bool useBadSong;
}

[Serializable]
public struct TutorialStudentResetState
{
    public Vector3 position;
    public Quaternion rotation;
    public TutorialStudentMode mode;
    public float health;
    public bool autoStandUp;
    public bool boostBlocked;
    public BehaviorWeightSet behaviorWeightSet;
}

[Serializable]
public struct TutorialGateState
{
    public ExitGate gate;
    public bool isBarricadePlaced;
    public float health;
}

[Serializable]
public struct TutorialPlayerState
{
    public Vector3 position;
    public Quaternion rotation;
    public Quaternion viewRotation;
    public float viewPitch;
    public float health;
    public float stamina;
}

[Serializable]
public struct TutorialWeaponState
{
    public WeaponBase weapon;
    public WeaponItem weaponItem;
    public float ammunition;
}

[Serializable]
public sealed class TutorialWeaponSnapshot
{
    public int selectedIndex;
    public TutorialWeaponState[] slots = Array.Empty<TutorialWeaponState>();
}

[Serializable]
public struct TutorialMicrowaveState
{
    public Microwave microwave;
    public bool hasFood;
    public bool isHazardFood;
    public bool isOperating;
    public GameObject foodObject;
}

[Serializable]
public struct TutorialFireState
{
    public Fire fire;
    public bool isBurning;
}

[Serializable]
public struct TutorialFireSuppressionState
{
    public FireSuppressionSystem system;
    public bool isFlooding;
    public float floodFillRatio;
}

[Serializable]
public struct TutorialRechargerState
{
    public Recharger recharger;
    public bool canRecharge;
    public bool isPreparing;
    public bool interactable;
    public float supplyProgress;
}

[Serializable]
public struct TutorialRechargerBinding
{
    public Recharger recharger;
    public bool enabled;
    [Min(0)] public int sessionCost;
}

[Serializable]
public struct TutorialGateMarkerBinding
{
    public ExitGate gate;
    [Tooltip("파괴되거나 교체되는 런타임 파츠가 아닌 ExitGate 하위의 고정 Transform을 연결합니다.")]
    public Transform markerAnchor;
}

[Serializable]
public struct TutorialProfessorTaskMarkerBinding
{
    public ProfessorTask task;
    [Tooltip("교수가 일하지 않을 때 목표 마커를 표시할 고정 Transform입니다.")]
    public Transform markerAnchor;
}

[Serializable]
public struct TutorialMicrowaveMarkerBinding
{
    public Microwave microwave;
    [Tooltip("전자레인지의 파괴되지 않는 고정 자식 Transform을 연결합니다.")]
    public Transform markerAnchor;
}

public readonly struct ChaosChangedData
{
    public readonly float current;
    public readonly float delta;
    public readonly float rate;
    public readonly ChaosChangeReason reason;

    public ChaosChangedData(float current, float delta, float rate, ChaosChangeReason reason)
    {
        this.current = current;
        this.delta = delta;
        this.rate = rate;
        this.reason = reason;
    }
}

public readonly struct TutorialBehaviorRuntimeContext
{
    public readonly Professor player;
    public readonly StageSpots stageSpots;
    public readonly BehaviorWeightSet behaviorWeightSet;

    public TutorialBehaviorRuntimeContext(
        Professor player,
        StageSpots stageSpots,
        BehaviorWeightSet behaviorWeightSet)
    {
        this.player = player;
        this.stageSpots = stageSpots;
        this.behaviorWeightSet = behaviorWeightSet;
    }
}

public sealed class TutorialActorPoolSnapshot
{
    public readonly List<PostStudent> miniWaveRoster = new();
    public readonly List<TutorialStudentResetState> studentStates = new();
}
