using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using static Global;

public class PostStudent : MonoBehaviour
{
    private static float _idleSpeed = 0;
    public static float _walkSpeed = 2.34f;
    public static float _jogSpeed = 2.43f;
    public static float _slowRunSpeed = 3.49f;
    public static float _mediumRunSpeed = 4.17f;
    public static float _fastRunSpeed = 5.47f;
    public static float _sprintSpeed = 6.75f;

    // Arena Spectator?Ä ?ôÏùº???†ÌÉù Í∞ÄÏ§ëÏπò: Cheer/Rally/Clap 2, Punch/Jab 1.
    private static readonly string[] _tutorialCheerTriggerPool =
    {
        "Cheer_S", "Cheer_S",
        "Rally_S", "Rally_S",
        "Clap_S", "Clap_S",
        "Punch_S",
        "Jab_S",
    };

    private RandomSelector _speedSelector;

    private NavMeshAgent _agent;
    private Animator _anim;
    private BT_Node _root;
    private Blackboard _blackboard;
    private CapsuleCollider _characterCollider;
    [Header("?§Ï†ï")]
    //[SerializeField] private float _changeInterval = 2.0f; // 2Ï¥?Í∞ÑÍ≤©
    //[SerializeField] private Transform _targetDestination; // ?¥Îèô Î™©Ìëú ÏßÄ??
    //
    //[SerializeField] private BehaveSpot _chairSpot;
    //[SerializeField] private SpotGroup _restSpots;
    //[SerializeField] private SpotGroup _microwaveSpots;
    //[SerializeField] private SpotGroup _prowlSpots;

    //[SerializeField] private Professor _player;
    public string Name { get; set; }
    public BehaviorWeightSet BehaviorWeightSet { get; set; }
    //[SerializeField] private StageSpots _stageSpots;

    //private bool _isDamaged = false;
    private DamageReceiver _damageReceiver;
    private BoostReceiver _boostReceiver;
    private Professor _player;
    private StageSpots _stageSpots;

    public Blackboard Blackboard => _blackboard;

    private CharacterRagdoll _characterRagdoll;
    private AnimAttacher[] _animAttachers;
    private PlateAttacher _plateAttacher;
    private SingAttacher _singAttacher;
    private SoundBehavior _soundBehavior;
    private bool _sharedRuntimeInitialized;
    private bool _tutorialBehaviorRuntimeInitialized;
    private bool _isInitializingTutorialRuntime;
    private TutorialStudentMode _tutorialMode;
    private bool _tutorialBoostBlocked;
    private bool _hasScriptedBehaviorRequest;
    private bool _scriptedActionStartedReported;
    private ScriptedBehaviorRequest _scriptedBehaviorRequest;
    private Coroutine _tutorialCheerCoroutine;

    [SerializeField] private OverlapAttacker _bodyOverlapAttacker;
    [SerializeField] private OverlapAttacker _tackleOverlapAttacker;

    [Header("Rush Charge")]
    [SerializeField, Min(0f), Tooltip("Minimum delay added per actual damage hit while charging a rush.")]
    private float _rushHitDelayMin;
    [SerializeField, Min(0f), Tooltip("Maximum delay added per actual damage hit while charging a rush.")]
    private float _rushHitDelayMax;

    private bool _isRushCharging;
    private float _rushChargeBaseDuration;
    private float _rushChargeElapsed;
    private float _rushChargeAddedDelay;
    private int _rushChargeDelayHitCount;

    [HideInInspector] public UnityEvent<PostStudent, HitInfo> DieEvent = new();
    [HideInInspector] public UnityEvent<PostStudent> EscapeEvent = new();
    [Header("Audios")]
    [SerializeField] private SoundData _bodyHitSD;

    public bool IsWorking =>
        Blackboard != null && Blackboard.destBehavior == BehaviorType.Work
        && _anim != null && _anim.enabled && _anim.GetBool("Typing");

    public bool IsDoingHazardBehavior => Blackboard != null && (
        Blackboard.destBehavior.IsHazard()
        || (Blackboard.destBehavior == BehaviorType.UseMicrowave && _plateAttacher.CurrentFood != null && _plateAttacher.CurrentFood.isCauseFire)
        || Blackboard.targetDamageable != null
        || (Blackboard.destBehavior == BehaviorType.Sing && _singAttacher.IsBad));

    public bool IsCausingChaos => _damageReceiver != null && Blackboard != null && _singAttacher != null && _damageReceiver.CanEffect && (Blackboard.targetDamageable != null || (Blackboard.destBehavior == BehaviorType.Sing && _singAttacher.IsBad));
    public bool IsComputerBehavior => Blackboard != null && (
        Blackboard.destBehavior == BehaviorType.Work
        || Blackboard.destBehavior == BehaviorType.Game
        || Blackboard.destBehavior == BehaviorType.Hack);
    public bool IsTutorialBehaviorRuntimeInitialized => _tutorialBehaviorRuntimeInitialized;
    public TutorialStudentMode TutorialMode => _tutorialMode;
    public bool CountsForStageAggregation => !_tutorialBehaviorRuntimeInitialized
        || _tutorialMode == TutorialStudentMode.Training
        || _tutorialMode == TutorialStudentMode.MiniWave;
    public bool SuppressScriptedWorldConsequences => _tutorialBehaviorRuntimeInitialized
        && _hasScriptedBehaviorRequest
        && _scriptedBehaviorRequest.suppressWorldConsequences;
    public bool SuppressOutgoingDamage => _tutorialBehaviorRuntimeInitialized
        && _hasScriptedBehaviorRequest
        && _scriptedBehaviorRequest.suppressOutgoingDamage;
    public bool IsHealthDepleted => _damageReceiver != null && _damageReceiver.Health.IsDepleted;
    public float CurrentHealth => _damageReceiver != null ? _damageReceiver.Health.Current : 0f;
    public float MaxHealth => _damageReceiver != null ? _damageReceiver.Health.Max : 0f;
    public bool IsRushCharging => _isRushCharging;
    public float RushChargeAddedDelay => _rushChargeAddedDelay;
    public int RushChargeDelayHitCount => _rushChargeDelayHitCount;
    public NavMeshAgent TutorialAgent => _agent;
    public event Action<PostStudent, ScriptedBehaviorRequest, TutorialBehaviorTelemetry> ScriptedBehaviorTelemetryEvent;
    public event Action<PostStudent> TutorialStandUpCompletedEvent;


    public MonitorSpot SeatSpot {  get; set; }
    //public BehaviorWeightSet BehaviorWeightSet { get; set; }
    private AttributeModifier _moveSpeedModifier;
    public BT_Node Root => _root;



    private void OnValidate()
    {
        _rushHitDelayMin = Mathf.Max(0f, _rushHitDelayMin);
        _rushHitDelayMax = Mathf.Max(_rushHitDelayMin, _rushHitDelayMax);
    }



    private void Awake()
    {
        _soundBehavior = GetComponent<SoundBehavior>();
        _characterRagdoll = GetComponent<CharacterRagdoll>();
        _damageReceiver = GetComponent<DamageReceiver>();
        _boostReceiver = GetComponent<BoostReceiver>();
        _agent = GetComponent<NavMeshAgent>();
        _anim = GetComponent<Animator>();
        _characterCollider = GetComponent<CapsuleCollider>();
        _agent.acceleration = 100f;
        _animAttachers = GetComponents<AnimAttacher>();
        _plateAttacher = GetComponent<PlateAttacher>();
        _singAttacher = GetComponent<SingAttacher>();

        _damageReceiver.StatDownEvent?.AddListener(OnDamaged);
        _damageReceiver.DepletedEvent?.AddListener(OnDie);

        _boostReceiver.WorkTriggerEvent.AddListener(OnWorkTriggered);
        _boostReceiver.FrenzyTriggerEvent.AddListener(OnFrenzyTriggered);

        _characterRagdoll.StandUpStartEvent?.AddListener(OnStandUpStart);
        _characterRagdoll.StandUpCompleteEvent.AddListener(OnStandUpComplete);

        _player = StageController.Instance.Player;
        _player.DieEvent.AddListener(_ => UnFocusProfessorAttack());

        _stageSpots = StageController.Instance.StageSpots;

        StageController.Instance.StageStartEvent.AddListener(Wakeup);
    }



    private void OnDisable() => ClearRushChargeDelay();



    private void OnDestroy() => ClearRushChargeDelay();




    private void Start()
    {
        InitializeSharedRuntimeIfNeeded();
    }



    private void InitializeSharedRuntimeIfNeeded()
    {
        if (_sharedRuntimeInitialized) return;
        if (BehaviorWeightSet == null)
        {
            Debug.LogError($"[{name}] BehaviorWeightSet Ï∞∏Ï°∞Í∞Ä ?ÜÏäµ?àÎã§.", this);
            return;
        }

        _agent.stoppingDistance = 0.1f;
        BehaviorWeightSet = BehaviorWeightSet.CreateDeepCopy();
        if (!_isInitializingTutorialRuntime)
            BehaviorWeightSet.ModifyChance(BehaviorType.Escape, AttributeSystem.Instance.StudEscapeChanceMod.GetFinalValue());
        HideAllAnimAttachments();
        StopAllOverlapAttackers();
        _characterRagdoll.UnTriggerRagdoll();
        _speedSelector = ConstructSpeedSelector();
        _boostReceiver.CanEffectChecker = CanReceiveBoostByAllRules;
        _damageReceiver.CanEffectChecker = CanReceiveDamageByAllRules;
        _moveSpeedModifier = AttributeSystem.Instance.StudMoveSpeedMod;
        _anim.SetFloat("MoveSpeedScale", _moveSpeedModifier.GetFinalValue());
        _characterCollider.enabled = false;
        Invoke(nameof(PlaySleepingSFX), UnityEngine.Random.Range(0.5f, 2f));
        _anim.SetBool("Laying", true);
        _sharedRuntimeInitialized = true;
    }



    private void Wakeup()
    {
        CancelInvoke(nameof(PlaySleepingSFX));
        _soundBehavior.StopSleeping();
        _anim.SetBool("Laying", false);
    }



    private void PlaySleepingSFX()
    {
        _soundBehavior.PlaySleeping();
    }



    private void StartBehavior()
    {
        CreateBehaviorRuntime();
    }



    private void Update()
    {
        // ?ÑÏû¨ ?êÏù¥?ÑÌä∏???§Ï†ú ?çÎèÑÎ•??†ÎãàÎ©îÏù¥?∞Ïóê ?ÑÎã¨ (Î≥¥Ìè≠ ÎßûÏ∂îÍ∏?
        // MagnitudeÎ•??¨Ïö©?òÎ©¥ Î∞©Ìñ•Í≥??ÅÍ??ÜÏù¥ ?§Ï†ú ?¥Îèô ?çÎèÑÍ∞Ä ?ÑÎã¨?©Îãà??
        //_anim.SetFloat("MoveSpeed", _agent.velocity.magnitude, 0.1f, Time.deltaTime);
        //if (_hitReceiver.IsDead)
        //{
        //    Debug.Log("Die!!");
        //}
        if (_root != null && ShouldEvaluateBehaviorTree())
        {
            if (_tutorialBehaviorRuntimeInitialized && _hasScriptedBehaviorRequest && _scriptedBehaviorRequest.holdUntilResolved)
            {
                _blackboard.isForceBehavior = true;
                _blackboard.destBehavior = _scriptedBehaviorRequest.behavior;
                _blackboard.useAssignedSpot = _scriptedBehaviorRequest.fixedSpot != null;
            }
            _root.Evaluate();
        }
    }



    private void CreateBehaviorRuntime()
    {
        ClearRushChargeDelay();
        _characterCollider.enabled = true;
        _blackboard = new Blackboard(gameObject, BehaviorWeightSet, _stageSpots, _player.gameObject);
        _blackboard.EscapeSuccessEvent.AddListener(OnEscaped);
        _root = ConstructBehaviorTree();
        _root.SetBlackboard(_blackboard);
    }



    private bool ShouldEvaluateBehaviorTree()
    {
        if (!_tutorialBehaviorRuntimeInitialized) return true;
        if (_tutorialMode == TutorialStudentMode.MiniWave) return true;
        return _tutorialMode == TutorialStudentMode.Training
            && (_hasScriptedBehaviorRequest
                || (_blackboard != null
                    && (_blackboard.hasToWork
                        || (_blackboard.isForceBehavior
                            && _blackboard.destBehavior == BehaviorType.Work))));
    }



    private bool CanReceiveBoostByAllRules()
    {
        bool normalRules = _root != null
            && _blackboard != null
            && _blackboard.targetObject == null
            && (_blackboard.destBehavior != BehaviorType.Escape
                || _anim.GetLayerWeight(STRIKE_LAYER_INDEX) < 0.5f);
        bool tutorialRules = !_tutorialBehaviorRuntimeInitialized
            || (!_tutorialBoostBlocked
                && ((_tutorialMode == TutorialStudentMode.Training
                        && (!_hasScriptedBehaviorRequest || _scriptedActionStartedReported))
                    || _tutorialMode == TutorialStudentMode.MiniWave));
        return normalRules && tutorialRules;
    }



    private bool CanReceiveDamageByAllRules()
    {
        bool normalRules = _blackboard != null && !_blackboard.isEscaping;
        bool tutorialRules = !_tutorialBehaviorRuntimeInitialized
            || (_tutorialMode == TutorialStudentMode.Training
                && (!_hasScriptedBehaviorRequest || _scriptedActionStartedReported))
            || _tutorialMode == TutorialStudentMode.MiniWave;
        return normalRules && tutorialRules;
    }



    //void OnAnimatorMove()
    //{
    //    // 1. ?ÑÏû¨ ?ÑÎ†à?ÑÏóê???†ÎãàÎ©îÏù¥?òÏù¥ ?¥Îèô?¥Ïïº ??Í±∞Î¶¨(Delta)Î•?Í∞Ä?∏Ïò¥
    //    // 2. ?¨Í∏∞???¨Ïö©?êÍ? ?êÌïò??% (multiplier)Î•?Í≥±Ìï®
    //    Vector3 desiredVelocity = (_anim.deltaPosition / Time.deltaTime);// * movementMultiplier;

    //    // 3. ?êÏù¥?ÑÌä∏?êÍ≤å "???çÎèÑÎ°??ÄÏßÅÏó¨???ºÍ≥† ÏßÅÏ†ë Î™ÖÎ†π
    //    // ?¥Î†áÍ≤??òÎ©¥ ?†ÎãàÎ©îÏù¥???¨ÏÉù ?çÎèÑ??ÎßûÏ∂∞ ?êÏù¥?ÑÌä∏Í∞Ä ?ÄÏßÅÏù¥ÎØÄÎ°??±ÌÅ¨Í∞Ä ?àÎ? Íπ®Ï?ÏßÄ ?äÏùå
    //    _agent.velocity = desiredVelocity;
    //}



    private void OnWorkTriggered()
    {
        if (_blackboard == null || (_tutorialBehaviorRuntimeInitialized && _tutorialBoostBlocked)) return;
        if (_blackboard.isEscaping) return;
        Debug.Log("OnWorkTriggered");
        _blackboard.isForceBehavior = false;
        _blackboard.hasToWork = true;
    }



    private void OnFrenzyTriggered()
    {
        if (_blackboard == null || (_tutorialBehaviorRuntimeInitialized && _tutorialBoostBlocked)) return;
        if (_blackboard.isEscaping) return;
        Debug.Log("OnFrenzyTriggered");
        _blackboard.hasToFrenzy = true;
    }



    public OverlapAttacker GetOverlapAttacker(OverlapAttackType overlapAttackType)
    {
        switch(overlapAttackType)
        {
            case OverlapAttackType.BodySlam:
                return _bodyOverlapAttacker;
            case OverlapAttackType.Tackle:
                return _tackleOverlapAttacker;
        }
        return null;
    }



    public void StopAllOverlapAttackers()
    {
        Debug.Log("StopAllOverlapAttackers");
        _bodyOverlapAttacker.StopAttack();
        _tackleOverlapAttacker.StopAttack();
    }



    public bool BeginRushChargeDelay(float baseDuration)
    {
        ClearRushChargeDelay();
        if (!isActiveAndEnabled || IsHealthDepleted) return false;

        _rushChargeBaseDuration = Mathf.Max(0f, baseDuration);
        _isRushCharging = true;
        return true;
    }



    public bool TickRushChargeDelay(float deltaTime)
    {
        if (!_isRushCharging) return false;

        _rushChargeElapsed += Mathf.Max(0f, deltaTime);
        return _rushChargeElapsed >= _rushChargeBaseDuration + _rushChargeAddedDelay;
    }



    public void CompleteRushChargeDelay() => ClearRushChargeDelay();



    public void CancelRushChargeDelay() => ClearRushChargeDelay();



    private void AddRushChargeHitDelay(float hitAmount)
    {
        if (!_isRushCharging || hitAmount <= 0f || IsHealthDepleted) return;

        float minDelay = Mathf.Max(0f, _rushHitDelayMin);
        float maxDelay = Mathf.Max(minDelay, _rushHitDelayMax);
        if (maxDelay <= 0f) return;

        float addedDelay = Mathf.Approximately(minDelay, maxDelay)
            ? minDelay
            : UnityEngine.Random.Range(minDelay, maxDelay);
        _rushChargeAddedDelay += addedDelay;
        _rushChargeDelayHitCount++;
    }



    private void ClearRushChargeDelay()
    {
        _isRushCharging = false;
        _rushChargeBaseDuration = 0f;
        _rushChargeElapsed = 0f;
        _rushChargeAddedDelay = 0f;
        _rushChargeDelayHitCount = 0;
    }



    private RandomSelector ConstructSpeedSelector()
    {
        RandomSelector speedSelector = new RandomSelector(
            new List<BT_Node> {
                new SetSpeed(() => _walkSpeed),
                new SetSpeed(() => _jogSpeed),
                new SetSpeed(() => _slowRunSpeed),
                new SetSpeed(() => _mediumRunSpeed),
                new SetSpeed(() => _fastRunSpeed),
                new SetSpeed(() => _sprintSpeed),
            },
            new List<System.Func<float>> {
                () => 40, // Walk ?ïÎ•† 40%
                () => 25, // Jog ?ïÎ•† 25%
                () => 15, // SlowRun 15%
                () => 10, // MedRun 10%
                () => 7,  // FastRun 7%
                () => 3   // Sprint 3%
            }
        );
        return speedSelector;
    }



    private BT_Node ConstructCombatSequence()
    {
        Sequence combatSequence = new Sequence(new List<BT_Node>
        {
            new SetAttackTarget(() => _player.gameObject),
            new CombatApproachPattern()
        });

        return combatSequence;
    }


    //private BT_Node ConstructWorkSequence()
    //{
    //    // 1. Í∞úÎ≥Ñ ?°ÏÖò ?úÌÄÄ???ïÏùò
    //    Sequence angrySeq = new Sequence(new List<BT_Node> { new PlayOnceAnim("Angry", "Angry", 1) });
    //    Sequence clapSeq = new Sequence(new List<BT_Node> { new PlayOnceAnim("Clap", "Clap", 1) });
    //    Sequence frustrateSeq = new Sequence(new List<BT_Node> { new PlayOnceAnim("Frustrated", "Frustrated", 1) });

    //    // 2. ?ÑÎ¨¥Í≤ÉÎèÑ ???òÍ≥† ?Ä?¥ÌïëÎß?Í≥ÑÏÜç???ÅÌÉú (?ÄÍ∏??∏Îìú)
    //    Sequence justTyping = new Sequence(new List<BT_Node> { new Delay(() => 0.1f) });

    //    // 3. ?ïÎ•† ?†ÌÉùÍ∏?Íµ¨ÏÑ± (Í∞ÄÏ§ëÏπò Î∂Ä??
    //    RandomSelector chanceActionSelector = new RandomSelector(
    //        new List<BT_Node> { angrySeq, clapSeq, frustrateSeq, justTyping },
    //        new List<System.Func<int>> {
    //            () => 10, // ??Î∂ÑÎÖ∏) 10%
    //            () => 10, // Î∞ïÏàò 10%
    //            () => 10, // Ï¢åÏ†à 10%
    //            () => 1  // Í∑∏ÎÉ• Í≥ÑÏÜç ?Ä?¥Ìïë 70%
    //        }
    //    );

    //    // 4. Î©îÏù∏ ?åÌÅ¨ ?úÌÄÄ?§Ïóê Ï°∞Î¶Ω
    //    Sequence workSequence = new Sequence(new List<BT_Node>
    //    {
    //        //new SetBehaveSpot(_chairSpot),
    //        _speedSelector,
    //        new MoveToSpot(),
    //        new RotateToSpot(),
    //        new SetAnimBool("Sitting", true),
    //        new SetAnimBool("Typing", true),
    //        new Delay(() => 3f),
    //        chanceActionSelector,
    //        new Delay(() => 6f),
    //        new SetAnimBool("Sitting", false),
    //        new SetAnimBool("Typing", false),
    //    });
    //    return workSequence;
    //}

    private BT_Node ConstructBehaviorTree()
    {
        // ?ôÏûë ?§Í≥Ñ:
        // 1. ?úÎç§ ÏßÄ?êÏúºÎ°??¥Îèô
        // 2. ?ÑÏ∞©?òÎ©¥ 3Ï¥àÍ∞Ñ Ï£ºÎ? Íµ¨Í≤Ω(Loop)
        // 3. 50% ?ïÎ•†Î°?Í∏∞Ï?Í∞?ÏºúÍ∏∞(Once), 50% ?ïÎ•†Î°?Í∑∏ÎÉ• ?ÄÍ∏?
        Sequence prowlSequence = new Sequence(new List<BT_Node>
        {
            //new SetRandomBehaveSpot(_prowlSpots),
            new ActionNode(() => Debug.Log("prowlSequence")),
            new SetRandomSpeedPattern(),
            //_speedSelector,
            new MoveToSpot()
            //new PlayLoopAnim("LookAround", 5)
        });
        Sequence restSequence = new Sequence(new List<BT_Node>
        {
            //new SetRandomBehaveSpot(_restSpots),
            //_speedSelector,
            new SetRandomSpeedPattern(),
            new MoveToSpot(),
            new PlayOnceAnim("LookAround", "LookAround")
            //new PlayLoopAnim("LookAround", 5)
        });
        Sequence smokeSequence = new Sequence(new List<BT_Node>
        {
            //new SetRandomBehaveSpot(_restSpots),
            //_speedSelector,
            new SetRandomSpeedPattern(),
            new MoveToSpot(),
            new PlayOnceAnim("Smoke", "Smoke"),
            //new Delay(() => 2f),
        });
        //Sequence workSequence = new Sequence(new List<BT_Node>
        //{
        //    new SetBehaveSpot(chairSpot),
        //    new SetRandomSpeed(GetRandomSpeed),
        //    new MoveToTarget(),
        //    new RotateToTarget(),
        //    new PlayLoopAnim("Typing", 5)
        //});
        Sequence microwaveSequence = new Sequence(new List<BT_Node>
        {
            //new SetRandomBehaveSpot(_microwaveSpots),
            //_speedSelector,
            new SetRandomSpeedPattern(),
            new SetAnimBool("Carrying", true),
            new MoveToSpot(),
            new SetAnimBool("Carrying", false),
            new ActionNode(() =>
            {
                MicrowaveSpot microwaveSpot = _blackboard.destSpot as MicrowaveSpot;
                if (microwaveSpot == null) return;
                microwaveSpot.PutFoodInMicrowave(GetComponent<PlateAttacher>().CurrentFood);
            }),
            new RotateToSpot(),
            new PlayOnceAnim("PushButton", "PushButton"),
            new ActionNode(() =>
            {
                MicrowaveSpot microwaveSpot = _blackboard.destSpot as MicrowaveSpot;
                if (microwaveSpot == null) return;
                microwaveSpot.OperateMicrowave();
            }),
        });

        //RandomSelector randomJobSelector = new RandomSelector(
        //    new List<BT_Node> { prowlSequence, restSequence, ConstructWorkSequence(), microwaveSequence },
        //    new List<System.Func<int>> { () => 50, () => 50, () => 50, () => 50 }
        //);

        BT_Node combatSubTree = new Sequence(new List<BT_Node>
        {
            new SetAttackTarget(() => _player.gameObject),
            // 1. ?ÅÏóêÍ≤??ëÍ∑º (?¨Í±∞Î¶??àÏóê ?§Ïñ¥???åÍπåÏßÄ Running, ?§Ïñ¥?§Î©¥ Success)
            new ParallelNode(new List<BT_Node>
            {
                new CombatApproachPattern(),
                new RotateToTarget(),
            }),
            //new CombatApproachPattern(),

            // new ParallelNode(new List<BT_Node>
            // {
            //     new Sequence(new List<BT_Node>
            //     {
            //         new StopNode(),
            //         new LerpLayerWeight(Global.COMBAT_LAYER_INDEX, 1f, 16f),
            //     }),
            //     new MeleeAttackPattern(),
            // }),

            // 2. ?¨Í±∞Î¶??àÏóê??Î¨¥Ïûë??Í≥µÍ≤© ?òÌñâ (?†ÎãàÎ©îÏù¥???ùÎÇ† ?åÍπåÏßÄ Running)
            //new MeleeAttackPattern(),

            // 3. Í≥µÍ≤© ???†Íπê????(AIÍ∞Ä ?àÎ¨¥ ??Í∞Ä?òÍ≤å Í≥µÍ≤©?òÏ? ?äÎèÑÎ°?
        });

        //return combatSubTree;
        //return new ReactiveSelector(new List<BT_Node>
        //{
        //    new ConditionDecorator(() => _isDamaged,
        //        new Sequence(new List<BT_Node>
        //        {
        //            new ActionNode(() => _anim.SetLayerWeight(COMBAT_LAYER_INDEX, 0), NodeState.Success),
        //            new SetAnimRootMotion(true),
        //            new PlayOnceAnim("Reaction", "Reaction", 3),
        //            new SetAnimRootMotion(false),
        //            new ActionNode(() => _anim.SetLayerWeight(COMBAT_LAYER_INDEX, 1), NodeState.Success),
        //            new ActionNode(() => _isDamaged = false, NodeState.Success),
        //        })
        //    ),
        //    combatSubTree,
        //});

        //return ConstructCombatSequence();
        // 4. ?ÑÏ≤¥ Î£®Ìä∏Î•?Î∞òÎ≥µ(Selector ?êÎäî Sequence) ?òÎèÑÎ°??§Ï†ï

        //return new Selector(new List<BT_Node> { randomJobSelector });

        Sequence danceSequence = new Sequence(new List<BT_Node>
        {
            //_speedSelector,
            new SetRandomSpeedPattern(),
            new MoveToSpot(),
            new SetAnimBool("Dancing", true),
            //new Delay(() => 5f),
            new DelayRange(6, 8),
        });

        Sequence worshipSequence = new Sequence(new List<BT_Node>
        {
            //_speedSelector,
            new SetRandomSpeedPattern(),
            new MoveToSpot(),
            new RotateToSpot(),
            new SetAnimBool("Praying", true),
            //new Delay(() => 5f),
            new DelayRange(6, 8),
        });

        Sequence sportsSequence = new Sequence(new List<BT_Node>
        {
            //_speedSelector,
            new SetRandomSpeedPattern(),
            new MoveToSpot(),
            new SetAnimBool("Burpeeing", true),
            //new Delay(() => 5f),
            new DelayRange(6, 8),
        });

        Sequence sleepSequence = new Sequence(new List<BT_Node>
        {
            //_speedSelector,
            new SetRandomSpeedPattern(),
            new MoveToSpot(),
            new SetAnimBool("Sleeping", true),
            //new Delay(() => 5f),
            new DelayRange(6, 8),
        });

        Sequence sitFloorSequence = new Sequence(new List<BT_Node>
        {
            //_speedSelector,
            new SetRandomSpeedPattern(),
            new MoveToSpot(),
            new SetAnimBool("SittingFloor", true),
            //new Delay(() => 5f),
            new DelayRange(6, 8),
        });

        Sequence sitChairSequence = new Sequence(new List<BT_Node>
        {
            //_speedSelector,
            new SetRandomSpeedPattern(),
            new MoveToSpot(),
            new RotateToSpot(),
            new SetAnimBool("SittingChair", true),
            //new Delay(() => 5f),
            new DelayRange(6, 8),
        });

        Sequence singSequence = new Sequence(new List<BT_Node>
        {
            //_speedSelector,
            new SetRandomSpeedPattern(),
            new MoveToSpot(),
            new StopAndDisableAgentUpdate(),
            new SetAnimRootMotion(true),
            new SetAnimBool("Singing", true),
            new ActionNode(() => _singAttacher.SingASong()),
            //new Delay(() => 20f),
            new DelayRange(18, 20),
        });

        Sequence coopSequence = new Sequence(new List<BT_Node>
        {
            new ConditionNode(() => (_blackboard.destSpot as CoopSpot2).InviteParticipant(this, _blackboard.destBehavior, StageController.Instance.GetChaosEffectedDelay(UnityEngine.Random.Range(8, 10)))),
            new ClearDestSpot(),



            //new SetRandomSpeedPattern(),
            //new MoveToSpot(),
            //new StopAndDisableAgentUpdate(),
            //new SetAnimRootMotion(true),
            //new SetAnimBool("Singing", true),
            //new ActionNode(() => _singAttacher.SingASong()),
            //new DelayRange(18, 20),
        });

        var behaviorNodes = new Dictionary<BehaviorType, BT_Node>
        {
            { BehaviorType.LookAround, restSequence },
            { BehaviorType.Work, new WorkPattern() },
            { BehaviorType.Game, new WorkPattern() },
            { BehaviorType.Hack, new WorkPattern() },
            { BehaviorType.UseMicrowave, microwaveSequence },
            { BehaviorType.Escape, new TryEscapePattern() },
            { BehaviorType.RushThrough, new RushThroughPattern() },
            { BehaviorType.Smoke, smokeSequence },

            { BehaviorType.Dance, danceSequence },
            { BehaviorType.Worship, worshipSequence },
            { BehaviorType.Sports, sportsSequence },
            { BehaviorType.Sleep, sleepSequence },

            { BehaviorType.SitChair, sitChairSequence },
            { BehaviorType.SitFloor, sitFloorSequence },

            { BehaviorType.Sing, singSequence },
            { BehaviorType.Talk, coopSequence },
            { BehaviorType.Fight, coopSequence },
        };

        Selector jopBehavior = new Selector(new List<BT_Node>
        {
            // 1. Î¨¥Ìïú Î∞òÎ≥µ?¥Ïïº ?òÎäî ?πÏ†ï ÎπÑÌó§?¥ÎπÑ??Ï≤¥ÌÅ¨
            //new ConditionDecorator(() => _blackboard.destBehavior == BehaviorType.Escape,
            //    // ?¨Í∏∞??Ï¥àÍ∏∞?îÍ? ?ÑÏöî ?ÜÎäî Î£®ÌîÑ Î°úÏßÅ Î∞∞Ïπò
            //    behaviorNodes[BehaviorType.Escape]
            //),

            new ConditionDecorator(() => _blackboard.destBehavior == BehaviorType.Tackle,
                // ?¨Í∏∞??Ï¥àÍ∏∞?îÍ? ?ÑÏöî ?ÜÎäî Î£®ÌîÑ Î°úÏßÅ Î∞∞Ïπò
                new Sequence(new List<BT_Node>
                {
                    new ActionNode(HideAllAnimAttachments),
                    new ActionNode(StopAllOverlapAttackers),
                    new EnableAgentUpdate(),
                    new ResetAnimParameters(),
                    new ClearDestSpot(),
                    new TacklePattern(),
                    //new ClearDestBehavior(),
                })
            ),

            // 2. ?ºÎ∞ò?ÅÏù∏ ÎπÑÌó§?¥ÎπÑ??(Îß§Î≤à Ï¥àÍ∏∞?îÍ? ?ÑÏöî??Í∑∏Î£π)
            new Sequence(new List<BT_Node>
            {
                //new SetRandomBehavior(),
                new FindSpotPattern(),
                new ActionNode(HideAllAnimAttachments),
                new ActionNode(StopAllOverlapAttackers),
                new EnableAgentUpdate(),
                new ResetAnimParameters(),
                new SetAnimRootMotion(false),
                new ActionNode(() => Debug.Log(_blackboard.destBehavior)),
                new LerpLayerWeight(COMBAT_LAYER_INDEX, 0, 10),
                new LerpLayerWeight(STRIKE_LAYER_INDEX, 0, 10),
                new EnumSwitchSelector<BehaviorType>(
                    bb => _blackboard.destBehavior,
                    behaviorNodes,
                    prowlSequence
                ),
            })
        });

        Sequence jobSeq = new Sequence(new List<BT_Node>
        {
        new Selector(new List<BT_Node>
            {
                // Í∞ïÏ†ú Î™®ÎìúÎ©??ÑÎ¨¥Í≤ÉÎèÑ ???òÍ≥† Î∞îÎ°ú Success (?¥Î? Í≤∞Ï†ï???âÎèô ?†Ï?)
                new ConditionDecorator(() => _blackboard.isForceBehavior == true && _blackboard.destBehavior != BehaviorType.None,
                    new ActionNode(null, NodeState.Success)),
                new SetRandomBehavior()
            }),

            new PrintDebug("jopBehavior"),
            jopBehavior
        });
        //return new TakeHitReactivePattern(new AttackReactivePattern(new SwimOverridePattern(new BoostReactivePattern(new CoopReactivePattern(new EscapeGiveUpReactivePattern(jobSeq))))));
        return new TakeHitReactivePattern(new AttackReactivePattern(new SwimOverridePattern(new BoostReactivePattern(new CoopReactivePattern(jobSeq)))));
        //return new TakeHitReactivePattern(new AttackReactivePattern(new SwimOverridePattern(new CoopReactivePatttern(new EscapeGiveUpReactivePattern(jobSeq)))));
        //return new TakeHitReactivePattern(new AttackReactivePattern(new SwimOverridePattern(new CoopReactivePatttern(jobSeq))));
        //return new TakeHitReactivePattern(new AttackReactivePattern(new CoopReactivePatttern(jopBehavior)));
        BT_Node tackleTree = new Sequence(new List<BT_Node>
        {
            new TacklePattern(),
        });
        return tackleTree;
    }



    public bool InitializeTutorialBehaviorRuntime(TutorialBehaviorRuntimeContext context)
    {
        if (_tutorialBehaviorRuntimeInitialized)
        {
            Debug.LogWarning($"[{name}] ?úÌÜ†Î¶¨Ïñº ?âÎèô runtime?Ä ?ôÏÉùÎßàÎã§ ??Î≤àÎßå Ï¥àÍ∏∞?îÌï† ???àÏäµ?àÎã§.", this);
            return false;
        }
        if (context.player == null || context.stageSpots == null || context.behaviorWeightSet == null)
        {
            Debug.LogError($"[{name}] ?úÌÜ†Î¶¨Ïñº ?âÎèô runtime ?ÑÏàò Ï∞∏Ï°∞Í∞Ä ?ÑÎùΩ?êÏäµ?àÎã§.", this);
            return false;
        }

        _player = context.player;
        _stageSpots = context.stageSpots;
        BehaviorWeightSet = context.behaviorWeightSet;
        _isInitializingTutorialRuntime = true;
        InitializeSharedRuntimeIfNeeded();
        _isInitializingTutorialRuntime = false;
        if (!_sharedRuntimeInitialized) return false;

        CancelInvoke(nameof(PlaySleepingSFX));
        _soundBehavior.StopSleeping();
        _anim.SetBool("Laying", false);
        CreateBehaviorRuntime();
        _tutorialBehaviorRuntimeInitialized = true;
        SetTutorialMode(TutorialStudentMode.Standby);
        return true;
    }



    public bool ResetTutorialBehaviorRuntime(TutorialStudentResetState state)
    {
        if (!_tutorialBehaviorRuntimeInitialized)
        {
            Debug.LogError($"[{name}] Ï¥àÍ∏∞?îÎêòÏßÄ ?äÏ? ?úÌÜ†Î¶¨Ïñº ?âÎèô runtime?Ä reset?????ÜÏäµ?àÎã§.", this);
            return false;
        }

        if (!gameObject.activeSelf) gameObject.SetActive(true);

        if (_hasScriptedBehaviorRequest)
            CancelScriptedBehaviorInternal(false);
        else
            CleanupCurrentBehaviorRuntime();
        _root?.Reset();
        ClearRushChargeDelay();
        _root = null;
        if (state.behaviorWeightSet != null)
            BehaviorWeightSet = state.behaviorWeightSet.CreateDeepCopy();

        _characterRagdoll.RestoreAutoStandUpRuntimeOverride();
        if (!state.autoStandUp)
            _characterRagdoll.SetAutoStandUpRuntimeOverride(false);
        _characterRagdoll.UnTriggerRagdoll();
        _damageReceiver.Health.Initialize(true);
        _damageReceiver.Health.Increase(Mathf.Clamp(state.health, 0f, _damageReceiver.Health.Max));
        _tutorialBoostBlocked = state.boostBlocked;
        ResetTutorialAnimationAndAttachments();

        if (!_agent.enabled) _agent.enabled = true;
        WarpForTutorial(state.position, state.rotation);
        _agent.updatePosition = true;
        _agent.updateRotation = true;
        _characterCollider.enabled = true;

        CreateBehaviorRuntime();
        SetTutorialMode(state.mode);
        return true;
    }



    public TutorialStudentResetState CaptureTutorialResetState()
    {
        return new TutorialStudentResetState
        {
            position = transform.position,
            rotation = transform.rotation,
            mode = _tutorialMode,
            health = CurrentHealth,
            autoStandUp = _characterRagdoll.IsAutoStandUpEnabled,
            boostBlocked = _tutorialBoostBlocked,
            behaviorWeightSet = BehaviorWeightSet,
        };
    }



    public void SetTutorialMode(TutorialStudentMode mode)
    {
        if (!_tutorialBehaviorRuntimeInitialized) return;
        if (_tutorialMode != mode)
            ClearRushChargeDelay();
        if (_tutorialMode == TutorialStudentMode.Cheer && mode != TutorialStudentMode.Cheer)
            StopTutorialCheerAnimation(true);
        if (mode != TutorialStudentMode.Training && mode != TutorialStudentMode.MiniWave)
        {
            if (_hasScriptedBehaviorRequest)
                CancelScriptedBehaviorInternal(true);
            else if (_tutorialMode == TutorialStudentMode.Training || _tutorialMode == TutorialStudentMode.MiniWave)
                CleanupCurrentBehaviorRuntime();
        }
        _tutorialMode = mode;
        bool isActive = mode == TutorialStudentMode.Training || mode == TutorialStudentMode.MiniWave;
        _characterCollider.enabled = isActive;
        if (!isActive)
        {
            StopAllOverlapAttackers();
            if (_agent.enabled && _agent.isOnNavMesh)
            {
                _agent.ResetPath();
                _agent.velocity = Vector3.zero;
            }
            _anim.SetFloat("MoveSpeed", 0f);
        }
    }



    public bool BeginScriptedBehavior(ScriptedBehaviorRequest request)
    {
        if (!_tutorialBehaviorRuntimeInitialized || _tutorialMode != TutorialStudentMode.Training)
            return false;
        if (string.IsNullOrWhiteSpace(request.scenarioId)
            || request.behavior == BehaviorType.None
            || request.fixedSpot == null)
        {
            Debug.LogError($"[{name}] scripted behavior ?îÏ≤≠??ID, ?âÎèô ?êÎäî spot???ÑÎùΩ?êÏäµ?àÎã§.", this);
            return false;
        }

        CancelScriptedBehaviorInternal(false);
        _scriptedBehaviorRequest = request;
        _hasScriptedBehaviorRequest = true;
        _scriptedActionStartedReported = false;
        _blackboard.prevBehavior = _blackboard.destBehavior;
        _blackboard.destBehavior = request.behavior;
        _blackboard.destSpot = request.fixedSpot;
        _blackboard.destPosition = request.fixedSpot.transform.position;
        _blackboard.isForceBehavior = true;
        _blackboard.useAssignedSpot = true;
        request.fixedSpot.Use(this);
        if (request.overrideSongQuality)
            _singAttacher?.SetTutorialSongQuality(request.useBadSong);
        _root?.Reset();
        ScriptedBehaviorTelemetryEvent?.Invoke(this, request, TutorialBehaviorTelemetry.Selected);
        return true;
    }



    public bool ResolveScriptedBehavior(string scenarioId)
    {
        if (!_hasScriptedBehaviorRequest || _scriptedBehaviorRequest.scenarioId != scenarioId)
            return false;
        ScriptedBehaviorRequest completed = _scriptedBehaviorRequest;
        CancelScriptedBehaviorInternal(false);
        ScriptedBehaviorTelemetryEvent?.Invoke(this, completed, TutorialBehaviorTelemetry.Completed);
        return true;
    }



    public bool CancelScriptedBehavior(string scenarioId)
    {
        if (!_hasScriptedBehaviorRequest || _scriptedBehaviorRequest.scenarioId != scenarioId)
            return false;
        return CancelScriptedBehaviorInternal(true);
    }



    private bool CancelScriptedBehaviorInternal(bool reportInterrupted)
    {
        if (!_hasScriptedBehaviorRequest) return false;
        ClearRushChargeDelay();
        DOTween.Kill(this);
        ScriptedBehaviorRequest cancelled = _scriptedBehaviorRequest;
        _blackboard?.destSpot?.Release(this);
        if (_blackboard != null)
        {
            _blackboard.SecadeCoop();
            _blackboard.SecadeCoop2();
            _blackboard.destSpot = null;
            _blackboard.destBehavior = BehaviorType.None;
            _blackboard.targetDamageable = null;
            _blackboard.targetObject = null;
            _blackboard.hasToWork = false;
            _blackboard.hasToFrenzy = false;
            _blackboard.isForceBehavior = false;
            _blackboard.useAssignedSpot = false;
        }
        _root?.Reset();
        _singAttacher?.ClearTutorialSongQuality();
        ResetTutorialAnimationAndAttachments();
        _hasScriptedBehaviorRequest = false;
        _scriptedActionStartedReported = false;
        _scriptedBehaviorRequest = default;
        if (reportInterrupted)
            ScriptedBehaviorTelemetryEvent?.Invoke(this, cancelled, TutorialBehaviorTelemetry.Interrupted);
        return true;
    }



    private void CleanupCurrentBehaviorRuntime()
    {
        ClearRushChargeDelay();
        DOTween.Kill(this);
        if (_blackboard != null)
        {
            _blackboard.destSpot?.Release(this);
            _blackboard.SecadeCoop();
            _blackboard.SecadeCoop2();
            _blackboard.destSpot = null;
            _blackboard.destBehavior = BehaviorType.None;
            _blackboard.targetDamageable = null;
            _blackboard.targetObject = null;
            _blackboard.hasToWork = false;
            _blackboard.hasToFrenzy = false;
            _blackboard.isForceBehavior = false;
            _blackboard.useAssignedSpot = false;
            _blackboard.isEscaping = false;
        }
        _root?.Reset();
        ResetTutorialAnimationAndAttachments();
    }



    public void NotifyTutorialBehaviorActionStarted()
    {
        if (!_tutorialBehaviorRuntimeInitialized
            || !_hasScriptedBehaviorRequest
            || _scriptedActionStartedReported)
            return;
        _scriptedActionStartedReported = true;
        ScriptedBehaviorTelemetryEvent?.Invoke(
            this,
            _scriptedBehaviorRequest,
            TutorialBehaviorTelemetry.ActionStarted);
    }



    public void SetTutorialBoostBlocked(bool isBlocked)
    {
        if (_tutorialBehaviorRuntimeInitialized)
            _tutorialBoostBlocked = isBlocked;
    }



    public bool PrepareTutorialBoostedWorkSpot(BehaveSpot spot)
    {
        if (!_tutorialBehaviorRuntimeInitialized
            || _tutorialMode != TutorialStudentMode.Training
            || spot == null
            || !spot.HasBehavior(BehaviorType.Work)
            || !spot.IsUsable)
            return false;

        _blackboard.destSpot?.Release(this);
        _blackboard.destSpot = spot;
        _blackboard.destPosition = spot.transform.position;
        _blackboard.useAssignedSpot = true;
        spot.Use(this);
        return true;
    }



    public void SetTutorialAutoStandUp(bool isEnabled)
    {
        if (!_tutorialBehaviorRuntimeInitialized) return;
        _characterRagdoll.SetAutoStandUpRuntimeOverride(isEnabled);
    }



    public void RestoreTutorialAutoStandUp()
    {
        if (!_tutorialBehaviorRuntimeInitialized) return;
        _characterRagdoll.RestoreAutoStandUpRuntimeOverride();
    }



    public void ForceStopTutorialWork()
    {
        if (!_tutorialBehaviorRuntimeInitialized) return;
        if (_hasScriptedBehaviorRequest && _scriptedBehaviorRequest.behavior == BehaviorType.Work)
            CancelScriptedBehaviorInternal(true);
        if (_blackboard != null)
        {
            _blackboard.destSpot?.Release(this);
            _blackboard.SecadeCoop();
            _blackboard.SecadeCoop2();
            _blackboard.destSpot = null;
            _blackboard.hasToWork = false;
            if (_blackboard.destBehavior == BehaviorType.Work)
                _blackboard.destBehavior = BehaviorType.None;
            _blackboard.isForceBehavior = false;
            _blackboard.useAssignedSpot = false;
        }
        _root?.Reset();
        _anim.SetBool("Typing", false);
        _anim.SetBool("Sitting", false);
    }



    public void StartTutorialMiniWave(BehaviorWeightSet miniWaveWeights)
    {
        if (!_tutorialBehaviorRuntimeInitialized || miniWaveWeights == null) return;
        CancelScriptedBehaviorInternal(false);
        ClearRushChargeDelay();
        _root = null;
        BehaviorWeightSet = miniWaveWeights.CreateDeepCopy();
        CreateBehaviorRuntime();
        SetTutorialMode(TutorialStudentMode.MiniWave);
    }



    public bool WarpForTutorial(Vector3 position, Quaternion rotation)
    {
        if (!_tutorialBehaviorRuntimeInitialized) return false;
        if (!NavMesh.SamplePosition(position, out NavMeshHit hit, 1f, NavMesh.AllAreas))
        {
            transform.SetPositionAndRotation(position, rotation);
            return false;
        }
        transform.SetPositionAndRotation(hit.position, rotation);
        if (!_agent.enabled) _agent.enabled = true;
        return _agent.Warp(hit.position) && _agent.isOnNavMesh;
    }



    public bool TryRecoverTutorialAgentOnNavMesh(Vector3 returnTarget)
    {
        if (!_tutorialBehaviorRuntimeInitialized || !isActiveAndEnabled || _agent == null)
            return false;
        if (!NavMesh.SamplePosition(returnTarget, out NavMeshHit targetHit, 5f, NavMesh.AllAreas))
            return false;

        NavMeshPath path = new();
        if (_agent.enabled
            && _agent.isOnNavMesh
            && NavMesh.CalculatePath(transform.position, targetHit.position, NavMesh.AllAreas, path)
            && path.status == NavMeshPathStatus.PathComplete)
            return true;

        Vector3 recoveryOrigin = transform.position;
        float searchRadius = 2f;
        for (int attempt = 0; attempt < 5; attempt++)
        {
            if (NavMesh.SamplePosition(recoveryOrigin, out NavMeshHit recoveryHit, searchRadius, NavMesh.AllAreas)
                && NavMesh.CalculatePath(recoveryHit.position, targetHit.position, NavMesh.AllAreas, path)
                && path.status == NavMeshPathStatus.PathComplete
                && TryPlaceTutorialAgentOnNavMesh(recoveryHit.position))
                return true;
            searchRadius += 4f;
        }

        return TryPlaceTutorialAgentOnNavMesh(targetHit.position);
    }



    private bool TryPlaceTutorialAgentOnNavMesh(Vector3 position)
    {
        if (_agent.enabled && _agent.isOnNavMesh)
            _agent.ResetPath();
        transform.position = position;
        if (!_agent.enabled) _agent.enabled = true;
        return _agent.Warp(position) && _agent.isOnNavMesh;
    }



    public bool MoveForTutorial(Vector3 destination, float speed)
    {
        if (!_tutorialBehaviorRuntimeInitialized || !_agent.enabled || !_agent.isOnNavMesh)
            return false;
        _agent.updatePosition = true;
        _agent.updateRotation = true;
        _agent.speed = speed;
        bool pathStarted = _agent.SetDestination(destination);
        _anim.SetFloat("MoveSpeed", pathStarted ? speed : 0f);
        return pathStarted;
    }



    public void StopTutorialMovementAnimation()
    {
        if (!_tutorialBehaviorRuntimeInitialized) return;
        _anim.SetFloat("MoveSpeed", 0f);
    }



    public bool StartTutorialCheer()
    {
        if (!_tutorialBehaviorRuntimeInitialized
            || _tutorialMode != TutorialStudentMode.Cheer
            || IsHealthDepleted
            || !isActiveAndEnabled)
            return false;

        foreach (string triggerName in _tutorialCheerTriggerPool)
        {
            if (HasAnimatorParameter(triggerName, AnimatorControllerParameterType.Trigger))
                continue;
            Debug.LogError($"[{name}] Arena ?ëÏõê Trigger '{triggerName}'Í∞Ä Animator???ÜÏäµ?àÎã§.", this);
            return false;
        }

        ResetTutorialAnimationAndAttachments();
        _tutorialCheerCoroutine = StartCoroutine(TutorialCheerRoutine());
        return true;
    }



    public void StopTutorialCheer() => StopTutorialCheerAnimation(true);



    private IEnumerator TutorialCheerRoutine()
    {
        while (_tutorialMode == TutorialStudentMode.Cheer && !IsHealthDepleted)
        {
            while (_anim.IsInTransition(0)
                && _tutorialMode == TutorialStudentMode.Cheer
                && !IsHealthDepleted)
                yield return null;

            if (_tutorialMode != TutorialStudentMode.Cheer || IsHealthDepleted)
                break;

            string triggerName = _tutorialCheerTriggerPool[
                UnityEngine.Random.Range(0, _tutorialCheerTriggerPool.Length)];
            _anim.SetTrigger(triggerName);
            bool enteredState = false;

            while (_tutorialMode == TutorialStudentMode.Cheer && !IsHealthDepleted)
            {
                AnimatorStateInfo stateInfo = _anim.GetCurrentAnimatorStateInfo(0);
                bool isTargetState = stateInfo.IsName(triggerName)
                    || stateInfo.IsName("Base Layer." + triggerName);
                if (isTargetState)
                {
                    enteredState = true;
                    if (stateInfo.normalizedTime >= 0.95f)
                        break;
                }
                else if (enteredState && !_anim.IsInTransition(0))
                {
                    break;
                }
                yield return null;
            }

            _anim.ResetTrigger(triggerName);
            yield return null;
        }

        _tutorialCheerCoroutine = null;
    }



    private bool HasAnimatorParameter(string parameterName, AnimatorControllerParameterType parameterType)
    {
        foreach (AnimatorControllerParameter parameter in _anim.parameters)
        {
            if (parameter.type == parameterType && parameter.name == parameterName)
                return true;
        }
        return false;
    }



    private void StopTutorialCheerAnimation(bool returnToIdle)
    {
        bool wasRunning = _tutorialCheerCoroutine != null;
        if (_tutorialCheerCoroutine != null)
        {
            StopCoroutine(_tutorialCheerCoroutine);
            _tutorialCheerCoroutine = null;
        }
        foreach (string triggerName in _tutorialCheerTriggerPool)
            _anim.ResetTrigger(triggerName);
        if (wasRunning && returnToIdle && _anim.enabled && gameObject.activeInHierarchy)
            _anim.CrossFade("Locomotion", 0.1f, 0);
    }



    public void ApplyTutorialPose(string animatorBool)
    {
        if (!_tutorialBehaviorRuntimeInitialized) return;
        ResetTutorialAnimationAndAttachments();
        if (string.IsNullOrWhiteSpace(animatorBool)) return;
        foreach (AnimatorControllerParameter parameter in _anim.parameters)
        {
            if (parameter.type == AnimatorControllerParameterType.Bool && parameter.name == animatorBool)
            {
                _anim.SetBool(animatorBool, true);
                return;
            }
        }
        Debug.LogError($"[{name}] Animator bool '{animatorBool}'???ÜÏäµ?àÎã§.", this);
    }



    public bool ShowTutorialFood(GameObject foodSource)
    {
        if (!_tutorialBehaviorRuntimeInitialized
            || _tutorialMode != TutorialStudentMode.Standby
            || _plateAttacher == null
            || foodSource == null)
            return false;

        if (!_plateAttacher.ShowTutorialFood(foodSource))
            return false;

        _anim.SetBool("Carrying", true);
        return true;
    }



    public void ClearTutorialFood()
    {
        if (!_tutorialBehaviorRuntimeInitialized || _plateAttacher == null) return;
        _anim.SetBool("Carrying", false);
        _plateAttacher.ClearTutorialFood();
    }



    private void ResetTutorialAnimationAndAttachments()
    {
        ClearRushChargeDelay();
        StopTutorialCheerAnimation(false);
        StopAllOverlapAttackers();
        HideAllAnimAttachments();
        _anim.applyRootMotion = false;
        if (!_anim.enabled) _anim.enabled = true;
        foreach (AnimatorControllerParameter parameter in _anim.parameters)
        {
            switch (parameter.type)
            {
                case AnimatorControllerParameterType.Bool:
                    _anim.SetBool(parameter.nameHash, false);
                    break;
                case AnimatorControllerParameterType.Trigger:
                    _anim.ResetTrigger(parameter.nameHash);
                    break;
            }
        }
        _anim.SetFloat("MoveSpeed", 0f);
    }



    public void UnFocusProfessorAttack()
    {
        if (_blackboard == null || _player == null) return;
        if (_blackboard.targetObject != _player.gameObject) return;
        _blackboard.targetObject = null;
        _blackboard.targetDamageable = null;
        _blackboard.isForceBehavior = false;
        _blackboard.hasToWork = false;
        _blackboard.hasToFrenzy = false;
    }



    private float GetRandomSpeed()
    {
        return UnityEngine.Random.Range(_walkSpeed, _sprintSpeed);
    }

    //private void Awake()
    //{
    //    _agent = GetComponent<NavMeshAgent>();
    //    _anim = GetComponent<Animator>();

    //    // Í∞Ä?çÎèÑÎ•??íÏó¨???çÎèÑ Î≥Ä?îÍ? Ï¶âÍ∞Å?ÅÏúºÎ°?Î≥¥ÏûÖ?àÎã§.
    //    _agent.acceleration = 30f;
    //}

    //private void Start()
    //{
    //    if (_targetDestination != null)
    //    {
    //        _agent.SetDestination(_targetDestination.position);
    //        StartCoroutine(MovementRoutine());
    //    }
    //}

    private IEnumerator MovementRoutine()
    {
        while (true)
        {
            // 1?®Í≥Ñ: ?ïÏ?
            // UpdateState("?ïÏ?", _idleSpeed);
            // yield return new WaitForSeconds(_changeInterval);

            // 2?®Í≥Ñ: Í±∑Í∏∞
            // UpdateState("Í±∑Í∏∞", _walkSpeed);
            // yield return new WaitForSeconds(_changeInterval);

            // 3?®Í≥Ñ: Ï°∞ÍπÖ
            //UpdateState("Ï°∞ÍπÖ", _jogSpeed);
            //yield return new WaitForSeconds(_changeInterval);

            // 4?®Í≥Ñ: ?∞Í∏∞
            // UpdateState("?∞Í∏∞", _runSpeed);
            // yield return new WaitForSeconds(_changeInterval);

            // 5?®Í≥Ñ: ?ÑÎ†•ÏßàÏ£º
            // UpdateState("?ÑÎ†•ÏßàÏ£º", _sprintSpeed);
            // yield return new WaitForSeconds(_changeInterval);
        }
    }

    private void UpdateState(string stateName, float speed)
    {
        _agent.speed = speed;
        Debug.Log($"?ÑÏû¨ ?ÅÌÉú: {stateName} (?çÎèÑ: {speed})");
    }



    private void OnDodge(HitInfo hitInfo)
    {
        Debug.Log("Dodge!!!");
    }



    private void OnDamaged(Vector3 hitPoint, Quaternion hitRotation, float impulse, GameObject killer)
    {

    }



    private Coroutine knockbackCoroutine;



    private void OnDamaged(HitInfo hitInfo, float hitAmount)
    {
        AddRushChargeHitDelay(hitAmount);
        if (_blackboard == null) return;
        _blackboard.isDamaged = true;
        _blackboard.isStunned = true;
        if (hitAmount > 0f && _player != null && hitInfo.attacker == _player.gameObject && !_damageReceiver.Health.IsDepleted)
        {
            HitMarkerUI.Instance?.PlayHit();
        }
        //PlayScene3DSFX(_bodyHitSD, hitInfo.hitPoint);
    }



    public void HideAllAnimAttachments()
    {
        foreach (var animAttacher in _animAttachers)
        {
            animAttacher.HideAll();
        }
    }



    public void OnEscaped()
    {
        if (SuppressScriptedWorldConsequences) return;
        ClearRushChargeDelay();
        EscapeEvent?.Invoke(this);
        _blackboard.destSpot?.Release(this);
        gameObject.SetActive(false);
        _root = null;
    }



    private void OnDie(HitInfo hitInfo)
    {
        ClearRushChargeDelay();
        DieEvent?.Invoke(this, hitInfo);

        GameObject playerObject = _blackboard.Player.gameObject;
        if (_blackboard.targetObject == playerObject && hitInfo.attacker == playerObject)
        {
            int money = (int)AttributeSystem.Instance.MutinyMoneyMod.GetFinalValue(0);
            if (money > 0)
            {
                StageController.Instance.Earn(money);
            }
        }

        _root = null;
        _agent.speed = 0;
        _agent.enabled = false;
        _anim.enabled = false;
        _characterCollider.enabled = false;
        _blackboard.destSpot?.Release(this);
        _blackboard.destBehavior = BehaviorType.None;
        _blackboard.targetDamageable = null;
        _blackboard.targetObject = null;
        StopAllCoroutines();
        StopAllOverlapAttackers();
        HideAllAnimAttachments();
        //_ragdollStandup.SetRagdoll(true);
        _characterRagdoll.TriggerRagdoll();
        _characterRagdoll.ApplyBoneImpact(hitInfo.hitPoint, hitInfo.hitRotation, hitInfo.impulse);

        //Invoke(nameof(Revive), 2f);
    }



    private void OnStandUpStart()
    {
        //bool originAgentEnabled = _agent.enabled;
        //bool originAgentUpdatePos = _agent.updatePosition;
        //_agent.enabled = true;
        //_agent.updatePosition = true;
        //_agent.Warp(SampleNavMesh(transform.position, 100f));
        //_agent.enabled = originAgentEnabled;
        //_agent.updatePosition = originAgentUpdatePos;

        _damageReceiver.SetStatFull();
    }



    private void OnStandUpComplete()
    {
        _agent.updatePosition = true;    // ?êÏù¥?ÑÌä∏Í∞Ä ?∏Îûú?§Ìèº???ÄÏßÅÏù¥?ÑÎ°ù ?àÏö©
        _agent.updateRotation = true;    // ?åÏ†Ñ???àÏö©
        _anim.applyRootMotion = false;
        _anim.SetFloat("MoveSpeedScale", _moveSpeedModifier.GetFinalValue());
        if (_tutorialBehaviorRuntimeInitialized)
        {
            CreateBehaviorRuntime();
            TutorialStandUpCompletedEvent?.Invoke(this);
        }
        else
        {
            _blackboard = new Blackboard(gameObject, BehaviorWeightSet, _stageSpots, _player.gameObject);
            _root = ConstructBehaviorTree();
            _root.SetBlackboard(_blackboard);
            OnWorkTriggered();
        }
    }


    //?¥Ï†Ñ
    //private void SetRagdoll(bool isActive)
    //{
    //    _anim.enabled = !isActive;
    //    _agent.enabled |= isActive;

    //    if (TryGetComponent(out Rigidbody rootRb))
    //    {
    //        //rootRb.isKinematic = isActive; // ?òÍ∑∏?åÏù¥Î©?Î≥∏Ï≤¥ Î¨ºÎ¶¨ ?∞ÏÇ∞ Ï§ëÎã®
    //        rootRb.useGravity = !isActive;
    //    }

    //    foreach (var rb in GetComponentsInChildren<Rigidbody>())
    //    {
    //        if (rb == rootRb) continue;
    //        rb.isKinematic = !isActive;

    //        if (isActive) rb.linearVelocity = Vector3.zero;

    //        if (rb.TryGetComponent(out Collider col))
    //        {
    //            col.isTrigger = !isActive;
    //        }
    //    }
    //}



    private void OnDie(Vector3 hitPoint, Quaternion hitRotation, float impulse, GameObject killer)
    {
        ClearRushChargeDelay();
        _root = null;
        _agent.speed = 0;
        _anim.enabled = false;
        _characterCollider.enabled = false;

        // ?òÍ∑∏??Î∂Ä?ÑÎì§??Ï∞æÏïÑ Î¨ºÎ¶¨ ?ÅÏö©
        foreach (var rb in GetComponentsInChildren<Rigidbody>())
        {
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero; // ?Ä???ÑÏÉÅ Î∞©Ï???Ï¥àÍ∏∞??

            // ?? killer???ÑÏπòÎ°úÎ???Î∞òÎ? Î∞©Ìñ•?ºÎ°ú ?ÑÏ£º ?¥Ïßù ?òÏùÑ Ï£ºÎ©¥ ???êÏó∞?§ÎüΩ?µÎãà??
            if (killer != null)
            {
                //ApplyRagdollImpact(hitPoint, hitRotation, impulse);
            }
        }
    }



    //private void ApplyRagdollImpact(Vector3 hitPoint, Quaternion hitRotation, float impulse)
    //{
    //    Rigidbody closestRb = null;
    //    float closestDistance = float.MaxValue;

    //    // 1. Î™®Îì† ?òÍ∑∏??Î¶¨Ï??úÎ∞î??Ï§??ºÍ≤© ÏßÄ?êÍ≥º Í∞Ä??Í∞ÄÍπåÏö¥ Î∂Ä?ÑÎ? Ï∞æÏäµ?àÎã§.
    //    Rigidbody[] rbs = GetComponentsInChildren<Rigidbody>();
    //    foreach (var rb in rbs)
    //    {
    //        float dist = Vector3.Distance(rb.position, hitPoint);
    //        if (dist < closestDistance)
    //        {
    //            closestDistance = dist;
    //            closestRb = rb;
    //        }
    //    }

    //    // 2. ?¥Îãπ Î∂Ä?ÑÏóê Î¨ºÎ¶¨ Ï∂©Í≤©??Í∞Ä?©Îãà??
    //    if (closestRb != null)
    //    {
    //        // hitRotation??forward Î∞©Ìñ•?ºÎ°ú ?òÏùÑ ?ÑÎã¨
    //        Vector3 forceDir = hitRotation * Vector3.back;

    //        // AddForceAtPosition???∞Î©¥ ?ºÍ≤© ÏßÄ??Í∏∞Ï??ºÎ°ú ?åÏ†Ñ?•ÍπåÏßÄ Î∞úÏÉù?¥ÏÑú ???¨Ïã§?ÅÏûÖ?àÎã§.
    //        closestRb.AddForceAtPosition(forceDir * impulse, hitPoint, ForceMode.Impulse);
    //    }
    //}
}
