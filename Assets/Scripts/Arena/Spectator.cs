using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Spectator : MonoBehaviour
{
    private BT_Node _root;
    private Blackboard _blackboard;
    private DamageReceiver _damageReceiver;
    private CharacterRagdoll _characterRagdoll;
    private NavMeshAgent _agent;
    private Animator _anim;
    private Collider _characterCollider;

    private Transform _fightFocusPoint;



    private void Awake()
    {
        _anim = GetComponent<Animator>();
        _agent = GetComponent<NavMeshAgent>();
        _characterCollider = GetComponent<Collider>();
        _characterRagdoll = GetComponent<CharacterRagdoll>();
        _damageReceiver = GetComponent<DamageReceiver>();
        _damageReceiver.StatDownEvent?.AddListener(OnDamaged);
        _damageReceiver.DepletedEvent?.AddListener(OnDie);
    }



    private void Start()
    {
        _anim.SetFloat("MoveSpeedScale", 1);
        //_agent.acceleration = 100f;
    }



    private void Update()
    {
        if (_root != null)
        {
            _root.Evaluate();
        }
    }



    public void StartCheer(Transform fightCenter)
    {
        _blackboard = new Blackboard(gameObject, null, null, null);
        _fightFocusPoint = fightCenter;
        _root = ConstructBehavior();
        _root.SetBlackboard(_blackboard);
    }



    private BT_Node ConstructBehavior()
    {
        return new TakeHitReactivePattern
        (
            new ParallelOR(new List<BT_Node>
            {
                new RandomSelector(
                    new List<BT_Node>
                    {
                        new PlayOnceAnim("Cheer_S", "Cheer_S"),
                        new PlayOnceAnim("Rally_S", "Rally_S"),
                        new PlayOnceAnim("Clap_S", "Clap_S"),
                        new PlayOnceAnim("Punch_S", "Punch_S"),
                        new PlayOnceAnim("Jab_S", "Jab_S"),
                    },
                    new List<System.Func<float>>
                    {
                        () => 2, 
                        () => 2,
                        () => 2,
                        () => 1,
                        () => 1,
                    }),
                new RotateToPoint(_fightFocusPoint),
            })
        );
    }



    private void OnDamaged(HitInfo hitInfo, float hitAmount)
    {
        _blackboard.isDamaged = true;
        _blackboard.isStunned = true;
        _damageReceiver.SetStatFull();
    }


    private void OnDie(HitInfo hitInfo)
    {
        _root = null;
        //_agent.speed = 0;
        //_agent.enabled = false;
        _anim.enabled = false;
        _characterCollider.enabled = false;
        _blackboard.targetDamageable = null;
        _blackboard.targetObject = null;
        StopAllCoroutines();
        //_ragdollStandup.SetRagdoll(true);
        _characterRagdoll.TriggerRagdoll();
        _characterRagdoll.ApplyBoneImpact(hitInfo.hitPoint, hitInfo.hitRotation, hitInfo.impulse);

        //Invoke(nameof(Revive), 2f);
    }
}
