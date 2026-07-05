using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Dancer : MonoBehaviour
{
    private BT_Node _root;
    private Blackboard _blackboard;
    private CharacterRagdoll _characterRagdoll;
    private NavMeshAgent _agent;
    private Animator _anim;
    private Collider _characterCollider;



    private void Awake()
    {
        _anim = GetComponent<Animator>();
        _agent = GetComponent<NavMeshAgent>();
        _characterCollider = GetComponent<Collider>();
        _characterRagdoll = GetComponent<CharacterRagdoll>();
    }



    private void Start()
    {
        _anim.SetFloat("MoveSpeedScale", 1);
        StartCheer();
        //_agent.acceleration = 100f;
    }



    private void Update()
    {
        if (_root != null)
        {
            _root.Evaluate();
        }
    }



    public void StartCheer()
    {
        _blackboard = new Blackboard(gameObject, null, null, null);
        _root = ConstructBehavior();
        _root.SetBlackboard(_blackboard);
    }



    private BT_Node ConstructBehavior()
    {
        //return new TakeHitReactivePattern
        //(
        //    new ParallelOR(new List<BT_Node>
        //    {
        //        new RandomSelector(
        //            new List<BT_Node>
        //            {
        //                new PlayOnceAnim("Cheer_S", "Cheer_S"),
        //                new PlayOnceAnim("Rally_S", "Rally_S"),
        //                new PlayOnceAnim("Clap_S", "Clap_S"),
        //                new PlayOnceAnim("Punch_S", "Punch_S"),
        //                new PlayOnceAnim("Jab_S", "Jab_S"),
        //            },
        //            new List<System.Func<float>>
        //            {
        //                () => 2,
        //                () => 2,
        //                () => 2,
        //                () => 1,
        //                () => 1,
        //            }),
        //        new RotateToPoint(_fightFocusPoint),
        //    })
        //);

        return new Sequence(new List<BT_Node>
        {
            new Delay(() => 8f),
            new SetAnimRootMotion(false),
            new PlayOnceAnim("SnakeHipHopDance_M", "SnakeHipHopDance_M"),
            new PlayOnceAnim("HipHopDance_M", "HipHopDance_M"),
            new PlayOnceAnim("SillyDance_M", "SillyDance_M"),
            new PlayOnceAnim("SwingDance_M", "SwingDance_M"),
            new PlayOnceAnim("YmcaDance_M", "YmcaDance_M"),
        });

    }



    private void OnDamaged(HitInfo hitInfo, float hitAmount)
    {
        _blackboard.isDamaged = true;
        _blackboard.isStunned = true;
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