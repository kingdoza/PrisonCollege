using DG.Tweening;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using static Global;



public enum NodeState
{
    Running, // 실행 중 (예: 목적지로 이동 중)
    Success, // 성공 (예: 목적지 도착, 조건 만족)
    Failure  // 실패 (예: 경로 없음, 조건 불만족)
}



[System.Serializable]
public abstract class BT_Node
{
    protected Blackboard _bb; // 모든 자식 노드에서 접근 가능
    public virtual void SetBlackboard(Blackboard blackboard) => _bb = blackboard;
    public virtual void Reset() { }
    public abstract NodeState Evaluate();
}



public class ConditionDecorator : BT_Node
{
    private readonly Func<bool> _condition; // 체크할 조건식
    private readonly BT_Node _child;         // 실행할 자식 노드

    public ConditionDecorator(Func<bool> condition, BT_Node child)
    {
        _condition = condition;
        _child = child;
    }

    public override void SetBlackboard(Blackboard blackboard)
    {
        base.SetBlackboard(blackboard);
        _child?.SetBlackboard(blackboard);
    }

    public override NodeState Evaluate()
    {
        if (_condition == null || _child == null) return NodeState.Failure;

        // 1. 조건을 체크한다.
        if (_condition.Invoke())
        {
            // 2. 조건이 맞으면 자식을 실행하고 그 결과를 그대로 부모에게 보고한다.
            return _child.Evaluate();
        }

        // 3. 조건이 틀리면 자식을 리셋하고 실패를 보고한다.
        _child.Reset();
        return NodeState.Failure;
    }

    public override void Reset()
    {
        _child?.Reset();
    }
}



public class ActionNode : BT_Node
{
    private readonly Action _action;
    private readonly NodeState _resultState;

    // 실행할 함수와, 종료 후 보고할 상태를 인자로 받음
    public ActionNode(Action action, NodeState resultState = NodeState.Success)
    {
        _action = action;
        _resultState = resultState;
    }

    public override NodeState Evaluate()
    {
        // 1. 주입된 함수 실행 (null 체크 포함)
        _action?.Invoke();

        // 2. 지정된 노드 상태 반환
        return _resultState;
    }
}



public class StopNode : BT_Node
{
    private readonly int _speedHash = Animator.StringToHash("MoveSpeed");

    public override NodeState Evaluate()
    {
        if (_bb.Agent != null && _bb.Agent.isOnNavMesh)
        {
            // 1. 물리적 속도 즉시 제거
            _bb.Agent.velocity = Vector3.zero;
            _bb.Agent.speed = 0;
            // 2. NavMeshAgent의 경로 계산 중지 및 정지
            _bb.Agent.isStopped = true; 
            _bb.Agent.ResetPath();

            // 3. 애니메이션 파라미터 즉시 0으로 설정 (DampTime 제거)
            _bb.Anim.SetFloat(_speedHash, 0f);
        }

        // 즉시 중지이므로 바로 Success 반환
        return NodeState.Success;
    }
}



public class SetRandomBehaveSpot : BT_Node
{
    private SpotGroup _behaveSpots;

    public SetRandomBehaveSpot(SpotGroup behaveSpots)
    {
        _behaveSpots = behaveSpots;
    }

    public override NodeState Evaluate()
    {
        // 현재 위치 주변 랜덤 좌표 계산
        BehaveSpot randomPoint = _behaveSpots.GetRandomSpotByWeight();

        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomPoint.transform.position, out hit, NAVMESH_SAMPLE_RANGE, 1))
        {
            _bb.destSpot = randomPoint;
            _bb.destPosition = hit.position; // 블랙보드에 목적지 저장
            return NodeState.Success;
        }
        return NodeState.Failure;
    }
}



public class SetBehaveSpot : BT_Node
{
    private BehaveSpot _behaveSpot;

    public SetBehaveSpot(BehaveSpot behaveSpot)
    {
        _behaveSpot = behaveSpot;
    }

    public override NodeState Evaluate()
    {
        NavMeshHit hit;
        if (NavMesh.SamplePosition(_behaveSpot.transform.position, out hit, NAVMESH_SAMPLE_RANGE, 1))
        {
            _bb.destSpot = _behaveSpot;
            _bb.destPosition = hit.position; // 블랙보드에 목적지 저장
            return NodeState.Success;
        }
        return NodeState.Failure;
    }
}



public class WaitUntilCondition : BT_Node
{
    private Func<bool> _condition;

    /// <param name="condition">true를 반환할 때까지 대기할 조건 함수</param>
    public WaitUntilCondition(Func<bool> condition)
    {
        _condition = condition;
    }

    public override NodeState Evaluate()
    {
        // 조건이 충족되었는지 확인
        if (_condition != null && _condition.Invoke())
        {
            // 조건 충족 시 Success를 반환하여 다음 노드로 넘어감
            return NodeState.Success;
        }

        // 조건이 아직 충족되지 않았다면 계속 Running 상태 유지
        return NodeState.Running;
    }
}



public class MoveToSpot : BT_Node
{
    private AttributeModifier _speedModifier;



    public MoveToSpot()
    {
        _speedModifier = AttributeSystem.Instance.StudMoveSpeedMod;
    }


    public override NodeState Evaluate()
    {
        //Debug.Log(_bb.destSpot);
        if (_bb.Agent.destination != _bb.destSpot.transform.position)
            _bb.Agent.SetSampleDestination(_bb.destSpot.transform.position, 1);
        //Debug.Log($"목적지: {_bb.destSpot.name}, 남은 거리: {_bb.Agent.remainingDistance}");

        // 목적지에 거의 도착했는지 확인
        if (!_bb.Agent.pathPending && _bb.Agent.remainingDistance <= _bb.Agent.stoppingDistance)
        {
            Debug.Log(_bb.Agent.stoppingDistance);
            _bb.Anim.SetFloat("MoveSpeed", 0);
            return NodeState.Success;
        }
        float currentSpeed = _bb.Agent.speed / _speedModifier.GetFinalValue();
        _bb.Anim.SetFloat("MoveSpeed", currentSpeed);
        return NodeState.Running; // 아직 가는 중
    }
}



public class MoveToTarget : BT_Node
{
    private AttributeModifier _speedModifier;



    public MoveToTarget()
    {
        _speedModifier = AttributeSystem.Instance.StudMoveSpeedMod;
    }




    public override NodeState Evaluate()
    {
        _bb.Agent.SetSampleDestination(_bb.targetDamageable.Position, 2);
        //Debug.Log($"목적지: {_bb.targetObject.name}, 남은 거리: {_bb.Agent.remainingDistance}");

        // 목적지에 거의 도착했는지 확인
        if (!_bb.Agent.pathPending && _bb.Agent.remainingDistance <= _bb.Agent.stoppingDistance)
        {
            _bb.Anim.SetFloat("MoveSpeed", 0);
            return NodeState.Success;
        }

        float currentSpeed = _bb.Agent.speed / _speedModifier.GetFinalValue();
        _bb.Anim.SetFloat("MoveSpeed", currentSpeed);
        return NodeState.Running; // 아직 가는 중
    }
}



public class MoveToPlayer : BT_Node
{
    private AttributeModifier _speedModifier;



    public MoveToPlayer()
    {
        _speedModifier = AttributeSystem.Instance.StudMoveSpeedMod;
    }




    public override NodeState Evaluate()
    {
        _bb.Agent.SetSampleDestination(_bb.Player.transform.position, 2);
        //Debug.Log($"목적지: {_bb.targetObject.name}, 남은 거리: {_bb.Agent.remainingDistance}");

        // 목적지에 거의 도착했는지 확인
        if (!_bb.Agent.pathPending && _bb.Agent.remainingDistance <= _bb.Agent.stoppingDistance)
        {
            _bb.Anim.SetFloat("MoveSpeed", 0);
            return NodeState.Success;
        }

        float currentSpeed = _bb.Agent.speed / _speedModifier.GetFinalValue();
        _bb.Anim.SetFloat("MoveSpeed", currentSpeed);
        return NodeState.Running; // 아직 가는 중
    }
}



//나중에 일정 주기 가동시 Time.deltaTime 보정 필요
public class RotateToSpot : BT_Node
{
    private float _rotationSpeed = STUDENT_ROTQTE_SPEED;
    private float _threshold = 0.999f; // 약 1도 이내로 정렬되면 완료



    public override NodeState Evaluate()
    {
        if (_bb.destSpot == null) return NodeState.Failure;

        // 1. 목표 회전값 계산
        Quaternion targetRot = _bb.destSpot.transform.rotation;

        // 2. 현재 각도와 목표 각도의 차이(내적) 확인
        float dot = Vector3.Dot(_bb.Avatar.forward, _bb.destSpot.transform.forward);

        // 3. 이미 정렬되어 있다면 성공 반환
        if (dot >= _threshold)
        {
            _bb.Avatar.rotation = targetRot; // 오차 보정
            return NodeState.Success;
        }

        // 4. 부모(Owner)를 부드럽게 회전
        _bb.Avatar.rotation = Quaternion.Slerp(
            _bb.Avatar.rotation,
            targetRot,
            Time.deltaTime * _rotationSpeed
        );

        return NodeState.Running;
    }
}



public class RotateToTarget : BT_Node
{
    private const float ROTATION_SPEED = 10f; // 회전 속도
    private const float FINISH_ANGLE = 5.0f;  // 이 각도 이내로 들어오면 완료

    public override NodeState Evaluate()
    {
        if (_bb.targetDamageable == null) return NodeState.Failure;

        Vector3 targetDir = _bb.targetDamageable.Position - _bb.Avatar.transform.position;
        targetDir.y = 0;

        if (targetDir.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(targetDir);
            _bb.Avatar.transform.rotation = Quaternion.Slerp(
                _bb.Avatar.transform.rotation, 
                targetRotation, 
                Time.deltaTime * 10f // 회전 속도
            );
        }

        // ParallelNode 안에서 계속 돌아야 하므로 항상 Running 반환
        return NodeState.Running;
    }
}



public class RotateToPlayer : BT_Node
{
    private const float ROTATION_SPEED = 10f; // 회전 속도
    private const float FINISH_ANGLE = 5.0f;  // 이 각도 이내로 들어오면 완료

    public override NodeState Evaluate()
    {
        if (_bb.Player == null) return NodeState.Failure;

        Vector3 targetDir = _bb.Player.transform.position - _bb.Avatar.transform.position;
        targetDir.y = 0;

        if (targetDir.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(targetDir);
            _bb.Avatar.transform.rotation = Quaternion.Slerp(
                _bb.Avatar.transform.rotation,
                targetRotation,
                Time.deltaTime * 10f // 회전 속도
            );
        }

        // ParallelNode 안에서 계속 돌아야 하므로 항상 Running 반환
        return NodeState.Running;
    }
}



public class RotateToPoint : BT_Node
{
    private const float ROTATION_SPEED = 10f; // 회전 속도
    private const float FINISH_ANGLE = 5.0f;  // 이 각도 이내로 들어오면 완료
    private Transform lookPoint;



    public RotateToPoint(Transform lookPoint)
    {
        this.lookPoint = lookPoint;
    }

    public override NodeState Evaluate()
    {
        if (lookPoint == null) return NodeState.Failure;

        Vector3 targetDir = lookPoint.position - _bb.Avatar.transform.position;
        targetDir.y = 0;

        if (targetDir.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(targetDir);
            _bb.Avatar.transform.rotation = Quaternion.Slerp(
                _bb.Avatar.transform.rotation,
                targetRotation,
                Time.deltaTime * 10f // 회전 속도
            );
        }

        // ParallelNode 안에서 계속 돌아야 하므로 항상 Running 반환
        return NodeState.Running;
    }
}



// 중간에 Interrupt 발생시, Timer 초기화 로직 필요
public class Delay : BT_Node
{
    private Func<float> _getWaitFunc; // 대기 시간을 가져올 함수
    private float _timer = 0f;
    private float _currentWaitTime = -1f; // 이번 차례에 기다려야 할 시간

    // 생성자에서 함수를 주입받음
    public Delay(Func<float> getWaitFunc)
    {
        _getWaitFunc = getWaitFunc;
    }

    public override void Reset()
    {
        _timer = 0f;
        _currentWaitTime = -1f; // 초기화하여 다음 진입 시 새로 시간을 계산하게 함

        // 대기 중단 시 애니메이션 초기화 (선택 사항)
        // if (_bb.Anim != null) _bb.Anim.SetFloat("Speed", 0f);
    }

    public override NodeState Evaluate()
    {
        // 1. 처음 진입했을 때만 대기 시간을 함수로부터 받아옴
        if (_currentWaitTime < 0f)
        {
            _currentWaitTime = _getWaitFunc != null ? _getWaitFunc() : 0f;

            // 대기 시작 시 이동 애니메이션 멈춤
            // if (_bb.Anim != null) _bb.Anim.SetFloat("Speed", 0f);
        }

        // 2. 타이머 진행
        _timer += Time.deltaTime;

        // 3. 목표 시간에 도달했는지 확인
        if (_timer >= _currentWaitTime)
        {
            Reset(); // 성공했으므로 다음을 위해 리셋
            return NodeState.Success;
        }

        return NodeState.Running;
    }
}




//public class SetRandomSpeed : BT_Node
//{
//    private Func<float> _getSpeedFunc;

//    public SetRandomSpeed(Func<float> getSpeedFunc)
//    {
//        _getSpeedFunc = getSpeedFunc;
//    }

//    public override NodeState Evaluate()
//    {
//        if (_getSpeedFunc == null) return NodeState.Failure;

//        float speed = _getSpeedFunc();
//        _bb.Agent.speed = speed;
//        return NodeState.Success;
//    }
//}



public class SetSpeed : BT_Node
{
    private Func<float> _getSpeedFunc;
    private AttributeModifier _speedModifier;

    public SetSpeed(Func<float> getSpeedFunc)
    {
        _getSpeedFunc = getSpeedFunc;
        _speedModifier = AttributeSystem.Instance.StudMoveSpeedMod;
    }

    public override NodeState Evaluate()
    {
        if (_getSpeedFunc == null) return NodeState.Failure;

        float speed = _getSpeedFunc();
        _bb.Agent.speed = speed * _speedModifier.GetFinalValue(1);
        return NodeState.Success;
    }
}



public class Accelerate : BT_Node
{
    private Func<float> _getSpeedFunc;
    private float _acceleration = 5f; // 초당 속도 변화량 (가속도)
    private readonly int _speedHash = Animator.StringToHash("MoveSpeed");

    public Accelerate(Func<float> getSpeedFunc, float acceleration = 5f)
    {
        _getSpeedFunc = getSpeedFunc;
        _acceleration = acceleration;
    }

    public override NodeState Evaluate()
    {
        if (_getSpeedFunc == null) return NodeState.Failure;

        float targetSpeed = _getSpeedFunc();
        
        // 1. 현재 에이전트의 속도값 가져오기
        float currentSpeed = _bb.Agent.speed;

        // 2. 목표 속도를 향해 부드럽게 보간 (MoveTowards는 목표치에 정확히 안착함)
        float nextSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, _acceleration * Time.deltaTime);

        // 3. 에이전트와 애니메이터에 동시에 적용
        _bb.Agent.speed = nextSpeed;
        _bb.Anim.SetFloat(_speedHash, nextSpeed);

        // 4. 목표 속도에 충분히 도달했으면 Success, 아니면 계속 가감속 중이므로 Running
        if (Mathf.Approximately(nextSpeed, targetSpeed))
        {
            return NodeState.Success;
        }

        return NodeState.Running;
    }
}



public class PlayLoopAnim : BT_Node
{
    private string _boolName;
    private float _duration;
    private float _timer = 0f;
    private int _layer; // 레이어 정보 추가

    public PlayLoopAnim(string boolName, float duration, int layer = 0)
    {
        _boolName = boolName;
        _duration = duration;
        _layer = layer;
    }

    public override void Reset()
    {
        _timer = 0f;
        if (_bb.Anim != null) _bb.Anim.SetBool(_boolName, false);
    }

    public override NodeState Evaluate()
    {
        if (_timer == 0f)
        {
            if (_bb.Anim != null) _bb.Anim.SetBool(_boolName, true);
        }

        _timer += Time.deltaTime;

        // 나중에 필요하다면 여기서 _layer를 사용해 특정 상태인지 확인할 수 있습니다.
        // var stateInfo = bb.Anim.GetCurrentAnimatorStateInfo(_layer);

        if (_timer >= _duration)
        {
            Reset();
            return NodeState.Success;
        }

        return NodeState.Running;
    }
}



//public class PlayOnceAnim : BT_Node
//{
//    private string _triggerName;
//    private string _stateName;   // 애니메이터에 설정된 스테이트 이름
//    private int _layer;
//    private bool _triggered = false;

//    public PlayOnceAnim(string triggerName, string stateName, int layer = 0)
//    {
//        _triggerName = triggerName;
//        _stateName = stateName;
//        _layer = layer;
//    }

//    public override void Reset()
//    {
//        _triggered = false;
//    }

//    //public override NodeState Evaluate()
//    //{
//    //    var stateInfo = _bb.Anim.GetCurrentAnimatorStateInfo(_layer);

//    //    // 1. 트리거 실행
//    //    if (!_triggered)
//    //    {
//    //        _bb.Anim.SetTrigger(_triggerName);
//    //        _triggered = true;
//    //        return NodeState.Running;
//    //    }

//    //    // 2. 애니메이션이 목표 스테이트에 있고, 한 바퀴 다 돌았는지 확인
//    //    // IsName은 스테이트 이름 혹은 "Base Layer.StateName" 형태여야 할 수 있습니다.
//    //    if (stateInfo.IsName(_stateName))
//    //    {
//    //        if (stateInfo.normalizedTime >= 0.95f)
//    //        {
//    //            Reset();
//    //            return NodeState.Success;
//    //        }
//    //    }
//    //    else if (_triggered && !_bb.Anim.IsInTransition(_layer))
//    //    {
//    //        // 트리거는 당겼는데 아직 스테이트 진입도 안 했고 트랜지션 중도 아니라면 대기
//    //        return NodeState.Running;
//    //    }

//    //    return NodeState.Running;
//    //}


//    public override NodeState Evaluate()
//    {
//        var stateInfo = _bb.Anim.GetCurrentAnimatorStateInfo(_layer);
//        bool isInTransition = _bb.Anim.IsInTransition(_layer);

//        // 1. 트리거 실행
//        if (!_triggered)
//        {
//            _bb.Anim.SetTrigger(_triggerName);
//            _triggered = true;
//            return NodeState.Running;
//        }

//        // 2. 현재 상태가 목표한 애니메이션 스테이트인 경우
//        if (stateInfo.IsName(_stateName))
//        {
//            // 애니메이션 완료 체크 (95% 이상 진행 시 성공)
//            if (stateInfo.normalizedTime >= 0.95f)
//            {
//                Reset();
//                return NodeState.Success;
//            }
//            return NodeState.Running;
//        }

//        // 3. 중단(Interrupt) 확인 로직
//        // 트리거를 이미 당겼고(triggered), 목표 스테이트도 아닌데(2번 통과 못함), 
//        // 현재 다른 스테이트로 전환 중(Transition)도 아니라면? -> "다른 곳으로 튕겨 나갔음"
//        if (_triggered && !isInTransition)
//        {
//            // 목표했던 스테이트가 아닌 다른 스테이트에 머물고 있다면 실패로 간주
//            Debug.Log($"[PlayOnceAnim] {_stateName} 중단됨 (현재 상태: {stateInfo.fullPathHash})");
//            Reset();
//            return NodeState.Failure;
//        }

//        return NodeState.Running;
//    }
//}



public class PlayOnceAnim : BT_Node
{
    private string _triggerName;
    private string _stateName;
    private int _layer;
    private bool _triggered = false;
    private bool _enteredState = false; // ★ 목표 스테이트에 진입했는지 확인용

    public PlayOnceAnim(string triggerName, string stateName, int layer = 0)
    {
        _triggerName = triggerName;
        _stateName = stateName;
        _layer = layer;
    }

    public override void Reset()
    {
        _triggered = false;
        _enteredState = false;
    }

    //public override NodeState Evaluate()
    //{
    //    var stateInfo = _bb.Anim.GetCurrentAnimatorStateInfo(_layer);
    //    bool isInTransition = _bb.Anim.IsInTransition(_layer);

    //    // 1. 트리거 실행
    //    if (!_triggered)
    //    {
    //        _bb.Anim.SetTrigger(_triggerName);
    //        _triggered = true;
    //        return NodeState.Running;
    //    }

    //    // 2. 현재 목표 스테이트 재생 중인지 확인
    //    bool isAtTarget = stateInfo.IsName(_stateName) || stateInfo.IsName("Base Layer." + _stateName);

    //    if (isAtTarget)
    //    {
    //        _enteredState = true; // 일단 한 번이라도 들어왔으면 체크

    //        // 95% 이상 돌았거나, 이미 다음 모션으로 '나가는' 트랜지션 중이라면 성공!
    //        if (stateInfo.normalizedTime >= 0.95f || isInTransition)
    //        {
    //            Reset();
    //            return NodeState.Success;
    //        }
    //        return NodeState.Running;
    //    }

    //    // 3. 목표 스테이트는 아니지만, 트랜지션 중이라면 일단 기다림
    //    if (isInTransition)
    //    {
    //        return NodeState.Running;
    //    }

    //    // 4. [수정된 로직] 이미 목표 스테이트에 들어갔다 나왔는데, 
    //    // 트랜지션도 끝나고 다른 스테이트(Idle 등)라면? -> 성공으로 간주하고 탈출
    //    if (_enteredState)
    //    {
    //        Reset();
    //        return NodeState.Success;
    //    }

    //    // 5. 트리거는 당겼는데 아예 목표 스테이트에 구경도 못 해보고 딴 데로 갔을 때만 실패
    //    if (_triggered && !isInTransition)
    //    {
    //        // 진짜 예외 상황일 때만 로그 출력
    //        Debug.Log($"[PlayOnceAnim] {_stateName} 진입 실패 (예기치 못한 중단)");
    //        Reset();
    //        return NodeState.Failure;
    //    }

    //    return NodeState.Running;
    //}



    public override NodeState Evaluate()
    {
        var stateInfo = _bb.Anim.GetCurrentAnimatorStateInfo(_layer);
        bool isInTransition = _bb.Anim.IsInTransition(_layer);

        if (!_triggered)
        {
            // 이미 트랜지션 중이면 트리거를 쏘지 않고 다음 프레임 대기
            if (isInTransition) return NodeState.Running;

            _bb.Anim.SetTrigger(_triggerName);
            _triggered = true;
            return NodeState.Running; // 트리거 쏜 프레임은 무조건 Running
        }

        bool isAtTarget = stateInfo.IsName(_stateName) || stateInfo.IsName("Base Layer." + _stateName);

        if (isAtTarget)
        {
            _enteredState = true;
            if (stateInfo.normalizedTime >= 0.95f || isInTransition)
            {
                Reset();
                return NodeState.Success;
            }
            return NodeState.Running;
        }

        if (isInTransition) return NodeState.Running;

        if (_enteredState)
        {
            Reset();
            return NodeState.Success;
        }

        // [중요] 트리거를 쏜 직후라면, 최소한 몇 프레임은 '실패'라고 단정짓지 말고 대기
        // 억지로 Failure를 반환해서 루프를 터뜨리지 않게 합니다.
        return NodeState.Running;
    }
}



public class SetAnimRootMotion : BT_Node
{
    private bool _useRootMotion;

    public SetAnimRootMotion(bool useRootMotion)
    {
        _useRootMotion = useRootMotion;
    }

    public override NodeState Evaluate()
    {
        _bb.Anim.applyRootMotion = _useRootMotion;
        return NodeState.Success;
    }
}



public class SetAnimBool : BT_Node
{
    private string _paramName;
    private bool _value;

    public SetAnimBool(string paramName, bool value)
    {
        _paramName = paramName;
        _value = value;
    }

    public override NodeState Evaluate()
    {
        _bb.Anim.SetBool(_paramName, _value);
        return NodeState.Success;
    }
}



public class SetAttackTarget : BT_Node
{
    private Func<GameObject> _targetSelector;
    private GameObject _targetObject;
    private DamageReceiver _targetDamageable;

    public SetAttackTarget(Func<GameObject> targetSelector)
    {
        _targetSelector = targetSelector;
    }

    public override NodeState Evaluate()
    {
        if (_targetSelector == null)
        {
            Debug.LogWarning("SetAttackTarget: Target GameObject is null.");
            return NodeState.Failure;
        }

        // 1. 타겟으로부터 IDamageable 인터페이스 추출
        _targetObject = _targetSelector.Invoke();
        _targetDamageable = _targetObject.GetComponent<DamageReceiver>();

        // 2. 공격 가능한 대상인지 검사 (인터페이스 존재 여부 및 생존 여부)
        if (_targetDamageable != null && _targetDamageable.CanEffect)
        {
            // 3. 블랙보드에 타겟 정보 저장 (이후 Chase, Attack 노드에서 사용)
            _bb.destSpot?.Release(_bb.Avatar.GetComponent<PostStudent>());
            _bb.targetObject = _targetObject;
            _bb.targetDamageable = _targetDamageable;

            return NodeState.Success;
        }

        // 공격 불가능한 대상인 경우
        return NodeState.Failure;
    }
}




//public class OverrideAttackTarget : BT_Node
//{
//    private Func<GameObject> _getTargetFunc;
//    private DamageReceiver _targetDamageable;

//    /// <summary>
//    /// 타겟을 반환하는 함수를 인자로 받습니다.
//    /// 예: () => _bb.coopData.target
//    /// </summary>
//    public OverrideAttackTarget(Func<GameObject> getTargetFunc)
//    {
//        _getTargetFunc = getTargetFunc;
//    }

//    public override NodeState Evaluate()
//    {
//        // 1. 함수를 실행하여 현재 타겟을 가져옴
//        GameObject currentTarget = _getTargetFunc?.Invoke();

//        if (currentTarget == null)
//        {
//            // 타겟이 없다면 블랙보드 정보도 비워주고 실패 반환
//            _bb.targetObject = null;
//            _bb.targetDamageable = null;
//            return NodeState.Failure;
//        }

//        // 2. 공격 가능한 대상인지 확인 (DamageReceiver 추출)
//        _targetDamageable = currentTarget.GetComponent<DamageReceiver>();

//        if (_targetDamageable != null && _targetDamageable.CanEffect)
//        {
//            // 3. 블랙보드에 실시간 타겟 정보 주입
//            _bb.targetObject = currentTarget;
//            _bb.targetDamageable = _targetDamageable;

//            // Debug.Log($"[BT] 타겟 오버라이드 완료: {currentTarget.name}");
//            return NodeState.Success;
//        }

//        // 공격 불가능한 상태(이미 사망 등)라면 실패
//        return NodeState.Failure;
//    }
//}



public class OverrideAttackTarget : BT_Node
{
    private Func<GameObject> _getTargetFunc;
    private DamageReceiver _currentTargetDR;

    public OverrideAttackTarget(Func<GameObject> getTargetFunc)
    {
        _getTargetFunc = getTargetFunc;
    }

    public override NodeState Evaluate()
    {
        GameObject newTarget = _getTargetFunc?.Invoke();

        // 1. 타겟이 바뀌었거나 null이 된 경우 기존 이벤트 해제
        if (_bb.targetObject != newTarget)
        {
            UnsubscribeCurrent();
        }

        if (newTarget == null)
        {
            ClearTarget();
            return NodeState.Failure;
        }

        // 2. 새로운 타겟 설정 및 이벤트 구독
        if (_bb.targetObject == null)
        {
            var dr = newTarget.GetComponent<DamageReceiver>();
            if (dr != null && dr.CanEffect)
            {
                _bb.targetObject = newTarget;
                _bb.targetDamageable = dr;
                _currentTargetDR = dr;

                // 타겟이 파괴(사망)되면 실행될 로직 등록
                dr.DepletedEvent.AddListener(_ => DOVirtual.DelayedCall(0.2f, () => OnTargetDepleted(), false));

                //dr.DepletedEvent.AddListener(_ => OnTargetDepleted());
            }
            else
            {
                return NodeState.Failure;
            }
        }

        return NodeState.Success;
    }

    private void OnTargetDepleted()
    {
        Debug.Log($"[BT] 타겟 {(_bb.targetObject != null ? _bb.targetObject.name : "Unknown")} 처치 완료. 참조를 제거합니다.");
        ClearTarget();
    }

    private void ClearTarget()
    {
        UnsubscribeCurrent();
        _bb.targetObject = null;
        _bb.targetDamageable = null;
    }

    private void UnsubscribeCurrent()
    {
        if (_currentTargetDR != null)
        {
            _currentTargetDR.DepletedEvent.RemoveListener(_ => OnTargetDepleted());
            _currentTargetDR = null;
        }
    }

    // 노드가 리셋될 때 안전하게 구독 해제
    public override void Reset()
    {
        UnsubscribeCurrent();
        base.Reset();
    }
}



public class ProbabilisticDodge : BT_Node
{
    private float _chance;
    private int _lastProcessedAttackID = -1;

    public ProbabilisticDodge(float chance) => _chance = chance;

    public override NodeState Evaluate()
    {
        if (_bb.targetObject == null) return NodeState.Failure;

        var targetAttackable = _bb.targetObject.GetComponent<IAttackable>();
        if (targetAttackable == null || !targetAttackable.IsAttacking) 
        {
            _lastProcessedAttackID = -1;
            return NodeState.Failure;
        }

        // 새로운 공격 세션인 경우에만 확률 계산
        if (targetAttackable.CurrentAttackID != _lastProcessedAttackID)
        {
            _lastProcessedAttackID = targetAttackable.CurrentAttackID;
            if (UnityEngine.Random.value < _chance)
            {
                _bb.Anim.SetTrigger("tDodge");
                return NodeState.Success;
            }
        }
        return NodeState.Failure;
    }
}



public class LerpLayerWeight : BT_Node
{
    private int _layerIndex;
    private float _targetWeight;
    private float _lerpSpeed;

    public LerpLayerWeight(int layerIndex, float targetWeight, float lerpSpeed = 5f)
    {
        _layerIndex = layerIndex;
        _targetWeight = targetWeight;
        _lerpSpeed = lerpSpeed;
    }

    public override NodeState Evaluate()
    {
        float currentWeight = _bb.Anim.GetLayerWeight(_layerIndex);
        
        // 목표값과 현재값의 차이가 아주 작으면 완료(Success)
        if (Mathf.Abs(currentWeight - _targetWeight) < 0.01f)
        {
            _bb.Anim.SetLayerWeight(_layerIndex, _targetWeight);
            return NodeState.Success;
        }

        // 점진적 보간
        float nextWeight = Mathf.Lerp(currentWeight, _targetWeight, Time.deltaTime * _lerpSpeed);
        _bb.Anim.SetLayerWeight(_layerIndex, nextWeight);
        
        return NodeState.Running;
    }
}



public class MeleeAttack : BT_Node
{
    private string[] _attackTriggers = { "tJab", "tHook", "tUppercut" };

    public override NodeState Evaluate()
    {
        // 이미 공격 애니메이션 재생 중이면 대기
        if (_bb.Anim.GetCurrentAnimatorStateInfo(1).IsTag("Attack"))
            return NodeState.Running;

        // 랜덤 공격 선택 및 실행
        string selected = _attackTriggers[UnityEngine.Random.Range(0, _attackTriggers.Length)];
        _bb.Anim.SetTrigger(selected);
        
        return NodeState.Success;
    }
}



public class ConditionNode : BT_Node
{
    private System.Func<bool> _condition;

    // 생성 시 판단 로직을 함수로 전달받음
    public ConditionNode(System.Func<bool> condition)
    {
        _condition = condition;
    }

    public override NodeState Evaluate()
    {
        // 조건이 참이면 Success, 거짓이면 Failure 반환
        return _condition() ? NodeState.Success : NodeState.Failure;
    }
}



public class DoSuccess : BT_Node {
    public override NodeState Evaluate() => NodeState.Success;
}



public class PrintDebug : BT_Node
{
    private string _message;
    private Color _logColor;

    // 메시지와 로그 색상을 지정할 수 있는 생성자
    public PrintDebug(string message, string color = "white")
    {
        _message = message;
        _logColor = GetColor(color);
    }

    public override NodeState Evaluate()
    {
        // 리치 텍스트를 이용해 콘솔에서 눈에 띄게 출력
        string colorHex = ColorUtility.ToHtmlStringRGB(_logColor);
        Debug.Log($"<color=#{colorHex}>[BT_Debug]: {_message}</color>");
        
        return NodeState.Success;
    }

    private Color GetColor(string color)
    {
        return color.ToLower() switch
        {
            "red" => Color.red,
            "green" => Color.green,
            "blue" => Color.blue,
            "yellow" => Color.yellow,
            _ => Color.white
        };
    }
}



public class SetRandomBehavior : BT_Node
{
    public override NodeState Evaluate()
    {
        // 1. 블랙보드에서 필요한 데이터 참조 (캐싱되어 있다고 가정)
        var weightSet = _bb.BehaviorWeightSet;

        if (weightSet == null)
        {
            Debug.LogError("블랙보드에 BehaviorWeightSet이 설정되지 않았습니다.");
            return NodeState.Failure;
        }

        BehaviorType pickedType = weightSet.GetRandomValue();
        Debug.Log($"[BT] 행동 선정됨: {pickedType}");

        if (pickedType == BehaviorType.Tackle && _bb.Avatar.DistanceTo(_bb.Player.transform) <= 5)
        {
            return NodeState.Failure;
        }

        if (pickedType == BehaviorType.None || pickedType == _bb.destBehavior || pickedType == _bb.prevBehavior)
        {
            return NodeState.Failure;
        }
        _bb.prevBehavior = _bb.destBehavior;
        _bb.destBehavior = pickedType;
        Debug.Log($"[BT] 행동 결정됨: {pickedType}");

        return NodeState.Success;
    }
}



public class ClearDestSpot : BT_Node
{
    public ClearDestSpot()
    {

    }



    public override NodeState Evaluate()
    {
        PostStudent student = _bb.Avatar.GetComponent<PostStudent>();
        _bb.destSpot?.Release(student);
        _bb.destSpot = null;
        return NodeState.Success;
    }
}



public class SetSpecificBehavior : BT_Node
{
    private BehaviorType _targetType;

    // 생성자를 통해 어떤 행동으로 설정할지 주입받습니다.
    public SetSpecificBehavior(BehaviorType type)
    {
        _targetType = type;
    }

    public override NodeState Evaluate()
    {
        // 1. 예외 조건: 현재 이미 그 행동을 하고 있거나, 이전 행동과 같다면 실패 처리 (무한 루프 방지)
        //if (_targetType == BehaviorType.None ||
        //    _targetType == _bb.destBehavior ||
        //    _targetType == _bb.prevBehavior)
        if (_targetType == BehaviorType.None ||
            _targetType == _bb.destBehavior)
        {
            return NodeState.Failure;
        }

        // 2. 특수 조건 (예: 태클인데 거리가 너무 가까우면 안 됨)
        if (_targetType == BehaviorType.Tackle && _bb.Avatar.DistanceTo(_bb.Player.transform) <= 5)
        {
            return NodeState.Failure;
        }

        // 3. 행동 전환
        _bb.prevBehavior = _bb.destBehavior;
        _bb.destBehavior = _targetType;

        Debug.Log($"[BT] 특정 행동으로 강제 설정됨: {_targetType}");

        return NodeState.Success;
    }
}



public class ClearDestBehavior : BT_Node
{
    public override NodeState Evaluate()
    {
        // 1. 현재 destBehavior를 초기화하기 전에 로그를 남기거나 이전 상태로 백업할 수 있습니다.
        // 만약 '이전 행동'을 명확히 끝내는 시점이라면 여기서 처리합니다.

        if (_bb == null)
        {
            Debug.LogError("블랙보드 참조가 없습니다.");
            return NodeState.Failure;
        }

        // 2. 행동 초기화 (None으로 설정)
        // BehaviorType.None이 정의되어 있다고 가정합니다.
        _bb.prevBehavior = _bb.destBehavior;
        _bb.destBehavior = BehaviorType.None;

        // 디버깅용 로그 (선택 사항)
        Debug.Log("[BT] destBehavior가 초기화되었습니다.");

        return NodeState.Success;
    }
}



public class FindDestSpot : BT_Node
{
    private float _sampleRange = 2.0f; // 스팟 주변에서 NavMesh를 검색할 반경
    private PostStudent _student;

    public override NodeState Evaluate()
    {
        if (_student == null)
            _student = _bb.Avatar.GetComponent<PostStudent>();
        BehaviorType targetType = _bb.destBehavior;

        _bb.destSpot?.Release(_student);
        _bb.destSpot = null;

        BehaveSpot spot = _bb.StageSpots.GetRandomSpotByType(targetType, _student);

        Debug.Log($"[{spot}]");
        if (spot != null && spot.IsUsable)
        {
            Vector3 rawPosition = spot.transform.position;
            if (NavMesh.SamplePosition(rawPosition, out NavMeshHit hit, _sampleRange, NavMesh.AllAreas))
            {
                if (_student == null)
                    return NodeState.Failure;
                //_bb.destSpot?.Release(student);
                _bb.destSpot = spot;
                _bb.destPosition = hit.position;
                Debug.Log($"FindDestSpot : {spot}");
                _bb.destSpot.Use(_student);
                return NodeState.Success;
            }
            else
            {
                Debug.LogWarning($"[FindDestSpot] {spot.name} 주변에서 유효한 NavMesh를 찾을 수 없습니다.");
                return NodeState.Failure;
            }
        }

        return NodeState.Failure;
    }
}



public class OverrideBehaveSpot : BT_Node
{
    private Func<SingleStudentSpot> _getSpotFunc;
    private Func<BehaviorType> _getTypeFunc;
    private float _sampleRange = 2.0f;

    public OverrideBehaveSpot(Func<SingleStudentSpot> getSpotFunc, Func<BehaviorType> getTypeFunc)
    {
        _getSpotFunc = getSpotFunc;
        _getTypeFunc = getTypeFunc;
    }

    public override NodeState Evaluate()
    {
        var spot = _getSpotFunc?.Invoke();
        //if (spot == _bb.coopData.spot) return NodeState.Failure;
        if (spot == null) return NodeState.Failure; // 실행 시점에 null이면 실패 처리
        if (spot == _bb.destSpot) return NodeState.Success;

        Vector3 rawPosition = spot.transform.position;
        if (NavMesh.SamplePosition(rawPosition, out NavMeshHit hit, _sampleRange, NavMesh.AllAreas))
        {
            PostStudent student = _bb.Avatar.GetComponent<PostStudent>();
            if (student == null)
                return NodeState.Failure;
            _bb.prevBehavior = _bb.destBehavior;
            _bb.destBehavior = _getTypeFunc.Invoke();

            _bb.destSpot?.Release(student);
            _bb.destSpot = spot;
            _bb.destPosition = hit.position;
            Debug.Log($"OverrideBehaveSpot : {spot}");
            _bb.destSpot.Use(student);
            return NodeState.Success;
        }
        else
        {
            Debug.LogWarning($"[OverrideBehaveSpot] {spot.name} 주변에서 유효한 NavMesh를 찾을 수 없습니다.");
            return NodeState.Failure;
        }
    }
}



public class EnumSwitchSelector<TEnum> : BT_Node where TEnum : Enum
{
    private readonly Dictionary<TEnum, BT_Node> _subTrees;
    private readonly BT_Node _defaultNode;

    // 블랙보드에서 어떤 열거형 값을 가져올지 결정하는 델리게이트
    private readonly Func<Blackboard, TEnum> _valueSelector;

    public EnumSwitchSelector(
        Func<Blackboard, TEnum> valueSelector,
        Dictionary<TEnum, BT_Node> subTrees,
        BT_Node defaultNode = null)
    {
        _valueSelector = valueSelector;
        _subTrees = subTrees;
        _defaultNode = defaultNode;
    }

    public override void SetBlackboard(Blackboard blackboard)
    {
        base.SetBlackboard(blackboard);
        foreach (var node in _subTrees.Values)
        {
            node.SetBlackboard(blackboard);
        }
        _defaultNode?.SetBlackboard(blackboard);
    }

    public override NodeState Evaluate()
    {
        TEnum currentValue = _valueSelector(_bb);

        if (_subTrees.TryGetValue(currentValue, out BT_Node node))
        {
            return node.Evaluate();
        }

        if (_defaultNode != null)
        {
            return _defaultNode.Evaluate();
        }

        return NodeState.Failure;
    }

    public override void Reset()
    {
        base.Reset();
        foreach (var node in _subTrees.Values) node.Reset();
        _defaultNode?.Reset();
    }
}



public class StopAndDisableAgentUpdate : BT_Node
{
    public override NodeState Evaluate()
    {
        if (_bb.Agent != null)
        {
            _bb.Agent.isStopped = true;       // 물리적 정지 명령
            _bb.Agent.velocity = Vector3.zero; // 남은 관성 제거
            _bb.Agent.updatePosition = false; // ★ 에이전트가 트랜스폼을 건드리지 못하게 함
            _bb.Agent.updateRotation = false; // 필요 시 회전도 고정
        }
        return NodeState.Success;
    }
}



public class EnableAgentUpdate : BT_Node
{
    public override NodeState Evaluate()
    {
        _bb.Agent.updatePosition = true;
        _bb.Agent.updateRotation = true;
        WarpToValidPathRecursive();
        //if (!_bb.Agent.isOnNavMesh)
        //{
        //    _bb.Agent.Warp(Utils.SampleNavMesh(_bb.Avatar.position, 500f));
        //}
        _bb.Agent.isStopped = false;
        return NodeState.Success;
    }


    public void WarpToValidPathRecursive()
    {
        if (_bb.Agent.isOnNavMesh) return;

        Vector3 targetPos = _bb.mySeatSpot.transform.position;
        float searchRadius = 2.0f;
        float step = 4.0f;         // 반경 확장 간격을 조금 더 넓게 잡음
        int maxAttempts = 5;       // 딱 5번만 시도
        int currentAttempt = 0;

        NavMeshHit hit;
        NavMeshPath path = new NavMeshPath();

        while (currentAttempt < maxAttempts)
        {
            currentAttempt++;

            // 1. 해당 반경 내에서 NavMesh 지점 탐색
            if (NavMesh.SamplePosition(_bb.Agent.transform.position, out hit, searchRadius, NavMesh.AllAreas))
            {
                if (NavMesh.CalculatePath(hit.position, targetPos, NavMesh.AllAreas, path))
                {
                    // [베스트] 목적지까지 완벽하게 연결된 경우 바로 워프
                    if (path.status == NavMeshPathStatus.PathComplete)
                    {
                        _bb.Agent.Warp(hit.position);
                        Debug.Log($"[성공] {currentAttempt}번째 시도(반경 {searchRadius}m)에서 완벽한 경로 발견.");
                        return;
                    }
                }
            }

            searchRadius += step;
        }


        _bb.Agent.Warp(Utils.SampleNavMesh(targetPos, 5f));
        Debug.LogWarning($"[차선] 5회 시도 내 완벽한 경로 없음. 가장 가까운 유효 메쉬 지점으로 워프.");
    }
}



public class ActivateLayerByIndex : BT_Node
{
    private readonly int _targetLayerIdx;

    /// <param name="layerIdx">활성화할 레이어 인덱스. 0이나 -1을 넣으면 모든 오버라이드 레이어를 끕니다.</param>
    public ActivateLayerByIndex(int layerIdx)
    {
        _targetLayerIdx = layerIdx;
    }

    public override NodeState Evaluate()
    {
        if (_bb.Anim == null) return NodeState.Failure;

        int layerCount = _bb.Anim.layerCount;

        // 0번(Base Layer)은 보통 1순위이므로 건드리지 않고, 1번부터 루프를 돕니다.
        for (int i = 1; i < layerCount; i++)
        {
            float weight = (i == _targetLayerIdx) ? 1f : 0f;
            _bb.Anim.SetLayerWeight(i, weight);
        }

        return NodeState.Success;
    }
}



public class FadeLayerByIndex : BT_Node
{
    private readonly int _targetLayerIdx;
    private readonly float _duration;
    private float _elapsedTime;
    private float[] _startWeights; // 시작 시점의 가중치 저장

    public FadeLayerByIndex(int targetLayerIdx, float duration = 0.5f)
    {
        _targetLayerIdx = targetLayerIdx;
        _duration = duration;
    }

    public override void Reset()
    {
        base.Reset();
        _elapsedTime = 0f;
        _startWeights = null;
    }

    public override NodeState Evaluate()
    {
        if (_bb.Anim == null) return NodeState.Failure;

        int layerCount = _bb.Anim.layerCount;

        // 1. 초기화: 시작 가중치 기록
        if (_startWeights == null)
        {
            _startWeights = new float[layerCount];
            for (int i = 1; i < layerCount; i++)
            {
                _startWeights[i] = _bb.Anim.GetLayerWeight(i);
            }
            _elapsedTime = 0f;
        }

        // 2. 시간 진행
        _elapsedTime += Time.deltaTime;
        float normalizedTime = Mathf.Clamp01(_elapsedTime / _duration);

        // 3. 모든 오버라이드 레이어 보간 실행
        for (int i = 1; i < layerCount; i++)
        {
            // 상/하반신 레이어(예: 1, 2번)를 제외하고 싶다면 i를 더 큰 숫자부터 시작하세요.
            float targetWeight = (i == _targetLayerIdx) ? 1f : 0f;
            float currentWeight = Mathf.Lerp(_startWeights[i], targetWeight, normalizedTime);

            _bb.Anim.SetLayerWeight(i, currentWeight);
        }

        // 4. 완료 판정
        if (normalizedTime >= 1f)
        {
            Reset(); // 다음 실행을 위해 리셋
            return NodeState.Success;
        }

        return NodeState.Running;
    }
}




public class ResetAnimParameters : BT_Node
{
    private string[] _excludeParams;

    // 초기화에서 제외하고 싶은 파라미터 이름들을 인자로 받을 수 있습니다.
    public ResetAnimParameters(params string[] excludeParams)
    {
        _excludeParams = excludeParams;
    }

    public override NodeState Evaluate()
    {
        if (_bb.Anim == null) return NodeState.Failure;

        foreach (var parameter in _bb.Anim.parameters)
        {
            // 제외 목록에 포함되어 있다면 스킵
            if (IsExcluded(parameter.name)) continue;

            // Bool 타입 초기화
            if (parameter.type == AnimatorControllerParameterType.Bool)
            {
                _bb.Anim.SetBool(parameter.name, false);
            }
            // Trigger 타입 초기화 (선택 사항)
            else if (parameter.type == AnimatorControllerParameterType.Trigger)
            {
                _bb.Anim.ResetTrigger(parameter.name);
            }
        }

        return NodeState.Success;
    }

    private bool IsExcluded(string name)
    {
        if (_excludeParams == null) return false;
        foreach (var p in _excludeParams)
        {
            if (p == name) return true;
        }
        return false;
    }
}