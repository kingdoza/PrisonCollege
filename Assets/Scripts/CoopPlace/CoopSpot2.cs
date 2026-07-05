using DG.Tweening;
using UnityEngine;

public class CoopSpot2 : SingleStudentSpot
{
    [SerializeField] private CoopPartSpot _opponentSpot;
    [SerializeField] private SoundData _talkingSD;
    private ParticipantInfo _participantInfo1;
    private ParticipantInfo _participantInfo2;
    private BehaviorType _coopType;
    private bool _isExecuting;
    private float _remainedExecuteTime;
    private SoundEmitter _talkingEmitter;

    public override bool IsUsable => base.IsUsable && _opponentSpot.IsUsable;



    private void Awake()
    {
        _opponentSpot.ReleaseEvent.AddListener(BreakUpCoop);
        _opponentSpot.ArrivedEvent.AddListener(ParticipantArrived);
    }



    private void Update()
    {
        if (_isExecuting && _coopType != BehaviorType.Fight)
        {
            _remainedExecuteTime -= Time.deltaTime;
            if (_remainedExecuteTime < 0)
                BreakUpCoop();
        }
    }


    public override void Use(PostStudent userStudent)
    {
        base.Use(userStudent);
    }



    public override void Release(PostStudent userStudent)
    {
        base.Release(userStudent);
        BreakUpCoop();
    }



    private void ParticipantArrived(PostStudent participant)
    {
        if (_participantInfo1.actor == participant.gameObject)
        {
            _participantInfo1.isArrived = true;
            CheckExcutable();
        }
        else if (_participantInfo2.actor == participant.gameObject)
        {
            _participantInfo2.isArrived = true;
            CheckExcutable();
        }
        else
        {
            BreakUpCoop();
        }
    }



    private void CheckExcutable()
    {
        if (!_participantInfo1.isArrived || !_participantInfo2.isArrived)
        {
            return;
        }
        if (_participantInfo1 == null || _participantInfo2 == null)
        {
            BreakUpCoop();
            return;
        }
        if (IsUsable)
        {
            BreakUpCoop();
            return;
        }
        _isExecuting = true;
        if (_coopType == BehaviorType.Fight)
        {
            _participantInfo1.actor.GetComponent<PostStudent>().Blackboard.ExecuteFight2(_participantInfo2.actor);
            _participantInfo2.actor.GetComponent<PostStudent>().Blackboard.ExecuteFight2(_participantInfo1.actor);
        }
        else if (_coopType == BehaviorType.Talk)
        {
            Vector3 talkingPos = (transform.position + _opponentSpot.transform.position) / 2f + Vector3.up * 1.5f;
            _talkingEmitter = SoundUtils.PlayOwnedScene3DSFX(_talkingSD, talkingPos, true, 1, true);
            _participantInfo1.actor.GetComponent<PostStudent>().Blackboard.ExecuteTalk2();
            _participantInfo2.actor.GetComponent<PostStudent>().Blackboard.ExecuteTalk2();
        }
        else
        {
            BreakUpCoop();
        }
    }



    public override void Arrived(PostStudent student)
    {
        base.Arrived(student);
        ParticipantArrived(student);
    }



    public bool InviteParticipant(PostStudent requester, BehaviorType behaviorType, float executeTime)
    {
        int layerMask = 1 << LayerMask.NameToLayer(Global.STUDENT_LAYER_NAME);
        Collider[] potentialPartners = Physics.OverlapSphere(transform.position, 50, layerMask);

        foreach (var col in potentialPartners)
        {
            if (col.gameObject != requester.gameObject && col.TryGetComponent(out PostStudent student))
            {
                if (student.Blackboard.CanCoop == false) continue;
                _coopType = behaviorType;
                _remainedExecuteTime = executeTime;
                student.Blackboard.InviteCoop2(_opponentSpot, behaviorType);
                requester.Blackboard.InviteCoop2(this, behaviorType);
                _participantInfo1 = new(requester.gameObject);
                _participantInfo2 = new(student.gameObject);
                return true;
            }
        }
        return false;
    }



    private void BreakUpCoop()
    {
        if (_coopType == BehaviorType.None) return;
        _talkingEmitter?.StopAndReturn();
        _coopType = BehaviorType.None;
        _isExecuting = false;
        _participantInfo1?.actor.GetComponent<PostStudent>().Blackboard.SecadeCoop2();
        _participantInfo2?.actor.GetComponent<PostStudent>().Blackboard.SecadeCoop2();

        _coopType = BehaviorType.None;
        _participantInfo1 = null;
        _participantInfo2 = null;
    }


    private void OnDisable()
    {
        _talkingEmitter?.StopAndReturn();
    }

    private void OnDestroy()
    {
        _talkingEmitter?.StopAndReturn();
    }
}
