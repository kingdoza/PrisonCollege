using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RushSpot : SingleStudentSpot
{
    [SerializeField] private ExitSpot _targetExitSpot;



    private void Awake()
    {
        LookAtTargetGate();
    }



    public void LookAtTargetGate()
    {
        if (_targetExitSpot == null) return;

        // 1. 방향 벡터 계산 (타겟 위치 - 내 위치)
        //Vector3 direction = _targetExitSpot.GatePosition - transform.position;
        Vector3 direction = _targetExitSpot.transform.position - transform.position;

        // 2. 수평 회전만 유지 (캐릭터가 앞뒤로 기울어지는 것 방지)
        direction.y = 0;

        // 3. 방향이 zero가 아닐 때만 회전 적용
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
    }



    public override void Use(PostStudent userStudent)
    {
        base.Use(userStudent);
        _targetExitSpot.Use(userStudent);
    }



    public override void Release(PostStudent userStudent)
    {
        base.Release(userStudent);
        _targetExitSpot.Release(userStudent);
    }




    public override bool IsUsable => base.IsUsable && _targetExitSpot.IsUsable && !_targetExitSpot.CanExit;
    public override BehaviorType BehaviorTypes => BehaviorType.RushThrough;
}
