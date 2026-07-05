using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Utils;

public class MeleeWeapon : WeaponBase
{
    [Header("--- Melee ---")]
    [SerializeField] private float _attackRange = 3.5f;
    [SerializeField] private float _attackRadius = 0.7f;  // 판정 두께 (구체 반지름)
    [SerializeField] private LayerMask _hitLayer;      // 대상 레이어 (Enemy, Obstacle 등)
    [SerializeField] private LayerMask _blockLayer;
    [SerializeField] private SoundData _hitSD;
    private float _originalDamage;
    public bool IsOwnerInAir => _owner != null && _owner.GetComponent<FirstPersonController>().IsGrounded == false;
    public float JumpDamageFactor => IsOwnerInAir ? AttributeSystem.Instance.JumpDamageMod.GetFinalValue() : 1f;



    protected override void Awake()
    {
        base.Awake();
    }


    private void Start()
    {
        _weaponData = DeepCopyWeaponData(_weaponData);
        float itemFactor = AttributeSystem.Instance.MeleeDamageMod.GetFinalValue(1);
        _weaponData.effect.value *= itemFactor;
        _weaponData.hitImpulse *= itemFactor;
        _originalDamage = _weaponData.effect.value;
    }

    public override string TypeName => "근접";

    protected override void ExecuteAttack() => PerformMeleeAttack(_attackRange, _attackRadius, _hitLayer, _blockLayer);


    protected void PerformMeleeAttack(float range, float radius, LayerMask hitLayer, LayerMask blockLayer)
    {
        Transform cam = Camera.main.transform;
        Vector3 origin = cam.position;
        Vector3 direction = cam.forward;

        _lastOrigin = origin;
        _lastDirection = direction;
        _lastRange = range;
        _lastRadius = radius;
        _showGizmo = true;

        RaycastHit[] hits = Physics.SphereCastAll(origin, radius, direction, range, hitLayer | blockLayer);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (var hit in hits)
        {
            GameObject hitObj = hit.collider.gameObject;
            if (hitObj == _owner) continue;

            // 1. 유틸리티 함수: 벽 레이어 체크
            if (hitObj.IsInLayerMask(blockLayer))
            {
                Debug.Log("벽에 막힘!");
                break;
            }

            if (hit.collider.TryGetComponent(out DamageReceiver receiver))
            {
                // 2. 유틸리티 함수: 안전한 위치 및 회전값 계산
                //float totalFactor = JumpDamageFactor * AttributeSystem.Instance.MeleeDamageMod.GetFinalValue(1);
                Vector3 contactPoint = hit.GetContactPoint(origin);
                Vector3 normal = hit.GetNormal(direction);
                HitInfo hitInfo = new HitInfo(contactPoint, Quaternion.LookRotation(normal), _owner, _weaponData.hitImpulse * JumpDamageFactor);
                _weaponData.effect.value = _originalDamage * JumpDamageFactor;
                receiver.TakeEffect(_weaponData.effect, hitInfo);
                SoundUtils.PlayScene2DSFX(_hitSD);
                CameraShaker.Instance.DoMeleeShake(_weaponData.effect.value * 2);
            }
        }
    }

    private Vector3 _lastOrigin;
    private Vector3 _lastDirection;
    private float _lastRange;
    private float _lastRadius;
    private bool _showGizmo = false;

    private void OnDrawGizmos()
    {
        if (!_showGizmo) return;

        Gizmos.color = Color.red; // 공격 범위 색상

        // 1. 시작 지점의 구체
        Gizmos.DrawWireSphere(_lastOrigin, _lastRadius);

        // 2. 실제 Cast된 거리 계산 (히트되지 않았을 때를 대비해 range만큼 그림)
        Vector3 endPoint = _lastOrigin + _lastDirection * _lastRange;

        // 3. 끝 지점의 구체
        Gizmos.DrawWireSphere(endPoint, _lastRadius);

        // 4. 시작과 끝을 잇는 4개의 선 (원통 형태 시각화)
        Vector3 up = Vector3.up * _lastRadius;
        Vector3 right = Vector3.right * _lastRadius;

        // 카메라 방향에 따른 수직/수평 벡터 계산 (더 정확한 시각화용)
        Vector3 orthoUp = Vector3.Cross(_lastDirection, Vector3.right).normalized * _lastRadius;
        if (orthoUp == Vector3.zero) orthoUp = Vector3.up * _lastRadius;
        Vector3 orthoRight = Vector3.Cross(_lastDirection, orthoUp).normalized * _lastRadius;

        Gizmos.DrawLine(_lastOrigin + orthoUp, endPoint + orthoUp);
        Gizmos.DrawLine(_lastOrigin - orthoUp, endPoint - orthoUp);
        Gizmos.DrawLine(_lastOrigin + orthoRight, endPoint + orthoRight);
        Gizmos.DrawLine(_lastOrigin - orthoRight, endPoint - orthoRight);
    }
}
