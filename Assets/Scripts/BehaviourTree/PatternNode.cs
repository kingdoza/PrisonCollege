using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;
using static Global;



public abstract class PatternNode : BT_Node
{
    protected BT_Node _patternRoot;

    protected PatternNode() { }

    public override void SetBlackboard(Blackboard blackboard)
    {
        base.SetBlackboard(blackboard);
        _patternRoot?.SetBlackboard(blackboard);
    }

    public override NodeState Evaluate()
    {
        return _patternRoot != null ? _patternRoot.Evaluate() : NodeState.Failure;
    }

    public override void Reset()
    {
        _patternRoot?.Reset();
    }
}



public class DefenseAttackPattern : PatternNode
{
    private int _lastProcessedAttackID = -1;

    public DefenseAttackPattern()
    {
        // 30% 회피, 40% 가드, 30% 멍때리기(피격)
        _patternRoot = new RandomSelector(new List<BT_Node> {
            new PlayOnceAnim("Dodge", "Dodge"), 
            new PlayOnceAnim("Guard", "Guard"),
            new DoSuccess() 
        }, new List<System.Func<float>> { () => 30, () => 40, () => 30 });
    }

    public override NodeState Evaluate()
    {
        var target = _bb.targetObject.GetComponent<IAttackable>();
        
        if (target != null && target.IsAttacking)
        {
            if (target.CurrentAttackID == _lastProcessedAttackID)
            {
                return NodeState.Failure; 
            }
            _lastProcessedAttackID = target.CurrentAttackID;
            return _patternRoot.Evaluate();
        }

        _lastProcessedAttackID = -1;
        return NodeState.Failure;
    }


    public override void Reset()
    {
        base.Reset();
        _lastProcessedAttackID = -1;
    }
}



public class CombatPattern : PatternNode
{
    public CombatPattern()
    {
        _patternRoot = new ParallelNode(new List<BT_Node>
        {
            new CombatApproachPattern(),
            new RotateToTarget(),
        });
    }
}



public class CombatApproachPattern : PatternNode
{
    private const float SPRINT_THRESHOLD = 3.0f;
    private const float APPROACH_RANGE = 1.4f;
    private const float ATTACK_RANGE = 1.6f;
    private bool _isAttacking = false;

    public CombatApproachPattern()
    {
        _patternRoot = new ReactiveSelector(new List<BT_Node>
        {
            new ConditionDecorator(() => _bb.isStunned,
                new Sequence(new List<BT_Node>
                {
                    new SetAnimRootMotion(true),
                    new WaitUntilCondition(() => !_bb.isDamaged),
                    //new Delay(() => UnityEngine.Random.Range(0f, 1f)),
                    //new DelayRange(0, 1),
                    new Delay(() =>
                    {
                        if (_bb.targetObject == _bb.Player)
                        {
                            return 0;
                            return UnityEngine.Random.Range(0f, 0.5f);
                        }
                        return UnityEngine.Random.Range(0f, 1f);
                    }),
                    new ActionNode(() => _bb.isStunned = false, NodeState.Success),
                })
            ),
            // 1. 전력질주 구간 (5m 이상)
            new ConditionDecorator(() => GetDistance() >= ATTACK_RANGE && !_isAttacking,
                new Sequence(new List<BT_Node>
                {
                    new SetAnimRootMotion(false),
                    new SetSpeed(() => 5.67f),
                    new ParallelNode(new List<BT_Node>
                    {
                        new LerpLayerWeight(COMBAT_LAYER_INDEX, 0f, 5f),
                        new MoveToTarget(),
                        //new RotateToTarget()
                    }),
                    new SetAnimRootMotion(false),
                })
            ),

            // 3. 최종 정지 구간 (1.5m 미만)
            new Sequence(new List<BT_Node>
            {
                // --- [공격 단계] ---
                //new ActionNode(() => _bb.Anim.SetLayerWeight(COMBAT_LAYER_INDEX, 1), NodeState.Success),
                new SetAnimRootMotion(true),
                new LerpLayerWeight(STRIKE_LAYER_INDEX, 0f, 5f),
                new LerpLayerWeight(COMBAT_LAYER_INDEX, 1f, 5f),
                new StopNode(),
                //new Delay(() => UnityEngine.Random.Range(1f, 2f)),
                //new DelayRange(1, 2f),
                new Delay(() =>
                {
                    if (_bb.targetObject == _bb.Player)
                    {
                        return 0;
                        return UnityEngine.Random.Range(0f, 0.2f);
                    }
                    return UnityEngine.Random.Range(1f, 2f);
                }),
                new ActionNode(() => _isAttacking = true, NodeState.Success), // 플래그 ON

                new ActionNode(() => _bb.soundBehavior.PlayGrunt()),
                new MeleeAttackPattern(), // 실제 주먹 휘두르는 동안

                new ActionNode(() => _isAttacking = false, NodeState.Success), // 공격 끝나자마자 플래그 OFF

                // --- [후딜레이 단계] ---
                // 이제 _isAttacking이 false이므로, 
                // 딜레이 도중 플레이어가 멀어지면 상위 Selector가 1번(추격)으로 즉시 갈아탑니다.
                //new Delay(() => UnityEngine.Random.Range(0f, 1f)),
                //new DelayRange(0, 0.1f),
                new Delay(() =>
                {
                    if (_bb.targetObject == _bb.Player)
                    {
                        return 0;
                        return UnityEngine.Random.Range(0f, 0.1f);
                    }
                    return UnityEngine.Random.Range(0f, 1f);
                }),
                new SetAnimRootMotion(false),
            })
        });
    }

    private bool IsAttacking()
    {
        return _bb.Anim.GetCurrentAnimatorStateInfo(Global.COMBAT_LAYER_INDEX).IsTag("Attack");
    }

    public override NodeState Evaluate()
    {
        return base.Evaluate();
        // 공격 사거리 안에 들어오면 패턴 성공(Success)으로 종료
        if (GetDistance() <= ATTACK_RANGE)
        {
            _bb.Agent.ResetPath();
            return NodeState.Success;
        }

        // 아직 멀다면 내부 트리(Selector) 실행
        return base.Evaluate();
    }

    private float GetDistance()
    {
        if (_bb.targetObject == null) return float.MaxValue;

        return Vector3.Distance(_bb.Avatar.transform.position, _bb.targetDamageable.Position);
    }



    public override void Reset()
    {
        base.Reset();
        _isAttacking = false;
    }
}



public class MeleeAttackPattern : PatternNode
{
    private static readonly string[] _animNames = {};
    private static readonly int[] _animProbs = {};

    public MeleeAttackPattern()
    {
        _patternRoot = new Sequence(new List<BT_Node>
        {
            new PrintDebug("MeleeAttackPattern Start"),
            //new SetAnimRootMotion(true),
            new RandomSelector(
                new List<BT_Node> {
                    new PlayOnceAnim("Punch1", "Punch1", COMBAT_LAYER_INDEX),
                    new PlayOnceAnim("Punch2", "Punch2", COMBAT_LAYER_INDEX),
                    new PlayOnceAnim("Punch3", "Punch3", COMBAT_LAYER_INDEX),
                    new PlayOnceAnim("Punch4", "Punch4", COMBAT_LAYER_INDEX),
                    new PlayOnceAnim("Punch5", "Punch5", COMBAT_LAYER_INDEX),
                    new PlayOnceAnim("Punch6", "Punch6", COMBAT_LAYER_INDEX),

                    new PlayOnceAnim("Elbow1", "Elbow1", COMBAT_LAYER_INDEX),

                    new PlayOnceAnim("Kick1", "Kick1", COMBAT_LAYER_INDEX),
                    new PlayOnceAnim("Kick2", "Kick2", COMBAT_LAYER_INDEX),
                    new PlayOnceAnim("Kick3", "Kick3", COMBAT_LAYER_INDEX),
                }
            ),
            //new SetAnimRootMotion(false),
            new PrintDebug("MeleeAttackPattern End"),
        });
    }

    public override NodeState Evaluate()
    {
        // base.Evaluate()가 RandomSelector를 실행하고, 
        // 그 안의 PlayOnceAnim이 Running/Success를 알아서 판단합니다.
        NodeState state = base.Evaluate();

        // 만약 한 사이클의 공격이 끝났다면(Success), 
        // 다음 접근을 위해 내부 상태를 Reset 해줍니다.
        if (state == NodeState.Success)
        {
            Reset();
        }

        return state;
    }
}



public class RandomSpotSelectPattern : PatternNode
{
    public RandomSpotSelectPattern()
    {
        _patternRoot = new Sequence(new List<BT_Node>
        {
            new RandomSelector(
                new List<BT_Node> {
                    new PlayOnceAnim("Punch6", "Punch6", COMBAT_LAYER_INDEX),
                    new PlayOnceAnim("Punch6", "Punch6", COMBAT_LAYER_INDEX),
                    new PlayOnceAnim("Punch6", "Punch6", COMBAT_LAYER_INDEX),
                    new PlayOnceAnim("Punch6", "Punch6", COMBAT_LAYER_INDEX),
                    new PlayOnceAnim("Punch6", "Punch6", COMBAT_LAYER_INDEX),
                    new PlayOnceAnim("Punch6", "Punch6", COMBAT_LAYER_INDEX),

                    new PlayOnceAnim("Elbow1", "Elbow1", COMBAT_LAYER_INDEX),

                    new PlayOnceAnim("Kick3", "Kick3", COMBAT_LAYER_INDEX),
                    new PlayOnceAnim("Kick3", "Kick3", COMBAT_LAYER_INDEX),
                    new PlayOnceAnim("Kick3", "Kick3", COMBAT_LAYER_INDEX)
                },
                new List<System.Func<float>> {
                    () => 50, // 잽은 자주
                    () => 10, // 훅은 보통
                    () => 10  // 어퍼컷은 가끔
                }
            ),
            //new SetAnimRootMotion(false),
            new PrintDebug("MeleeAttackPattern End"),
        });
    }
}




public class FindSpotPattern : PatternNode
{
    private const int MAX_RETRY = 3;
    private int _currentRetryCount = 0;

    public FindSpotPattern()
    {
        _patternRoot = new Selector(new List<BT_Node>
        {
            new FindDestSpot(), 
            new Sequence(new List<BT_Node>
            {
                new Delay(() => 1.0f),       // 1초 대기
                new ActionNode(() => 
                {
                    _currentRetryCount++;
                    Debug.Log($"[AI] 자리가 없어 재시도 중... ({_currentRetryCount}/{MAX_RETRY})");
                }, NodeState.Failure)
            })
        });
    }

    public override NodeState Evaluate()
    {
        if (_currentRetryCount >= MAX_RETRY)
        {
            Debug.Log("[AI] 모든 재시도 실패. 행동을 포기합니다.");
            Reset(); // 카운트 초기화
            return NodeState.Failure; // 전체 패턴 실패 -> 상위에서 다른 BehaviorType 결정 유도
        }

        // 2. 내부 트리 실행 (FindDestSpot 시도 -> 실패 시 Wait)
        NodeState state = _patternRoot.Evaluate();

        // 3. 만약 내부에서 스팟 찾기에 성공(Success)했다면 카운트 초기화
        if (state == NodeState.Success)
        {
            _currentRetryCount = 0;
            return NodeState.Success;
        }

        if (state == NodeState.Failure
        && _currentRetryCount < MAX_RETRY)
        {
            return NodeState.Running;
        }

        return state; // Running(대기 중) 또는 Success(찾음) 반환
    }

    public override void Reset()
    {
        base.Reset();
        _currentRetryCount = 0;
    }
}



public class DoorEscapePattern : PatternNode
{
    public DoorEscapePattern() 
    {
        _patternRoot = new Sequence(new List<BT_Node>
        {
            new SetAnimRootMotion(true),
            new SetAnimBool("EscapeRunning", true),
            new Delay(() => 0.5f),
            //new PlayOnceAnim("EscapeJump", "EscapeJump"),
        });
    }
}



public class WindowEscapePattern : PatternNode
{
    public WindowEscapePattern()
    {
        _patternRoot = new Sequence(new List<BT_Node>
        {
            new SetAnimRootMotion(true),
            //new SetAnimBool("EscapeRunning", true),
            new PlayOnceAnim("EscapeJump", "EscapeJump"),
            new ActionNode(() =>
            {
                Transform hipTransform = _bb.Avatar.transform.Find("Root/Hips");
                _bb.Anim.enabled = false;
                _bb.Agent.enabled = false;
                foreach (var rb in _bb.Avatar.GetComponentsInChildren<Rigidbody>())
                {
                    rb.isKinematic = false;
                    rb.linearVelocity = Vector3.zero;
                    rb.AddForce(Vector3.down * 12f, ForceMode.VelocityChange);
                    rb.AddForce((Vector3.down + _bb.Avatar.forward).normalized * 2f, ForceMode.VelocityChange);
            
                    if (rb.TryGetComponent(out Collider col))
                    {
                        col.isTrigger = false;
                    }
                }
            }),
            new Delay(() => 0.2f),
        });
    }
}



public class VentEscapePattern : PatternNode
{
    public VentEscapePattern()
    {
        _patternRoot = new Sequence(new List<BT_Node>
        {
        });
    }
}



public class EscapeTypeSelectPattern : PatternNode
{
    public EscapeTypeSelectPattern()
    {
        _patternRoot = new Selector(new List<BT_Node>
        {
            new ConditionDecorator(() => (_bb.destSpot as ExitSpot).GateType == ExitGateType.Door, new DoorEscapePattern()),
            new ConditionDecorator(() => (_bb.destSpot as ExitSpot).GateType == ExitGateType.Window, new WindowEscapePattern()),
            new ConditionDecorator(() => (_bb.destSpot as ExitSpot).GateType == ExitGateType.Vent, new VentEscapePattern()),
        });
    }
}



public class RushThroughPattern : PatternNode
{
    public RushThroughPattern()
    {
        _patternRoot = new Sequence(new List<BT_Node>
        {
            new PrintDebug("RushThroughPattern"),
            new SetRandomSpeedPattern(),
            //new SetSpeed(() => PostStudent._walkSpeed),
            new MoveToSpot(),
            new RotateToSpot(),
            new StopAndDisableAgentUpdate(),
            new SetAnimRootMotion(true),
            new SetAnimBool("Rush", true),
            //new Delay(() => 1.1f),
            new DelayRange(4, 6),
            //new SetAnimRootMotion(true),
            new ActionNode(() => {
                var attacker = _bb.Avatar.GetComponent<PostStudent>().GetOverlapAttacker(OverlapAttackType.BodySlam);
                attacker.StartAttack();
            }, NodeState.Success),
            new PlayOnceAnim("RushStart", "RushStart"),
            new ActionNode(null, NodeState.Running),
        });
    }
}



public class CoopPattern : PatternNode
{
    public CoopPattern()
    {
        // 협동 패턴 루트 구성 예시
        _patternRoot = new Sequence(new List<BT_Node>
        {
            //new ConditionNode(() => !_bb.coopData.isLeader ||_bb.coopData.spot != _bb.destSpot),
            new ActionNode(() => 
            {
                PostStudent student = _bb.Avatar.GetComponent<PostStudent>();
                student?.HideAllAnimAttachments();
                student?.StopAllOverlapAttackers();
            }),
            new EnableAgentUpdate(),
            new LerpLayerWeight(COMBAT_LAYER_INDEX, 0, 10),
            new LerpLayerWeight(STRIKE_LAYER_INDEX, 0, 10),
            new OverrideBehaveSpot(() => _bb.coopData2.spot, () => _bb.coopData2.type),
            new SetAnimRootMotion(false),
            new ResetAnimParameters(),
            new SetSpeed(() => 2.43f),
            new MoveToSpot(),
            new RotateToSpot(),
            new ActionNode(() => _bb.destSpot.Arrived(_bb.Avatar.GetComponent<PostStudent>())),
            
            //new PlayWaitAnimation(),

            // 3. 실행 신호가 올 때까지 대기 (Phase가 Ready가 될 때까지)
            new WaitUntilCondition(() => _bb.coopData2.isExecuting),

            // 4. 실제 협동 애니메이션 실행
            //new SetAnimRootMotion(true),
            //new SetAnimBool("Talking", true),

            //new OverrideAttackTarget(() => _bb.coopData.targetObject),
            //new ActionNode(null, NodeState.Running),

            //new SetAnimRootMotion(false),
            //new SetAttackTarget()
            //new PlayCoopAnimationNode()

            new Selector(new List<BT_Node>
            {
                new ConditionDecorator(() => _bb.coopData2.targetObject,
                    new Sequence(new List<BT_Node>
                    {
                        new OverrideAttackTarget(() => _bb.coopData2.targetObject),
                        new ActionNode(null, NodeState.Running),
                    })
                ),

                new Sequence(new List<BT_Node>
                {
                    new RandomSelector(new List<BT_Node>
                    {
                        new SetAnimBool("Talking1", true),
                        new SetAnimBool("Talking2", true),
                        new SetAnimBool("Talking3", true),
                        new SetAnimBool("Talking4", true),
                    }),
                    new ActionNode(null, NodeState.Running),
                })
            })
        });
    }
}



public class CoopReactivePattern : PatternNode
{
    public CoopReactivePattern(BT_Node normalRoutine)
    {
        _patternRoot = new ReactiveSelector(new List<BT_Node>
        {
            new ConditionDecorator(() => _bb.coopData2.spot != null && !_bb.isForceBehavior, new CoopPattern()),
            new Sequence(new List<BT_Node> { new ActionNode(() => _bb.SecadeCoop2()), normalRoutine}),
        });
    }
}


public class SwimPattern : PatternNode
{
    public SwimPattern()
    {
        _patternRoot = new Sequence(new List<BT_Node>
        {
            //new SetAnimRootMotion(true),
            new SetAnimBool("Swimming", true),
        });
    }
}



public class SwimReactivePattern : PatternNode
{
    public SwimReactivePattern(BT_Node normalRoutine)
    {
        _patternRoot = new ReactiveSelector(new List<BT_Node>
        {
            new ConditionDecorator(() =>
            {
                float floodFillRatio = FireSuppressionSystem.Instance.FloodFillRatio;
                return (floodFillRatio > 0.99f && _bb.Anim.GetFloat("MoveSpeed") > 0 && _bb.isEscaping == false);
            }, new SwimPattern()),
            new Sequence(new List<BT_Node>
            {
                //new SetAnimRootMotion(false),
                new SetAnimBool("Swimming", false),
                normalRoutine
            }),
        });
    }
}


public class SwimOverridePattern : BT_Node
{
    private BT_Node _child;

    public SwimOverridePattern(BT_Node child)
    {
        _child = child;
    }

    public override NodeState Evaluate()
    {
        // 1. 매 틱마다 물 높이와 이동 상태 체크
        float floodRatio = FireSuppressionSystem.Instance.FloodFillRatio;
        bool isMoving = _bb.Anim.GetFloat("MoveSpeed") > 0.1f;

        // 2. 조건 만족 시 '이동 방식'만 수영으로 강제 설정
        if (floodRatio > 0.98f && isMoving)
        {
            if (!_bb.Anim.GetBool("Swimming"))
            {
                _bb.Anim.applyRootMotion = true;
                _bb.Anim.SetBool("Swimming", true);
                // 필요하다면 수영 시 이동 속도 저하
                // _bb.Agent.speed = _bb.originalSpeed * 0.5f; 
            }
        }
        else
        {
            // 물이 빠졌거나 멈췄으면 보행 상태로 복구
            if (_bb.Anim.GetBool("Swimming"))
            {
                _bb.Anim.applyRootMotion = false;
                _bb.Anim.SetBool("Swimming", false);
                // _bb.Agent.speed = _bb.originalSpeed;
            }
        }

        // 3. ★ 핵심: 원래 하려던 행동(normalRoutine)은 조건과 상관없이 계속 실행 ★
        return _child.Evaluate();
    }


    public override void SetBlackboard(Blackboard blackboard)
    {
        base.SetBlackboard(blackboard);
        _child.SetBlackboard(blackboard);
    }
    public override void Reset()
    {
        base.Reset();
        _child.Reset();
    }
}


public class AttackReactivePattern : PatternNode
{
    public AttackReactivePattern(BT_Node normalRoutine)
    {
        _patternRoot = new ReactiveSelector(new List<BT_Node>
        {
            new ConditionDecorator(() => _bb.targetDamageable != null && _bb.isEscaping == false, 
                new Sequence(new List<BT_Node>
                {
                    new ActionNode(() =>
                    {
                        PostStudent student = _bb.Avatar.GetComponent<PostStudent>();
                        student?.HideAllAnimAttachments();
                        student?.StopAllOverlapAttackers();
                    }),
                    new EnableAgentUpdate(),
                    new ResetAnimParameters(),
                    new ClearDestBehavior(),

                    //new ClearDestSpot(),
                    new CombatPattern(),
                })
            ),
            normalRoutine
        });
    }
}



public class WorkPattern : PatternNode
{
    public WorkPattern()
    {
        Sequence angrySeq = new Sequence(new List<BT_Node> { new PlayOnceAnim("Angry", "Angry", 1) });
        Sequence clapSeq = new Sequence(new List<BT_Node> { new PlayOnceAnim("Clap", "Clap", 1) });
        Sequence frustrateSeq = new Sequence(new List<BT_Node> { new PlayOnceAnim("Frustrated", "Frustrated", 1) });
        Sequence justTyping = new Sequence(new List<BT_Node> { new Delay(() => 2f) });

        // 3. 확률 선택기 구성 (가중치 부여)
        RandomSelector chanceActionSelector = new RandomSelector(
            new List<BT_Node> { angrySeq, clapSeq, frustrateSeq },
            new List<System.Func<float>> {
                () => 10, // 욕(분노) 10%
                () => 10, // 박수 10%
                () => 10, // 좌절 10%
            }
        );

        _patternRoot = new Sequence(new List<BT_Node>
        {
            new Selector(new List<BT_Node>
            {
                // 강제 모드면 아무것도 안 하고 바로 Success (이미 결정된 행동 유지)
                new ConditionDecorator(() => _bb.isForceBehavior,
                    new SetSpeed(() => 5.27f)),
                new SetRandomSpeedPattern(),
            }),
            //new SetRandomSpeedPattern(),
            new MoveToSpot(),
            new RotateToSpot(),
            new SetAnimBool("Sitting", true),
            new SetAnimBool("Typing", true),
            new ActionNode(() =>
            {
                MonitorSpot monitorSpot = _bb.destSpot as MonitorSpot;
                switch (_bb.destBehavior)
                {
                    case BehaviorType.Work:
                        monitorSpot?.TurnOnMonitor(DisplayState.Working);
                        return;
                    case BehaviorType.Hack:
                        monitorSpot?.TurnOnMonitor(DisplayState.Hacking);
                        return;
                    case BehaviorType.Game:
                        monitorSpot?.TurnOnMonitor(DisplayState.Gaming);
                        return;
                }
            }),
            new DelayRange(4, 5),
            //new ActionNode(() =>
            //{
            //    (_bb.destSpot as MonitorSpot)?.PauseMonitor();
            //}),
            //chanceActionSelector,
            //new ActionNode(() =>
            //{
            //    (_bb.destSpot as MonitorSpot)?.ResumeMonitor();
            //}),
            //new DelayRange(3, 4),
            new ActionNode(() =>
            {
                (_bb.destSpot as MonitorSpot)?.PauseMonitor();
            }),
            chanceActionSelector,
            new ActionNode(() =>
            {
                (_bb.destSpot as MonitorSpot)?.ResumeMonitor();
                //if (_bb.destBehavior == BehaviorType.Hack)
                //{
                //    float defenseProb = AttributeSystem.Instance.HackBlockChanceMod.GetFinalValue(0);
                //    float rand = UnityEngine.Random.Range(0f, 1f);
                //    if (rand < defenseProb)
                //    {
                //        StageController.Instance.HackBlocked();
                //        LabLightSystem.Instance.HackDefensed();
                //    }
                //    else
                //    {
                //        StageController.Instance.Hacked();
                //        LabLightSystem.Instance.TurnOff();
                //    }
                //}
            }),
            new DelayRange(3, 4),
            new ActionNode(() =>
            {
                (_bb.destSpot as MonitorSpot)?.ResumeMonitor();
                if (_bb.destBehavior == BehaviorType.Hack)
                {
                    float defenseProb = AttributeSystem.Instance.HackBlockChanceMod.GetFinalValue(0);
                    float rand = UnityEngine.Random.Range(0f, 1f);
                    if (rand < defenseProb)
                    {
                        StageController.Instance.HackBlocked();
                        LabLightSystem.Instance.HackDefensed();
                    }
                    else
                    {
                        StageController.Instance.Hacked();
                        LabLightSystem.Instance.TurnOff();
                    }
                }
            }),
            new DelayRange(1, 1),
            new SetAnimBool("Sitting", false),
            new SetAnimBool("Typing", false),
            new ActionNode(() => _bb.isForceBehavior = false),
        });
    }
}



public class JopSelectSeqPattern : PatternNode
{
    public JopSelectSeqPattern(BT_Node afterRoutine)
    {
        _patternRoot = new Sequence(new List<BT_Node>
        {
            new ConditionDecorator(() => _bb.destBehavior != BehaviorType.Escape,
                new SetRandomBehavior()
            ),
            afterRoutine
        });
    }
}



public class BoostReactivePattern : PatternNode
{
    public BoostReactivePattern(BT_Node normalRoutine)
    {
        _patternRoot = new ReactiveSelector(new List<BT_Node>
        {
            new ConditionDecorator(() => _bb.hasToWork && !_bb.isForceBehavior && _bb.isEscaping == false,
                new Sequence(new List<BT_Node>
                {
                    //new ActionNode(() => _bb.SecadeCoop2()),
                    new PrintDebug("hasToWork"),
                    new SetSpecificBehavior(BehaviorType.Work),
                    new ActionNode(() =>
                    {
                        _bb.hasToWork = false;
                        _bb.isForceBehavior = true;
                    }),
                })
            ),
            new ConditionDecorator(() => _bb.hasToFrenzy && _bb.isEscaping == false,
                new Sequence(new List<BT_Node>
                {
                    new ActionNode(() => _bb.SecadeCoop2()),
                    new PrintDebug("hasToFrenzy"),
                    new ResetAnimParameters(),
                    new SetAttackTarget(() =>_bb.Player),
                    new ActionNode(() => _bb.isForceBehavior = true),
                    new ActionNode(() => _bb.hasToFrenzy = false),
                })
            ),
            normalRoutine
        });
    }
}



public class EscapeGiveUpReactivePattern : PatternNode
{
    private bool _isTriedGiveUp = false; 

    public EscapeGiveUpReactivePattern(BT_Node normalRoutine)
    {
        //_patternRoot = new ReactiveSelector(new List<BT_Node>
        //{
        //    new ConditionDecorator(() => _bb.destBehavior == BehaviorType.Escape && _isTriedGiveUp == false && (GetDistance() <= 5) && _bb.Anim.GetLayerWeight(STRIKE_LAYER_INDEX) >= 0.99f && HasToGiveUp(), 
        //        new Sequence(new List<BT_Node>
        //        {
        //            new ClearDestBehavior(),
        //            new ActionNode(() => _isTriedGiveUp = false),
        //        })
        //    ),
        //    new Sequence(new List<BT_Node>
        //    {
        //        //new PrintDebug("normalRoutine"),
        //        //new ActionNode(() => _isTriedGiveUp = false),
        //        normalRoutine
        //    }),
        //});



        //_patternRoot = new ReactiveSelector(new List<BT_Node>
        //{
        //    new ConditionDecorator(() =>
        //        _bb.destBehavior == BehaviorType.Escape
        //        && GetDistance() <= 5
        //        && _isTriedGiveUp == false // 1. 여기서 걸러줌
        //        && _bb.Anim.GetLayerWeight(STRIKE_LAYER_INDEX) >= 0.99f,

        //        new Sequence(new List<BT_Node>
        //        {
        //            // 2. Selector를 사용하여 성공/실패 여부와 상관없이 끝까지 가게 만듦
        //            new Selector(new List<BT_Node>
        //            {
        //                // 주사위 성공 시 실행될 로직
        //                new ConditionDecorator(() => HasToGiveUp(),
        //                    new Sequence(new List<BT_Node>
        //                    {
        //                        new PrintDebug("GiveUp Success"),
        //                        new ClearDestBehavior()
        //                    })
        //                ),

        //                // 주사위가 실패(False)하더라도 Selector이므로 여기로 넘어옴
        //                // 아무것도 안 하고 Success를 반환하게 해서 부모 Sequence가 계속 진행되게 함
        //                new ActionNode(null, NodeState.Success)
        //            }),

        //            // 3. 주사위 결과가 무엇이든 Selector가 Success를 뱉으므로 무조건 실행됨
        //            new ActionNode(() => _isTriedGiveUp = true)
        //        })
        //    ),

        //    normalRoutine
        //});



        //_patternRoot = new ReactiveSelector(new List<BT_Node>
        //{
        //    new ConditionDecorator(() =>
        //        _bb.destBehavior == BehaviorType.Escape
        //        && GetDistance() <= 5
        //        && _isTriedGiveUp == false // 1. 여기서 걸러줌
        //        && _bb.Anim.GetLayerWeight(STRIKE_LAYER_INDEX) >= 0.99f,

        //        new Sequence(new List<BT_Node>
        //        {
        //            new Selector(new List<BT_Node>
        //            {
        //                // 주사위 성공 시 실행될 로직
        //                new ConditionDecorator(() => HasToGiveUp(),
        //                    new ClearDestBehavior()
        //                ),
        //                new ActionNode(null, NodeState.Success)
        //            }),
        //            new ActionNode(() => _isTriedGiveUp = true, NodeState.Failure)
        //        })
        //    ),

        //    normalRoutine
        //});

        _patternRoot = new ReactiveSelector(new List<BT_Node>
        {
            new ConditionDecorator(() =>
                _bb.destBehavior == BehaviorType.Escape
                && _bb.isEscaping == false
                && GetDistance() <= 5
                && _isTriedGiveUp == false // 1. 여기서 걸러줌
                && _bb.Anim.GetLayerWeight(STRIKE_LAYER_INDEX) >= 0.99f
                && HasToGiveUp(),

                new Sequence(new List<BT_Node>
                {
                    new ResetAnimParameters(),
                    new LerpLayerWeight(STRIKE_LAYER_INDEX, 0, 5),
                    new ClearDestBehavior(),
                    new ClearDestSpot(),
                    new ActionNode(() => _isTriedGiveUp = true, NodeState.Success)
                })
            ),

            normalRoutine
        });
    }



    private float GetDistance()
    {
        return Vector3.Distance(_bb.Player.transform.position, _bb.Avatar.position);
    }



    public override NodeState Evaluate()
    {
        if (_bb.destBehavior != BehaviorType.Escape)
        {
            _isTriedGiveUp = false;
        }

        return _patternRoot.Evaluate();
    }



    public override void Reset()
    {
        base.Reset();
        _isTriedGiveUp = false;
    }



    //private bool HasToGiveUp()
    //{
    //    //if (_isTriedGiveUp) return false;
    //    if (_bb.Anim.GetLayerWeight(STRIKE_LAYER_INDEX) <= 0.99f) return false;
    //    ExitSpot exitSpot = _bb.destSpot as ExitSpot;
    //    if (exitSpot == null) return false;
    //    float healthRatio = exitSpot.GateHealthRatio;
    //    _isTriedGiveUp = true;
    //    Debug.Log(UnityEngine.Random.value + " : " + healthRatio);
    //    Debug.Log(UnityEngine.Random.value < healthRatio);
    //    return UnityEngine.Random.value < healthRatio;
    //}



    private bool HasToGiveUp()
    {
        ExitSpot exitSpot = _bb.destSpot as ExitSpot;
        if (exitSpot == null) return false;

        float roll = UnityEngine.Random.value;
        float healthRatio = exitSpot.GateHealthRatio;
        float giveupProbability = Mathf.Lerp(0, 0.4f, healthRatio);
        bool isSuccess = roll < giveupProbability;

        if (isSuccess == false)
        {
            _isTriedGiveUp = true;
        }

        Debug.Log($"[GiveUp 주사위] 결과: {isSuccess}, 확률: {roll} / {giveupProbability}");

        return isSuccess;
    }
}



public class TakeHitPattern : PatternNode
{
    public TakeHitPattern()
    {
        _patternRoot = new Sequence(new List<BT_Node>
        {
            new ActionNode(() => _bb.soundBehavior.PlayHurt()),
            new RandomSelector(new List<BT_Node>
            {
                new PlayOnceAnim("OnHit", "OnHit", 5),
                new PlayOnceAnim("OnHit3", "OnHit3", 5),
            }),
            new ActionNode(() => _bb.isDamaged = false, NodeState.Success),
        });
    }
}



public class TakeHitReactivePattern : PatternNode
{
    public TakeHitReactivePattern(BT_Node normalRoutine)
    {
        _patternRoot = new ParallelOR(new List<BT_Node>
        {
            new ConditionDecorator(() => _bb.isDamaged && !_bb.isEscaping, new TakeHitPattern()),
            normalRoutine
        });
    }
}



//버그 : 차단막 파괴되었는데도 한번 더 때리는 경우있음, 그리고 탈출대기중인거 강화할때 버그 (둘다 랜덤)
public class TryEscapePattern : PatternNode
{
    public TryEscapePattern()
    {
        // 내부 로직 설계: Selector를 통해 조건별 분기
        Sequence escapeBehaviorSeq = new Sequence(new List<BT_Node>
        {
            new PrintDebug("TryEscapePattern"),
            new ActionNode(() => _bb.isForceBehavior = true),
            new SetRandomSpeedPattern(),
            new MoveToSpot(),
            new RotateToSpot(),
            new ReactiveSelector(new List<BT_Node>
            {
                new ConditionDecorator(() =>
                {
                   ExitSpot exitSpot = _bb.destSpot as ExitSpot;
                   return exitSpot != null && exitSpot.CanExit;
                },
                   new Sequence(new List<BT_Node>
                   {
                       new RotateToSpot(),
                       new ActionNode(() => _bb.isEscaping = true),
                       new PrintDebug("Escape success!"),
                       new StopAndDisableAgentUpdate(),
                       new FadeLayerByIndex(0, 0.2f),
                       new ActionNode(() =>
                       {
                           ExitSpot exitGate = _bb.destSpot as ExitSpot;
                           exitGate.OpenGate();
                           DOVirtual.DelayedCall(0.8f, () => _bb.Avatar.GetComponent<PostStudent>().OnEscaped(), false);
                       }, NodeState.Success),
                       new EscapeTypeSelectPattern(),
                       //new ActionNode(() => _bb.EscapeSuccessEvent?.Invoke()),
                       new ActionNode(null, NodeState.Running),
                       new ActionNode(() => _bb.isEscaping = false),
                   })
                ),
                new ConditionDecorator(() => _bb.isStunned,
                   new Sequence(new List<BT_Node>
                   {
                       new SetAnimRootMotion(true),
                       new WaitUntilCondition(() => !_bb.isDamaged),
                       //new Delay(() => UnityEngine.Random.Range(1f, 2f)),
                       new DelayRange(1, 2),
                       new ActionNode(() => _bb.isStunned = false, NodeState.Success),
                   })
                ),
                new Sequence(new List<BT_Node>
                {
                    new RotateToSpot(), 
                    // --- [공격 단계] ---
                    new LerpLayerWeight(COMBAT_LAYER_INDEX, 0, 5),
                    new LerpLayerWeight(STRIKE_LAYER_INDEX, 1, 5),
                    //new ActionNode(() => _bb.Anim.SetLayerWeight(STRIKE_LAYER_INDEX, 1), NodeState.Success),
                    new StopAndDisableAgentUpdate(),
                    new SetAnimRootMotion(true),

                    new ActionNode(() => _bb.soundBehavior.PlayGrunt()),
                    new ExitAttackPattern(), // 실제 주먹 휘두르는 동안
                    //new Delay(() => 0.25f),
                    new DelayRange(1.5f, 2f),
                
                    //new Delay(() => Time.deltaTime),
                    new SetAnimRootMotion(false),

                    new EnableAgentUpdate(),
                })
            }),
        });

        _patternRoot = new LoopUntil(() => _bb.destBehavior != BehaviorType.Escape, escapeBehaviorSeq);
    }

    public override NodeState Evaluate()
    {
        // 1. 방어 코드: destSpot이 없거나 ExitSpot이 아니면 패턴 실패
        if (_bb.destSpot == null || !(_bb.destSpot is ExitSpot))
        {
            Debug.LogError("[TryEscapePattern] 목적지가 ExitSpot이 아닙니다.");
            return NodeState.Failure;
        }

        // 2. 내부 트리(Selector) 실행
        return _patternRoot.Evaluate();
    }
}



public class ExitAttackPattern : PatternNode
{
    private static readonly string[] _animNames = { };
    private static readonly int[] _animProbs = { };

    public ExitAttackPattern()
    {
        _patternRoot = new Sequence(new List<BT_Node>
        {
            new PrintDebug("ExitAttackPattern Start"),
            //new SetAnimRootMotion(true),
            new RandomSelector(
                new List<BT_Node> {
                    new PlayOnceAnim("Punch1_z", "Punch1_z", STRIKE_LAYER_INDEX),
                    //new PlayOnceAnim("Punch2_z", "Punch2_z", STRIKE_LAYER_INDEX),
                    new PlayOnceAnim("Punch3_z", "Punch3_z", STRIKE_LAYER_INDEX),
                    new PlayOnceAnim("Kick1_z", "Kick1_z", STRIKE_LAYER_INDEX),
                },
                new List<System.Func<float>> {
                    () => 10,
                    //() => 0,
                    () => 10,
                    () => 10,
                }
            ),
            //new SetAnimRootMotion(false),
            new PrintDebug("ExitAttackPattern End"),
        });
    }

    public override NodeState Evaluate()
    {
        NodeState state = base.Evaluate();
        if (state == NodeState.Success)
        {
            Reset();
        }

        return state;
    }
}



public class TacklePattern : PatternNode
{
    private float SLIDE_RANGE = 4f;
    private bool _isTackled = false;

    public TacklePattern()
    {
        //_patternRoot = new ReactiveSelector(new List<BT_Node>
        //{
        //    // 사거리 내: 태클 실행
        //    new ConditionDecorator(() => GetDistance() <= SLIDE_RANGE && !_isTackled,
        //        new Sequence(new List<BT_Node>
        //        {
        //            new StopAndDisableAgentUpdate(),
        //            new ActionNode(() => {
        //                _isTackled = true;
        //                var attacker = _bb.Avatar.GetComponent<PostStudent>().GetOverlapAttacker(OverlapAttackType.Tackle);
        //                attacker.StartAttack();
        //            }, NodeState.Success),
        //            new SetAnimRootMotion(true),
        //            new PlayOnceAnim("Tackle", "Tackle"),
        //            new ActionNode(() => 
        //            {
        //                //_bb.Agent.Warp(_bb.Avatar.position);
        //            }),
        //            new SetAnimRootMotion(false),
        //            //new EnableAgentUpdate(),
        //            new ActionNode(() => {
        //                var attacker = _bb.Avatar.GetComponent<PostStudent>().GetOverlapAttacker(OverlapAttackType.Tackle);
        //                attacker.StopAttack();
        //            }, NodeState.Success),
        //        })
        //    ),
    
        //    // 사거리 외: 추격
        //    new Sequence(new List<BT_Node> {
        //        new SetAnimRootMotion(false),
        //        new SetSpeed(() => 6.75f),
        //        new ParallelNode(new List<BT_Node>
        //        {
        //            new MoveToPlayer(),
        //            new RotateToPlayer()
        //        }),
        //    })
        //});

        _patternRoot = new ReactiveSelector(new List<BT_Node>
        {
            new ConditionDecorator(() => GetDistance() > SLIDE_RANGE && !_isTackled,
                new Sequence(new List<BT_Node> {
                    new SetAnimRootMotion(false),
                    new SetSpeed(() => 5.67f),
                    new ParallelNode(new List<BT_Node>
                    {
                        new MoveToPlayer(),
                        new RotateToPlayer()
                    }),
                })
            ),
            new Sequence(new List<BT_Node>
            {
                //new RotateToPlayer(),
                new ActionNode(() => {
                    _isTackled = true;
                    var attacker = _bb.Avatar.GetComponent<PostStudent>().GetOverlapAttacker(OverlapAttackType.Tackle);
                    attacker.StartAttack();
                    _bb.Avatar.GetComponent<Collider>().enabled = false;
                }, NodeState.Success),
                new StopAndDisableAgentUpdate(),
                new SetAnimRootMotion(true),
                new PlayOnceAnim("Tackle", "Tackle", 0),
                new ActionNode(() =>
                {
                    _bb.Agent.Warp(_bb.Avatar.position);
                }),
                new SetAnimRootMotion(false),
                new EnableAgentUpdate(),
                new ActionNode(() => {
                    var attacker = _bb.Avatar.GetComponent<PostStudent>().GetOverlapAttacker(OverlapAttackType.Tackle);
                    attacker.StopAttack();
                    _bb.Avatar.GetComponent<Collider>().enabled = true;
                }, NodeState.Success),
            })
        });
    }



    private float GetDistance()
    {
        if (_bb.Player == null) return float.MaxValue;

        return _bb.Avatar.DistanceTo(_bb.Player.transform);
    }


    public override void Reset()
    {
        base.Reset();
        _isTackled = false;
    }
}



//public class SetRandomSpeedPattern : PatternNode
//{
//    public SetRandomSpeedPattern()
//    {
//        _patternRoot = new RandomSelector(
//            new List<BT_Node> {
//                new SetSpeed(() => PostStudent._walkSpeed),
//                new SetSpeed(() => PostStudent._jogSpeed),
//                new SetSpeed(() => PostStudent._slowRunSpeed),
//                new SetSpeed(() => PostStudent._mediumRunSpeed),
//                new SetSpeed(() => PostStudent._fastRunSpeed),
//                new SetSpeed(() => PostStudent._sprintSpeed),
//            },
//            //new List<System.Func<int>> {
//            //    () => 40, // Walk 확률 40%
//            //    () => 25, // Jog 확률 25%
//            //    () => 15, // SlowRun 15%
//            //    () => 10, // MedRun 10%
//            //    () => 7,  // FastRun 7%
//            //    () => 3   // Sprint 3%
//            //}
//            new List<System.Func<int>> {
//                () => 1, // Walk 확률 40%
//                () => 0, // Jog 확률 25%
//                () => 0, // SlowRun 15%
//                () => 0, // MedRun 10%
//                () => 0,  // FastRun 7%
//                () => 0   // Sprint 3%
//            }
//        );
//    }
//}



public class SetRandomSpeedPattern : PatternNode
{
    public SetRandomSpeedPattern()
    {
        _patternRoot = new RandomSelector(
            new List<BT_Node> {
                //12개
                new SetSpeed(() => 0.69f),
                new SetSpeed(() => 0.70f),
                new SetSpeed(() => 0.71f),
                new SetSpeed(() => 0.83f),
                new SetSpeed(() => 0.90f),
                new SetSpeed(() => 0.98f),
                new SetSpeed(() => 0.99f),
                new SetSpeed(() => 1.05f),
                new SetSpeed(() => 1.09f),
                new SetSpeed(() => 1.11f),
                new SetSpeed(() => 1.45f),
                new SetSpeed(() => 1.53f),

                //6개
                new SetSpeed(() => 2.34f),
                new SetSpeed(() => 2.42f),
                new SetSpeed(() => 2.43f),
                new SetSpeed(() => 3.49f),
                new SetSpeed(() => 4.11f),
                new SetSpeed(() => 4.17f),

                //3개
                new SetSpeed(() => 5.27f),
                new SetSpeed(() => 5.67f),
                new SetSpeed(() => 6.00f),
            },
            new List<System.Func<float>>
            {
                () => 3,
                () => 3,
                () => 3,
                () => 3,
                () => 3,
                () => 3,
                () => 3,
                () => 3,
                () => 3,
                () => 3,
                () => 3,
                () => 3,

                () => StageController.Instance.GetChaosEffectedWeight(2, 5),
                () => StageController.Instance.GetChaosEffectedWeight(2, 5),
                () => StageController.Instance.GetChaosEffectedWeight(2, 5),
                () => StageController.Instance.GetChaosEffectedWeight(2, 5),
                () => StageController.Instance.GetChaosEffectedWeight(2, 5),
                () => StageController.Instance.GetChaosEffectedWeight(2, 5),

                () => StageController.Instance.GetChaosEffectedWeight(1, 50),
                () => StageController.Instance.GetChaosEffectedWeight(1, 50),
                () => StageController.Instance.GetChaosEffectedWeight(1, 50),
            }
        );
    }
}



public class DelayRange : PatternNode
{
    private float _min;
    private float _max;
    private bool _isChaosEffect;

    public DelayRange(float min, float max, bool isChaosEffect = true)
    {
        _min = min;
        _max = max;
        _isChaosEffect = isChaosEffect;
        _patternRoot = new Delay(GetDelay);
    }

    private float GetDelay()
    {
        float randDelay = UnityEngine.Random.Range(_min, _max);
        if (_isChaosEffect && StageController.Instance)
        {
            return StageController.Instance.GetChaosEffectedDelay(randDelay);
        }
        return randDelay;
    }
}