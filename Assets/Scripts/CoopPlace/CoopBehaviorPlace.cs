using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CoopBehaviorPlace : MonoBehaviour
{
    [SerializeField] private Stat _maxReadyDuration;
    [SerializeField] protected CoopSpot[] _coopSpots;
    [SerializeField] private float _searchRadius = 20f;

    protected List<ParticipantInfo> _participants = new();
    private GameObject _leader;
    private CoopPhase _phase = CoopPhase.None;

    public int CurrentParticipants => _participants.Count;
    public int RequiredParticipants => _coopSpots.Length;
    public CoopPhase Phase => _phase;
    protected virtual BehaviorType RequiredBehavior => BehaviorType.None;



    protected virtual void Awake()
    {
        for(int i = 0; i < _coopSpots.Length; ++i)
        {
            if (RequiredBehavior != BehaviorType.None && _coopSpots[i].HasBehavior(RequiredBehavior) == false)
            {
                Debug.LogError("CoopSpot의 RequiredBehavior 항목 누락");
            }
            _coopSpots[i].Index = i;
            _coopSpots[i].JoinEvent.AddListener(OnJoined);
            _coopSpots[i].DisjoinEvent.AddListener(OnDisjoined);
            _coopSpots[i].ArriveEvent.AddListener(OnActorArrived);
        }
        _maxReadyDuration.Initialize();
        _maxReadyDuration.MaxReachEvent.AddListener(BreakUpCoop);
    }



    private void Update()
    {
        if (CurrentParticipants > 0 && Phase != CoopPhase.Executing)
        {
            _maxReadyDuration.Increase(Time.deltaTime);
        }
    }



    public void OnJoined(GameObject actor)
    {
        _maxReadyDuration.Initialize();
        if (Phase == CoopPhase.Executing)
        {
            BreakUpCoop();
            return;
        }
        if (!_participants.Any(p => p.actor == actor))
        {
            _participants.Add(new ParticipantInfo(actor));
            Debug.Log($"[Coop] {actor.name} 합류. (현재: {CurrentParticipants}/{RequiredParticipants})");

            // 1. 첫 번째로 들어온 사람을 리더로 설정 (필요 시)
            if (_leader == null)
            {
                //if (actor.GetComponent<PostStudent>().Blackboard.coopData.spot != null)
                //{
                //    BreakUpCoop();
                //    return;
                //}
                SetLeader(actor);
                int sendCount = RequestPartners();
                if (sendCount <= 0)
                {
                    BreakUpCoop();
                }
            }

            // 2. 인원이 다 찼다면 실행 준비 상태로 변경
            if (CurrentParticipants >= RequiredParticipants)
            {
                _phase = CoopPhase.Ready;
                Debug.Log("[Coop] 모든 참여자 모집 완료. 실행 가능!");
            }
            else
            {
                _phase = CoopPhase.Waiting;
            }
        }
    }



    private void SetLeader(GameObject leader)
    {
        _leader = leader;
        if (leader == null) return;
        PostStudent leadStudent = leader.GetComponent<PostStudent>();
        if (leadStudent == null) return;

        leadStudent.Blackboard.LeadCoop();
    }



    //public void OnDisjoined(GameObject actor)
    //{
    //    int index = _participants.FindIndex(p => p.actor == actor);
    //    if (index != -1)
    //    {
    //        _participants.RemoveAt(index);
    //        Debug.Log($"[Coop] {actor.name} 이탈. (현재: {_participants.Count}/{RequiredParticipants})");

    //        // 2. 리더가 나갔다면 리더 재설정
    //        if (_leader == actor)
    //        {
    //            SetLeader(_participants.Count > 0 ? _participants[0].actor : null);
    //        }

    //        // 2. 인원이 부족해졌으므로 상태 변경
    //        if (_phase == CoopPhase.Ready || _phase == CoopPhase.Executing)
    //        {
    //            // 이미 실행 중이었다면 중단시키거나 대기 상태로 되돌림
    //            _phase = CoopPhase.Waiting;
    //        }

    //        // 3. 인원이 비었으면 다시 모집 시도
    //        if (CurrentParticipants < RequiredParticipants)
    //        {
    //            RequestPartners();
    //        }

    //        // 4. 만약 아무도 안 남았다면 초기화
    //        if (CurrentParticipants == 0)
    //        {
    //            _phase = CoopPhase.None;
    //            SetLeader(null);
    //        }
    //    }
    //}



    public void OnDisjoined(GameObject actor)
    {
        Debug.Log($"[Coop] OnDisjoined {actor.name}");
        int index = _participants.FindIndex(p => p.actor == actor);
        if (index != -1)
        {
            _participants.RemoveAt(index);
            Debug.Log($"[Coop] {actor.name} 이탈로 인해 협동이 파기되었습니다.");

            // 누군가 한 명이라도 나갔다면 즉시 전체 해산 로직 실행
            //Invoke("BreakUpCoop", 2f);
            BreakUpCoop();
        }
    }

    protected void BreakUpCoop()
    {
        // 1. 모든 참여자(남은 인원)에게 해산 신호 전달
        foreach (var p in _participants)
        {
            if (p.actor != null && p.actor.TryGetComponent(out PostStudent student))
            {
                // 학생의 협동 데이터 초기화 및 애니메이션 리셋 (기존에 만든 SecadeCoop 활용)
                student.Blackboard.SecadeCoop();
            }
        }

        // 2. 관리자 데이터 초기화
        _participants.Clear();
        _leader = null;
        _phase = CoopPhase.None;

        Debug.Log("[Coop] 모든 데이터가 초기화되었으며 참여자들은 각자의 길을 갑니다.");
    }



    public void OnActorArrived(GameObject actor)
    {
        // 리스트를 순회하며 해당 actor의 상태를 '도착'으로 변경
        for (int i = 0; i < _participants.Count; i++)
        {
            if (_participants[i].actor == actor)
            {
                _participants[i].isArrived = true;
                break;
            }
        }

        CheckAllArrived();
    }



    private void CheckAllArrived()
    {
        if (_participants.Count < RequiredParticipants) return;

        // 모든 참여자의 isArrived가 true인지 체크
        bool allReady = _participants.All(p => p.isArrived);

        if (allReady)
        {
            Execute();
        }
    }



    private CoopSpot GetFirstUsableSpot()
    {
        for (int i = 0; i < _coopSpots.Length; i++)
        {
            if (_coopSpots[i] != null && _coopSpots[i].IsEmpty)
            {
                return _coopSpots[i];
            }
        }
        return null;
    }



    //private BehaviorType GetLeaderBehaviorType()
    //{
    //    if (_leader == null) return BehaviorType.None;
    //    PostStudent student = _leader.GetComponent<PostStudent>();
    //    if (student == null) return BehaviorType.None;

    //    return student.Blackboard.coopData.type;
    //}



    public int RequestPartners()
    {
        // 1. 모집이 필요한 인원 계산 (전체 필요 인원 - 현재 참여자 수)
        int neededCount = RequiredParticipants - CurrentParticipants;

        // 이미 인원이 다 찼거나, 내가 리더가 아니라면 요청할 권한이 없음
        if (neededCount <= 0) return 0;

        // 2. 주변 후보군 검색 (학생 레이어 마스크 적용)
        int layerMask = 1 << LayerMask.NameToLayer(Global.STUDENT_LAYER_NAME);
        Collider[] potentialPartners = Physics.OverlapSphere(transform.position, _searchRadius, layerMask);

        int sentCount = 0;
        foreach (var col in potentialPartners)
        {
            // 필요한 만큼 다 요청했으면 중단
            if (sentCount >= neededCount) break;

            // 자기 자신(리더)이거나 이미 참여 중인 학생은 제외
            if (col.gameObject == gameObject || _participants.Any(p => p.actor == col.gameObject)) continue;

            if (col.TryGetComponent(out PostStudent student))
            {
                // [방어 로직] 이미 다른 협동 중인 학생은 건너뜀
                if (student.Blackboard.CanCoop == false) continue;
                student.Blackboard.InviteCoop(GetFirstUsableSpot(), RequiredBehavior);

                Debug.Log($"[Coop] {col.name}에게 협동 참여 요청을 보냈습니다. (남은 자리: {neededCount - (sentCount + 1)})");

                sentCount++;
            }
        }

        // 4. 상태 업데이트
        if (sentCount > 0 && _phase == CoopPhase.None)
        {
            _phase = CoopPhase.Waiting;
        }
        return sentCount;
    }



    public virtual void Execute()
    {
        // 1. 실행 가능한 상태인지 최종 확인
        if (_phase != CoopPhase.Ready)
        {
            Debug.LogWarning("[Coop] 아직 모든 참여자가 모이지 않아 실행할 수 없습니다.");
            return;
        }

        // 2. 상태 전환
        _phase = CoopPhase.Executing;

        // 3. 모든 참여자에게 실행 신호 전송
        for (int i = 0; i < _participants.Count; i++)
        {
            GameObject actor = _participants[i].actor;

            if (actor.TryGetComponent(out PostStudent student))
            {
                ExecuteStudent(student);
                //student.Blackboard.ExecuteCoop();
            }
        }

        Debug.Log($"[Coop] {CurrentParticipants}명의 참여자가 협동 행동을 시작합니다!");

        // 4. (선택 사항) 만약 문이 열리거나 물체가 움직여야 한다면 여기서 호출
        // StartEnvironmentAction(); 
    }



    protected virtual void ExecuteStudent(PostStudent student)
    {
        student.Blackboard.ExecuteCoop();
    }
}



[System.Serializable]
public class ParticipantInfo
{
    public GameObject actor;
    public bool isArrived;

    public ParticipantInfo(GameObject actor)
    {
        this.actor = actor;
        this.isArrived = false;
    }
}




public enum CoopPhase { None, Waiting, Ready, Executing, Completed }