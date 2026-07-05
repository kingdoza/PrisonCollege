using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class OverlapAttacker : MonoBehaviour
{
    [SerializeField] private bool _isAttacking = false;
    private HashSet<GameObject> _hitTargets = new HashSet<GameObject>();
    private GameObject _rootObject;
    //private ExplosionShacker _explosionShacker;

    [Header("Settings")]
    [SerializeField] private DamageData _damageData;
    [SerializeField] private float _hitImpulse;

    [Header("Layer Filters")]
    [SerializeField] private LayerMask _victimOnlyLayer; // 얘네는 맞기만 함 (예: Enemy)
    [SerializeField] private LayerMask _bothDamageLayer; // 닿으면 양쪽 다 데미지 (예: Trap, Destructible)
    [SerializeField] private SoundData _hitSD;


    private void Awake()
    {
        _rootObject = transform.root.gameObject;
        //_explosionShacker = GetComponent<ExplosionShacker>();
    }

    public void StartAttack()
    {
        _hitTargets.Clear();
        _isAttacking = true;
    }

    public void StopAttack() => _isAttacking = false;

    private void OnTriggerEnter(Collider other)
    {
        if (!_isAttacking) return;

        // 1. 최상위 부모 기준으로 중복 체크
        GameObject rootTarget = other.transform.root.gameObject;
        if (_hitTargets.Contains(rootTarget) || rootTarget == _rootObject) return;

        int targetLayer = other.gameObject.layer;
        bool isVictimOnly = ((1 << targetLayer) & _victimOnlyLayer) != 0;
        bool isBothDamage = ((1 << targetLayer) & _bothDamageLayer) != 0;

        if (!isVictimOnly && !isBothDamage) return; // 설정 안 된 레이어는 무시

        // 2. 공통 데이터 계산 (Contact Point 등)
        Vector3 origin = transform.GetComponent<Collider>().bounds.center;
        Vector3 contactPoint = other.ClosestPoint(origin);
        Vector3 normal = (origin - contactPoint).normalized;
        if (normal == Vector3.zero) normal = -transform.forward;

        HitInfo hitInfoToOther = new HitInfo(contactPoint, Quaternion.LookRotation(normal), _rootObject, _hitImpulse);

        // 3. 상대방 공격 (VictimOnly 또는 BothDamage일 때)
        if (other.TryGetComponent(out DamageReceiver otherReceiver))
        {
            otherReceiver.TakeEffect(_damageData, hitInfoToOther);
            _hitTargets.Add(rootTarget);
            SoundUtils.PlayScene3DSFX(_hitSD, hitInfoToOther.hitPoint);
        }

        if (isBothDamage)
        {
            DamageReceiver myReceiver = GetComponentInParent<DamageReceiver>();
            if (myReceiver)
            {
                float pushDist = 0.5f; // 약 5cm 정도 미세하게 밀어내기
                transform.root.position += normal * pushDist;
                HitInfo hitInfoToMe = new HitInfo(
                    contactPoint,
                    Quaternion.LookRotation(-normal),
                    other.gameObject,
                    _hitImpulse
                );

                // 3. 효과 적용
                //_explosionShacker.PlayShake();
                myReceiver.TakeEffect(_damageData, hitInfoToMe);
                if (other.gameObject.IsInLayerMask(Global.PLAYER_LAYER_NAME))
                {
                    SoundUtils.PlayScene2DSFX(_hitSD);
                }
                else
                {
                    SoundUtils.PlayScene3DSFX(_hitSD, hitInfoToOther.hitPoint);
                }
            }
        }
    }
}




public enum OverlapAttackType
{
    BodySlam, Tackle
}