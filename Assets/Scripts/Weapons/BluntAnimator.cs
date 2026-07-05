using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.UI.GridLayoutGroup;
using DOTweenSeq = DG.Tweening.Sequence;

public class BluntAnimator : WeaponAnimator
{
    [SerializeField] private SoundData _swingSD;
    protected override void AddAttackFrames(DOTweenSeq attackAnimSeq, System.Action attackExecution, float attackDuration)
    {
        attackAnimSeq.Append(transform.DOLocalMove(_originPos + new Vector3(0.2f, 0.2f, -0.4f), 0.25f).SetEase(Ease.OutQuad));
        attackAnimSeq.Join(transform.DOLocalRotate(_originRot + new Vector3(-20f, 60f, 0f), 0.25f).SetEase(Ease.OutQuad));

        attackAnimSeq.AppendCallback(() => SoundUtils.PlayScene2DSFX(_swingSD));
        // 2. 휘두르기 (전체의 약 15% - 매우 빠르게)
        attackAnimSeq.Append(transform.DOLocalMove(_originPos + new Vector3(-0.5f, 0f, -0.3f), 0.2f).SetEase(Ease.InExpo));
        attackAnimSeq.Join(transform.DOLocalRotate(_originRot + new Vector3(10f, -90f, -40f), 0.2f).SetEase(Ease.InExpo));

        // 타격 판정 (공격 시작 후 약 35% 시점)
        attackAnimSeq.AppendCallback(() => attackExecution.Invoke());

        float returnDuration = 0.55f;
        float earlyFinishTime = returnDuration * 0.45f;

        // 3. 복귀 (전체의 약 65%)
        attackAnimSeq.Append(transform.DOLocalMove(_originPos, 0.65f).SetEase(Ease.OutExpo));
        attackAnimSeq.Join(transform.DOLocalRotate(_originRot, 0.65f).SetEase(Ease.OutExpo));


        attackAnimSeq.InsertCallback(attackAnimSeq.Duration() - (returnDuration - earlyFinishTime), () => {
            attackAnimSeq.Complete(withCallbacks: true);
        });
    }
}
