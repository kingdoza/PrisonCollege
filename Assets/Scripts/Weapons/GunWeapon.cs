using DG.Tweening;
using UnityEngine;
using System;
using System.Collections;

public class GunWeapon : WeaponBase
{
    [SerializeField] private float _range = 100f; // 사거리
    [SerializeField] private LayerMask _targetLayer;
    [SerializeField] private LayerMask _penetrableLayer;
    [SerializeField] private int _initialBullets;
    [SerializeField] private ParticleSystem[] _muzzlaFlashParticles;
    [SerializeField] private GameObject _bulletHolePrefab;
    [SerializeField] private GameObject _trailPrefab;
    [SerializeField] private GameObject _cartridgePrefab;
    [SerializeField] private Transform _shellOutletSocket;

    public override string TypeName => "BB탄총";
    private Stat _magazine;
    public override bool CanAttack => base.CanAttack && !_magazine.IsDepleted;



    protected override void Awake()
    {
        base.Awake();
        _magazine = GetComponent<Stat>();
        _magazine.Initialize(true);
        _magazine.Increase(_initialBullets);
    }



    protected override void ExecuteAttack()
    {
        ShotBullet();
    }



    private void ShotBullet()
    {
        if (_magazine.IsDepleted) return;

        // 1. 소리는 즉시 재생 (반응성 확보)
        PlayShotSound();
        //ShootParticle();

        // 2. 나머지 로직은 딜레이 후 실행
        StartCoroutine(DelayedShotRoutine(_clipDelayLength));
    }

    private IEnumerator DelayedShotRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);

        // --- 여기서부터 딜레이 후 실행될 로직 ---

        // 파티클 재생
        foreach (var particle in _muzzlaFlashParticles)
        {
            particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particle.Play();
        }

        PlayCameraShake();

        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit[] hits = Physics.RaycastAll(ray, _range, _targetLayer | _penetrableLayer);

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (var hit in hits)
        {
            if (hit.collider.gameObject.IsInLayerMask(_targetLayer))
            {
                EffectReceiver receiver = _weaponData.effect.GetActorReceiver(hit.collider.gameObject);
                if (receiver && receiver.CanEffect)
                {
                    Vector3 contactPoint = hit.GetContactPoint(ray.origin);
                    Vector3 safeNormal = hit.GetNormal(ray.direction);

                    HitInfo hitInfo = new HitInfo(
                        contactPoint,
                        Quaternion.LookRotation(safeNormal),
                        _owner,
                        _weaponData.hitImpulse
                    );
                    receiver.TakeEffect(_weaponData.effect, hitInfo);
                }
            }

            if (hit.collider.gameObject.IsInLayerMask(_penetrableLayer))
            {
                GenerateBulletHole(hit);
                Debug.Log($"{hit.collider.name}에 탄흔을 생성했습니다.");
            }
        }

        Vector3 trailDest = ray.origin + (ray.direction * _range);
        BulletTrail trail = Instantiate(_trailPrefab, _muzzleSocket.position, Quaternion.identity).GetComponent<BulletTrail>();
        trail.Shot(trailDest);

        //Cartridge cartridge = Instantiate(_cartridgePrefab, _shellOutletSocket.position, Quaternion.identity).GetComponent<Cartridge>();
        //cartridge.Eject(_shellOutletSocket.right);

        _magazine.Decrease(1);
        StageController.Instance.GunShoot();
        InfoUpdateEvent?.Invoke(this);
    }

    private void GenerateBulletHole(RaycastHit hit)
    {
        if (_bulletHolePrefab == null) return;
        GameObject hole = Instantiate(_bulletHolePrefab, hit.point + (hit.normal * 0.01f), Quaternion.LookRotation(hit.normal));
        hole.transform.SetParent(hit.transform);
        Destroy(hole, 5f);
    }



    public bool Acquire(int count)
    {
        if (_magazine.IsMax) return false;
        _magazine.Increase(count);
        InfoUpdateEvent?.Invoke(this);
        return true;
    }



    public bool Fill()
    {
        int fillAmount = (int)(_magazine.Max - _magazine.Current);
        return Acquire(fillAmount);
    }

    [Header("Camera Shake Settings")]
    [SerializeField] private float _duration = 0.1f;  // 흔들리는 시간
    [SerializeField] private float _strength = 0.2f;  // 흔들리는 강도
    [SerializeField] private int _vibrato = 10;       // 진동 횟수
    [SerializeField] private float _randomness = 90f; // 랜덤성

    private void PlayCameraShake()
    {
        // 이전 흔들림이 끝나지 않았을 경우를 대비해 살짝 보정
        CameraShaker.Instance.DoRecoilShake(_strength);
        return;
        DOTween.Complete(Camera.main.transform);

        // Position 흔들기
        Camera.main.transform.DOShakePosition(_duration, _strength, _vibrato, _randomness)
                             .SetRelative(true); // 상대적 위치 기준

        // (선택 사항) 회전도 살짝 흔들면 타격감이 더 좋습니다.
        Camera.main.transform.DOShakeRotation(_duration, 0.5f, _vibrato);
    }

    [Header("Audio")]
    //[SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _shotSound;
    [Range(0f, 1f)][SerializeField] private float _volume = 0.7f;
    [SerializeField] private float _pitchRandomness = 0.1f; // 소리의 변주 (매번 똑같으면 귀가 피로함)
    [SerializeField] private float _clipDelayLength = 0.02f;

    private void PlayShotSound()
    {
        if (_audioSource == null || _shotSound == null) return;

        // 매번 똑같은 소리가 나지 않도록 피치를 살짝 조절 (타격감 상승)
        _audioSource.pitch = 1.0f + UnityEngine.Random.Range(-_pitchRandomness, _pitchRandomness);
        //_audioSource.time = 0.065f;
        //_audioSource.Play();

        // PlayOneShot은 소리가 재생 중이어도 중첩해서 재생합니다.
        _audioSource.PlayOneShot(_shotSound, _volume);
    }

    [Header("Steam")]
    [SerializeField] private GameObject _steamParticlePrefab; // 생성할 스팀 프리팹
    [SerializeField] private Transform _muzzleSocket;           // 생성 위치 (보통 총구)
    [SerializeField] private float _destroyTime = 10.0f;       // 파티클 자동 삭제 시간

    private void ShootParticle()
    {
        if (_steamParticlePrefab != null && _muzzleSocket != null)
        {
            // 1. 프리팹 생성 (위치와 회전값 적용)
            GameObject particle = Instantiate(_steamParticlePrefab, _muzzleSocket.position, _muzzleSocket.rotation, _muzzleSocket);

            // 2. 성능을 위해 일정 시간 후 오브젝트 파괴
            Destroy(particle, _destroyTime);
        }
        else
        {
            Debug.LogWarning("프리팹이나 생성 위치가 설정되지 않았습니다!");
        }
    }
}
