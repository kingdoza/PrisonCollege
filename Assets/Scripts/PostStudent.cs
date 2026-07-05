using System.Collections;
using System.Collections.Generic;
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

    private RandomSelector _speedSelector;

    private NavMeshAgent _agent;
    private Animator _anim;
    private BT_Node _root;
    private Blackboard _blackboard;
    private CapsuleCollider _characterCollider;
    [Header("설정")]
    //[SerializeField] private float _changeInterval = 2.0f; // 2초 간격
    //[SerializeField] private Transform _targetDestination; // 이동 목표 지점
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

    [SerializeField] private OverlapAttacker _bodyOverlapAttacker;
    [SerializeField] private OverlapAttacker _tackleOverlapAttacker;

    [HideInInspector] public UnityEvent<PostStudent, HitInfo> DieEvent = new();
    [HideInInspector] public UnityEvent<PostStudent> EscapeEvent = new();
    [Header("Audios")]
    [SerializeField] private SoundData _bodyHitSD;

    public bool IsWorking => 
        Blackboard != null && Blackboard.destBehavior == BehaviorType.Work
        && _anim != null && _anim.enabled && _anim.GetBool("Typing");

    public bool IsDoingHazardBehavior => (
        Blackboard.destBehavior.IsHazard()
        || (Blackboard.destBehavior == BehaviorType.UseMicrowave && _plateAttacher.CurrentFood != null && _plateAttacher.CurrentFood.isCauseFire)
        || Blackboard.targetDamageable != null
        || (Blackboard.destBehavior == BehaviorType.Sing && _singAttacher.IsBad));

    public bool IsCausingChaos => _damageReceiver != null && Blackboard != null && _singAttacher != null && _damageReceiver.CanEffect && (Blackboard.targetDamageable != null || (Blackboard.destBehavior == BehaviorType.Sing && _singAttacher.IsBad));
    public bool IsComputerBehavior =>
        Blackboard.destBehavior == BehaviorType.Work
        || Blackboard.destBehavior == BehaviorType.Game
        || Blackboard.destBehavior == BehaviorType.Hack;


    public MonitorSpot SeatSpot {  get; set; }
    //public BehaviorWeightSet BehaviorWeightSet { get; set; }
    private AttributeModifier _moveSpeedModifier;
    public BT_Node Root => _root;


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




    private void Start()
    {
        _agent.stoppingDistance = 0.1f;
        BehaviorWeightSet = BehaviorWeightSet.CreateDeepCopy();
        BehaviorWeightSet.ModifyChance(BehaviorType.Escape, AttributeSystem.Instance.StudEscapeChanceMod.GetFinalValue());
        HideAllAnimAttachments();
        StopAllOverlapAttackers();
        _characterRagdoll.UnTriggerRagdoll();
        _speedSelector = ConstructSpeedSelector();
        _boostReceiver.CanEffectChecker = () => _root != null && _blackboard != null && (_blackboard.targetObject == null && (_blackboard.destBehavior != BehaviorType.Escape ||_anim.GetLayerWeight(STRIKE_LAYER_INDEX) < 0.5f));
        _damageReceiver.CanEffectChecker = () => _blackboard != null && _blackboard.isEscaping == false;
        _moveSpeedModifier = AttributeSystem.Instance.StudMoveSpeedMod;
        _anim.SetFloat("MoveSpeedScale", _moveSpeedModifier.GetFinalValue());
        _characterCollider.enabled = false;
        Invoke(nameof(PlaySleepingSFX), UnityEngine.Random.Range(0.5f, 2f));
        _anim.SetBool("Laying", true);
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
        _characterCollider.enabled = true;
        _blackboard = new Blackboard(gameObject, BehaviorWeightSet, _stageSpots, _player.gameObject);
        _blackboard.EscapeSuccessEvent.AddListener(OnEscaped);
        _root = ConstructBehaviorTree();
        _root.SetBlackboard(_blackboard);
    }



    private void Update()
    {
        // 현재 에이전트의 실제 속도를 애니메이터에 전달 (보폭 맞추기)
        // Magnitude를 사용하면 방향과 상관없이 실제 이동 속도가 전달됩니다.
        //_anim.SetFloat("MoveSpeed", _agent.velocity.magnitude, 0.1f, Time.deltaTime);
        //if (_hitReceiver.IsDead)
        //{
        //    Debug.Log("Die!!");
        //}
        if (_root != null)
        {
            _root.Evaluate();
        }
    }



    //void OnAnimatorMove()
    //{
    //    // 1. 현재 프레임에서 애니메이션이 이동해야 할 거리(Delta)를 가져옴
    //    // 2. 여기에 사용자가 원하는 % (multiplier)를 곱함
    //    Vector3 desiredVelocity = (_anim.deltaPosition / Time.deltaTime);// * movementMultiplier;

    //    // 3. 에이전트에게 "이 속도로 움직여라"라고 직접 명령
    //    // 이렇게 하면 애니메이션 재생 속도에 맞춰 에이전트가 움직이므로 싱크가 절대 깨지지 않음
    //    _agent.velocity = desiredVelocity;
    //}



    private void OnWorkTriggered()
    {
        if (_blackboard.isEscaping) return;
        Debug.Log("OnWorkTriggered");
        _blackboard.isForceBehavior = false;
        _blackboard.hasToWork = true;
    }



    private void OnFrenzyTriggered()
    {
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
                () => 40, // Walk 확률 40%
                () => 25, // Jog 확률 25%
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
    //    // 1. 개별 액션 시퀀스 정의
    //    Sequence angrySeq = new Sequence(new List<BT_Node> { new PlayOnceAnim("Angry", "Angry", 1) });
    //    Sequence clapSeq = new Sequence(new List<BT_Node> { new PlayOnceAnim("Clap", "Clap", 1) });
    //    Sequence frustrateSeq = new Sequence(new List<BT_Node> { new PlayOnceAnim("Frustrated", "Frustrated", 1) });

    //    // 2. 아무것도 안 하고 타이핑만 계속할 상태 (대기 노드)
    //    Sequence justTyping = new Sequence(new List<BT_Node> { new Delay(() => 0.1f) });

    //    // 3. 확률 선택기 구성 (가중치 부여)
    //    RandomSelector chanceActionSelector = new RandomSelector(
    //        new List<BT_Node> { angrySeq, clapSeq, frustrateSeq, justTyping },
    //        new List<System.Func<int>> {
    //            () => 10, // 욕(분노) 10%
    //            () => 10, // 박수 10%
    //            () => 10, // 좌절 10%
    //            () => 1  // 그냥 계속 타이핑 70%
    //        }
    //    );

    //    // 4. 메인 워크 시퀀스에 조립
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
        // 동작 설계: 
        // 1. 랜덤 지점으로 이동
        // 2. 도착하면 3초간 주변 구경(Loop)
        // 3. 50% 확률로 기지개 켜기(Once), 50% 확률로 그냥 대기
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
            // 1. 적에게 접근 (사거리 안에 들어올 때까지 Running, 들어오면 Success)
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
            
            // 2. 사거리 안에서 무작위 공격 수행 (애니메이션 끝날 때까지 Running)
            //new MeleeAttackPattern(),
            
            // 3. 공격 후 잠깐의 틈 (AI가 너무 숨 가쁘게 공격하지 않도록)
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
        // 4. 전체 루트를 반복(Selector 또는 Sequence) 하도록 설정

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
            // 1. 무한 반복해야 하는 특정 비헤이비어 체크
            //new ConditionDecorator(() => _blackboard.destBehavior == BehaviorType.Escape, 
            //    // 여기에 초기화가 필요 없는 루프 로직 배치
            //    behaviorNodes[BehaviorType.Escape]
            //),

            new ConditionDecorator(() => _blackboard.destBehavior == BehaviorType.Tackle, 
                // 여기에 초기화가 필요 없는 루프 로직 배치
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

            // 2. 일반적인 비헤이비어 (매번 초기화가 필요한 그룹)
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
                // 강제 모드면 아무것도 안 하고 바로 Success (이미 결정된 행동 유지)
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



    public void UnFocusProfessorAttack()
    {
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

    //    // 가속도를 높여야 속도 변화가 즉각적으로 보입니다.
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
            // 1단계: 정지
            // UpdateState("정지", _idleSpeed);
            // yield return new WaitForSeconds(_changeInterval);

            // 2단계: 걷기
            // UpdateState("걷기", _walkSpeed);
            // yield return new WaitForSeconds(_changeInterval);

            // 3단계: 조깅
            //UpdateState("조깅", _jogSpeed);
            //yield return new WaitForSeconds(_changeInterval);

            // 4단계: 뛰기
            // UpdateState("뛰기", _runSpeed);
            // yield return new WaitForSeconds(_changeInterval);

            // 5단계: 전력질주
            // UpdateState("전력질주", _sprintSpeed);
            // yield return new WaitForSeconds(_changeInterval);
        }
    }

    private void UpdateState(string stateName, float speed)
    {
        _agent.speed = speed;
        Debug.Log($"현재 상태: {stateName} (속도: {speed})");
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
        _blackboard.isDamaged = true;
        _blackboard.isStunned = true;
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
        EscapeEvent?.Invoke(this);
        _blackboard.destSpot?.Release(this);
        gameObject.SetActive(false);
        _root = null;
    }



    private void OnDie(HitInfo hitInfo)
    {
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
        _agent.updatePosition = true;    // 에이전트가 트랜스폼을 움직이도록 허용
        _agent.updateRotation = true;    // 회전도 허용
        _anim.applyRootMotion = false;
        _anim.SetFloat("MoveSpeedScale", _moveSpeedModifier.GetFinalValue());
        _blackboard = new Blackboard(gameObject, BehaviorWeightSet, _stageSpots, _player.gameObject);
        _root = ConstructBehaviorTree();
        _root.SetBlackboard(_blackboard);
        OnWorkTriggered();
    }


    //이전
    //private void SetRagdoll(bool isActive)
    //{
    //    _anim.enabled = !isActive;
    //    _agent.enabled |= isActive;

    //    if (TryGetComponent(out Rigidbody rootRb))
    //    {
    //        //rootRb.isKinematic = isActive; // 래그돌이면 본체 물리 연산 중단
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
        _root = null;
        _agent.speed = 0;
        _anim.enabled = false;
        _characterCollider.enabled = false;

        // 래그돌 부위들을 찾아 물리 적용
        foreach (var rb in GetComponentsInChildren<Rigidbody>())
        {
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero; // 튀는 현상 방지용 초기화

            // 팁: killer의 위치로부터 반대 방향으로 아주 살짝 힘을 주면 더 자연스럽습니다.
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

    //    // 1. 모든 래그돌 리지드바디 중 피격 지점과 가장 가까운 부위를 찾습니다.
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

    //    // 2. 해당 부위에 물리 충격을 가합니다.
    //    if (closestRb != null)
    //    {
    //        // hitRotation의 forward 방향으로 힘을 전달
    //        Vector3 forceDir = hitRotation * Vector3.back;

    //        // AddForceAtPosition을 쓰면 피격 지점 기준으로 회전력까지 발생해서 더 사실적입니다.
    //        closestRb.AddForceAtPosition(forceDir * impulse, hitPoint, ForceMode.Impulse);
    //    }
    //}
}