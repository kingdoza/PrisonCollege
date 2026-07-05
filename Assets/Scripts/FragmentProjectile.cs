using DG.Tweening;
using UnityEngine;

public class FragmentProjectile : Projectile
{
    [SerializeField] private GameObject _fragmentPrefab;
    [SerializeField] private WeaponData _fragmentWeaponData;
    [SerializeField] private int _fragmentCount;
    [SerializeField] private float _spreadForce = 2f; // 파편이 사방으로 퍼지는 추가 힘
    [SerializeField] private ParticleSystem _fregmentParticle;
    [SerializeField] private Stat _fragmentDistanceStat;
    [SerializeField] private SoundData _fragmentSD;
    private Vector3 _lastPosition;



    protected override void Awake()
    {
        base.Awake();
        _fragmentDistanceStat.Initialize(true);
        _fragmentDistanceStat.MaxReachEvent.AddListener(Fragment);
        _lastPosition = transform.position;
    }



    private void Update()
    {
        float distanceDelta = Vector3.Distance(_lastPosition, transform.position);
        _fragmentDistanceStat.Increase(distanceDelta);
        _lastPosition = transform.position;
    }



    private void Fragment()
    {
        // 1. 현재 발사체의 운동 상태 저장
        Vector3 currentVelocity = _rigidbody != null ? _rigidbody.linearVelocity : transform.forward * 10f;

        // 진행 방향을 기준으로 하는 회전값 계산 (원형 퍼짐의 기준축)
        Quaternion spreadRotation = Quaternion.LookRotation(currentVelocity.normalized);

        if (_fregmentParticle != null)
        {
            // 파티클을 발사체 위치에서 정면(spreadRotation)을 보게 생성
            ParticleSystem vfx = Instantiate(_fregmentParticle, transform.position, spreadRotation);
            vfx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            vfx.Play();

            // 파티클 자동 삭제 (수명 계산)
            Destroy(vfx.gameObject, vfx.main.duration);
        }

        // 2. 파편 생성 루프
        for (int i = 0; i < _fragmentCount; i++)
        {
            GameObject fragment = Instantiate(_fragmentPrefab, transform.position, transform.rotation);
            Projectile projectile = fragment.GetComponent<Projectile>();
            projectile.Owner = Owner;
            projectile.WeaponData = DeepCopyWeaponData(_fragmentWeaponData);

            Rigidbody fragRb = fragment.GetComponent<Rigidbody>();
            if (fragRb != null)
            {
                // [수정] 2D 원형 랜덤 좌표를 생성 (x, y)
                Vector2 unitCircle = Random.insideUnitCircle * _spreadForce;

                // [핵심] 생성한 원형 좌표를 진행 방향(spreadRotation)에 맞춰 3D로 변환
                // Vector3(x, y, 0)을 통해 '앞'이 아닌 '옆'으로 퍼지게 만듭니다.
                Vector3 randomSpread = spreadRotation * new Vector3(unitCircle.x, unitCircle.y, 0);

                // 원래 속도에 원형 확산 힘을 더함
                fragRb.linearVelocity = (currentVelocity + randomSpread) * 1.5f;

                // 회전력 추가
                fragRb.AddTorque(Random.insideUnitSphere * 10f, ForceMode.Impulse);
            }
        }

        // 3. 발사체 본체 삭제 및 카메라 셰이크 (선택)
        // Camera.main.transform.DOComplete();
        // Camera.main.transform.DOShakePosition(0.25f, 1.5f);
        SoundUtils.PlayScene3DSFX(_fragmentSD, transform.position);
        Destroy(gameObject);
    }


    protected WeaponData DeepCopyWeaponData(WeaponData weaponData)
    {
        DamageData newDamageData = Instantiate(weaponData.effect) as DamageData;
        WeaponData newWeaponData = Instantiate(weaponData);
        newWeaponData.effect = newDamageData;
        return newWeaponData;
    }



    //private void Fragment()
    //{
    //    // 1. 현재 발사체의 운동 상태 저장 (진행 방향 축 설정)
    //    Vector3 currentVelocity = _rigidbody != null ? _rigidbody.linearVelocity : transform.forward * 10f;

    //    // 진행 방향이 0일 경우를 대비해 예외 처리 후 회전값 계산
    //    Vector3 normalizedVelocity = currentVelocity.normalized;
    //    if (normalizedVelocity == Vector3.zero) normalizedVelocity = transform.forward;
    //    Quaternion spreadRotation = Quaternion.LookRotation(normalizedVelocity);

    //    // 2. 균등 분할을 위한 각도 계산 (360도 / 파편 개수)
    //    float angleStep = 360f / _fragmentCount;

    //    // 3. 파편 생성 루프
    //    for (int i = 0; i < _fragmentCount; i++)
    //    {
    //        // [자연스러움의 핵심] 기본 각도에 미세한 오차(예: -10 ~ 10도)를 더함
    //        // 이 수치를 키울수록 더 무질서해지고, 줄일수록 더 정교한 원형이 됩니다.
    //        float randomOffset = Random.Range(-10f, 10f);
    //        float currentAngle = (i * angleStep) + randomOffset;

    //        // 삼각함수로 원 위의 좌표(x, y) 계산
    //        float x = Mathf.Cos(currentAngle * Mathf.Deg2Rad);
    //        float y = Mathf.Sin(currentAngle * Mathf.Deg2Rad);

    //        // 진행 방향(spreadRotation)을 기준으로 한 확산 벡터
    //        // z값을 0으로 고정해야 한쪽으로 쏠리지 않고 정확히 원형으로 퍼집니다.
    //        Vector3 spreadDirection = spreadRotation * new Vector3(x, y, 0) * _spreadForce;

    //        // 4. 파편 생성 및 물리 적용
    //        GameObject fragment = Instantiate(_fragmentPrefab, transform.position, transform.rotation);
    //        Rigidbody fragRb = fragment.GetComponent<Rigidbody>();

    //        if (fragRb != null)
    //        {
    //            // 원래 날아가던 속도 + 계산된 원형 확산 힘
    //            fragRb.linearVelocity = currentVelocity + spreadDirection;

    //            // 회전력(Torque)은 무작위로 주어 자연스럽게 회전하게 함
    //            fragRb.AddTorque(Random.insideUnitSphere * 15f, ForceMode.Impulse);
    //        }
    //    }

    //    // 6. 본체 삭제
    //    Destroy(gameObject);
    //}
}
