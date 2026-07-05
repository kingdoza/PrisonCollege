using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using static Utils;
using UnityEngine.UI;
using System.Linq;

public class Fighter : MonoBehaviour
{
    private BT_Node _root;
    private Blackboard _blackboard;
    private DamageReceiver _damageReceiver;
    private CharacterRagdoll _characterRagdoll;
    private NavMeshAgent _agent;
    private Animator _anim;
    private Collider _characterCollider;

    public UnityEvent<Fighter> DamageEvent = new();
    public UnityEvent<Fighter> DieEvent = new();

    private GameObject _enemyObject;

    private AttributeModifier _moveSpeedModifier;
    private List<Outline> _outlines;

    private readonly Vector3 RIGHT_GLOVE_POS = new Vector3(0.01f, 0.02f, 0.004f);
    private readonly Vector3 LEFT_GLOVE_POS = new Vector3(-0.01f, -0.02f, -0.004f);
    private readonly Quaternion GLOVE_ROT = Quaternion.Euler(new Vector3(0, 10, 80));
    private readonly Vector3 HELMET_POS = new Vector3(0, -0.1f, 0);
    private readonly Quaternion HELMET_ROT = Quaternion.Euler(new Vector3(90, 0, 0));



    private void Awake()
    {
        _outlines = GetComponentsInChildren<Outline>().ToList();
        _anim = GetComponent<Animator>();
        _agent = GetComponent<NavMeshAgent>();
        _characterCollider = GetComponent<Collider>();
        _characterRagdoll = GetComponent<CharacterRagdoll>();
        _damageReceiver = GetComponent<DamageReceiver>();
        _damageReceiver.StatDownEvent?.AddListener(OnDamaged);
        _damageReceiver.DepletedEvent?.AddListener(OnDie);
        GameObject hairObject = GetComponent<BaldModifier>().HairObject;
        Outline hairOutline = _outlines[0].CopyComponentTo(hairObject);
        _outlines.Add(hairOutline);
    }



    private void Start()
    {
        _agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
        foreach (var outline in _outlines)
        {
            outline.OutlineColor = Color.yellow;
            outline.OutlineWidth = 2f;
            outline.OutlineMode = Outline.Mode.OutlineAll;
        }
        SetOutlines(false);
        _moveSpeedModifier = AttributeSystem.Instance.StudMoveSpeedMod;
        _anim.SetFloat("MoveSpeedScale", _moveSpeedModifier.GetFinalValue());
        _agent.acceleration = 100f;
        _anim.SetLayerWeight(Global.COMBAT_LAYER_INDEX, 1);
    }



    private void Update()
    {
        if (_root != null)
        {
            _root.Evaluate();
        }
    }



    public void AttachHelmet(GameObject helmet)
    {
        Transform head = _anim.GetBoneTransform(HumanBodyBones.Head);
        helmet.transform.SetParent(head);
        //helmet.transform.localPosition = Vector3.zero;
        //helmet.transform.localRotation = Quaternion.identity;
        helmet.transform.localPosition = HELMET_POS;
        helmet.transform.localRotation = HELMET_ROT;
    }



    public void AttachLeftGlove(GameObject leftGlove)
    {
        Transform leftHand = _anim.GetBoneTransform(HumanBodyBones.LeftHand);
        leftGlove.transform.SetParent(leftHand);
        leftGlove.transform.localPosition = Vector3.zero;
        leftGlove.transform.localRotation = Quaternion.identity;
        //leftGlove.transform.localPosition = LEFT_GLOVE_POS;
        //leftGlove.transform.localRotation = GLOVE_ROT;
    }



    public void AttachRightGlove(GameObject rightGlove)
    {
        Transform rightHand = _anim.GetBoneTransform(HumanBodyBones.RightHand);
        rightGlove.transform.SetParent(rightHand);
        rightGlove.transform.localPosition = Vector3.zero;
        rightGlove.transform.localRotation = Quaternion.identity;
        //rightGlove.transform.localPosition = RIGHT_GLOVE_POS;
        //rightGlove.transform.localRotation = GLOVE_ROT;
    }



    public void StartFight(GameObject enemyObject)
    {
        Debug.Log($"Kill {enemyObject.name}!!");
        SetOutlines(false);
        _blackboard = new Blackboard(gameObject, null, null, null);
        _enemyObject = enemyObject;
        _root = ConstructBehavior();
        _root.SetBlackboard(_blackboard);
    }



    public void SetOutlines(bool active)
    {
        foreach (Outline outline in _outlines)
        {
            outline.enabled = active;
        }
    }



    private BT_Node ConstructBehavior()
    {
        return new TakeHitReactivePattern(new AttackReactivePattern
        (
            new Selector(new List<BT_Node>
            {
                new OverrideAttackTarget(() => _enemyObject),
                new Sequence(new List<BT_Node>
                {
                    new LerpLayerWeight(Global.COMBAT_LAYER_INDEX, 0, 5),
                    new SetAnimBool("Victorying", true)
                })
            })));
    }



    private void OnDamaged(HitInfo hitInfo, float hitAmount)
    {
        DamageEvent?.Invoke(this);
        _blackboard.isDamaged = true;
        _blackboard.isStunned = true;
    }


    private void OnDie(HitInfo hitInfo)
    {
        DieEvent?.Invoke(this);
        _root = null;
        _agent.speed = 0;
        _agent.enabled = false;
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
