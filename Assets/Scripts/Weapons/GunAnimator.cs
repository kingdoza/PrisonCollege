using DG.Tweening;
using DOTweenSeq = DG.Tweening.Sequence;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GunAnimator : WeaponAnimator
{
    [Header("--- Recoil ---")]
    [SerializeField] private float _kickBackZ = 0.2f;
    [SerializeField] private float _kickUpX = 5f;
    [SerializeField] private float _randomMaxYaw = 1f;
    [SerializeField] private float _preDelayTime = 1f;
    protected override void AddAttackFrames(DOTweenSeq attackAnimSeq, System.Action attackExecution, float attackDuration)
    {
        // 1. 시간 배분 (공격 지속 시간을 쪼갭니다)
        // 반동은 매우 빠르게(전체 시간의 15%), 나머지는 복귀에 할당
        float recoilTime = attackDuration * 0.15f;
        float returnTime = attackDuration * 0.85f;

        // 2. 반동 수치 설정 (인스펙터 변수가 있다면 그것을 사용하세요)
        float randomYaw = Random.Range(-_randomMaxYaw, _randomMaxYaw); // 좌우 무작위 흔들림

        attackAnimSeq.AppendInterval(_preDelayTime);
        // [Step 1] 반동 (Recoil Kick)
        // 매우 빠르게 뒤로 밀려나며 총구가 들립니다.
        attackAnimSeq.Append(transform.DOLocalMoveZ(-_kickBackZ, recoilTime).SetEase(Ease.OutExpo));
        attackAnimSeq.Join(transform.DOLocalRotate(new Vector3(-_kickUpX, randomYaw, 0), recoilTime).SetEase(Ease.OutExpo));

        attackAnimSeq.AppendCallback(() => attackExecution.Invoke());

        // [Step 2] 복귀 (Recovery)
        // 남은 시간 동안 원래 위치(zero)로 부드럽게 돌아옵니다.
        // Ease.OutBack을 쓰면 복귀 후 살짝 출렁이는 반동감이 생깁니다.
        attackAnimSeq.Append(transform.DOLocalMove(Vector3.zero, returnTime).SetEase(Ease.OutBack));
        attackAnimSeq.Join(transform.DOLocalRotate(Vector3.zero, returnTime).SetEase(Ease.OutBack));
    }
}
