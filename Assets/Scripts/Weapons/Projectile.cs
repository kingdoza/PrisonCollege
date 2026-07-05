using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class Projectile : MonoBehaviour
{
    [SerializeField] private float _minImpactThreshold = 5.0f;
    [SerializeField] private float _lifeTime = 5.0f;
    [SerializeField] private bool _destroyOnHit = false;
    [SerializeField] private SoundData _hitSD;
    protected Rigidbody _rigidbody;

    public WeaponData WeaponData { get; set; }
    public GameObject Owner { get; set; }
    public bool IsStage { get; set; }

    // 중복 충돌을 방지하기 위한 셋 (오브젝트 참조 저장)
    private HashSet<GameObject> _hitObjects = new HashSet<GameObject>();

    protected virtual void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }

    protected virtual void Start()
    {
        //WeaponData = Utils.DeepCopyByJson(WeaponData);
        float damageFactor = IsStage ? AttributeSystem.Instance.ThrowDamageMod.GetFinalValue(1) : 1f;
        WeaponData.effect.value *= damageFactor;
        WeaponData.hitImpulse *= damageFactor;
        Destroy(gameObject, _lifeTime);
    }

    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log($"[Projectile] {collision.gameObject.name}");
        Debug.Log($"[Projectile] {WeaponData}");
        // 1. 레이어 체크 및 주인 제외
        if (collision.gameObject.IsInLayerMask(Global.STUDENT_LAYER_NAME) == false) return;
        if (collision.gameObject == Owner) return;

        // 2. ★ 이미 맞은 대상인지 체크 ★
        if (_hitObjects.Contains(collision.gameObject)) return;

        float impactVelocity = collision.relativeVelocity.magnitude;
        // 3. 충격량 체크
        float impactForce = collision.impulse.magnitude / Time.fixedDeltaTime;
        if (_rigidbody.linearVelocity.magnitude < _minImpactThreshold)
        {
            Debug.Log($"[Projectile] 충격이 너무 약함 ({_rigidbody.linearVelocity.magnitude:F2}),  무시합니다.");
            return;
        }

        // 4. 데미지 전달
        if (WeaponData == null || WeaponData.effect == null) return;
        EffectReceiver receiver = WeaponData.effect.GetActorReceiver(collision.gameObject);
        if (receiver && receiver.CanEffect)
        {
                // 목록에 추가하여 다시 맞지 않게 함
            _hitObjects.Add(collision.gameObject);

            ContactPoint contact = collision.contacts[0];
            Vector3 hitPoint = contact.point;
            Vector3 hitNormal = contact.normal;
            HitInfo hitInfo = new HitInfo(hitPoint, Quaternion.LookRotation(hitNormal), Owner, WeaponData.hitImpulse);

            receiver.TakeEffect(WeaponData.effect, hitInfo);
            SoundUtils.PlayScene3DSFX(_hitSD, hitInfo.hitPoint);

            // 만약 관통형이 아니라 첫 충돌에 바로 사라져야 한다면 아래 주석 해제
            if (_destroyOnHit)
                Destroy(gameObject); 
        }
    }



    public void ResetForce()
    {
        _rigidbody.linearVelocity = Vector3.zero;
        _rigidbody.maxAngularVelocity = 1000f;
    }


    public void AddVelocityForce(Vector3 direction, float speed)
    {
        // ForceMode.VelocityChange이므로 질량을 무시하고 즉시 speed만큼의 속도가 붙습니다.
        _rigidbody.AddForce(direction * speed, ForceMode.VelocityChange);
    }

    // 2. 회전 추가 (축 * 회전량)
    public void AddTorqueForce(Vector3 torqueAxis, float torqueAmount)
    {
        // 직접 대입 방식이므로 직관적으로 축과 양을 곱해 더해줍니다.
        _rigidbody.angularVelocity += torqueAxis * torqueAmount;
    }
}