using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class HitReceiver : MonoBehaviour, IHittable
{
    private Health _health;

    public bool CanHit => _health != null && !_health.IsDepleted;
    public bool IsInvincible { get; set; }
    public Vector3 Position => transform.position;

    // IHittable 인터페이스를 위해 Health 정보 노출
    public float CurrentHealth => _health != null ? _health.Current : 0;
    public float MaxHealth => _health != null ? _health.Max : 0;
    public bool IsDead => _health != null && _health.IsDepleted;
    public UnityEvent<Vector3, Quaternion, float, GameObject> DamagedEvent = new UnityEvent<Vector3, Quaternion, float, GameObject>();
    public UnityEvent<Vector3, Quaternion, float, GameObject> DeathEvent = new UnityEvent<Vector3, Quaternion, float, GameObject>();



    private void Awake()
    {
        _health = GetComponent<Health>(); // 같은 오브젝트 혹은 부모에서 Health 참조
    }



    public void TakeHit(EffectData data, Vector3 hitPoint, Quaternion hitRotation, GameObject attacker)
    {
        //if (!CanHit || IsInvincible) return;

        //// 1. 이펙트 생성 (피격 위치와 각도 활용)
        //if (data.effectVisualPrefab != null)
        //    Instantiate(data.effectVisualPrefab, hitPoint, hitRotation);

        //// 2. 효과 타입에 따른 처리
        //switch (data.type)
        //{
        //    case EffectType.Damage:
        //        _health?.Decrease(data.value);
        //        //DamagedEvent?.Invoke(hitPoint, hitRotation, data.hitImpulse, attacker);
        //        if (IsDead)
        //        {
        //            //DeathEvent?.Invoke(hitPoint, hitRotation, data.hitImpulse, attacker);
        //        }
        //        break;
        //}
    }
}
