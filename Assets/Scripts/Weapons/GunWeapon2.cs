using System;
using UnityEngine;
using System.Linq;

public class GunWeapon2 : RangedWeapon
{
    [Header("Gun")]
    [SerializeField] private LayerMask _targetLayer;
    [SerializeField] private LayerMask _penetrableLayer;
    [SerializeField] private ParticleSystem[] _muzzlaFlashParticles;
    [SerializeField] private GameObject _bulletHolePrefab;
    [SerializeField] private GameObject _trailPrefab;
    [SerializeField] private float _recoilShakeStrength;
    [SerializeField] private SoundData _gunshotSD;
    [SerializeField] private SoundData _shotHitSD;

    public override string TypeName => "BBÅº ÃÑ";


    protected override void Shot(Vector3 viewportPoint)
    {
        PlayMuzzleParticles();
        PlayCameraShake();
        PlayShotSound();
        RaycastHit[] shotHits = GetShotHits(viewportPoint, out Ray shotRay);
        RaycastHit[] targetHits = shotHits.Where(h => h.collider.gameObject.IsInLayerMask(_targetLayer)).ToArray();
        RaycastHit[] penetrableHits = shotHits.Where(h => h.collider.gameObject.IsInLayerMask(_penetrableLayer)).ToArray();
        TryDamageTargets(targetHits, shotRay);
        MakeBulletsHoles(penetrableHits);
        StageController.Instance.GunShoot();
    }



    private void PlayCameraShake()
    {
        CameraShaker.Instance.DoRecoilShake(_recoilShakeStrength);
    }



    private void PlayShotSound()
    {
        //_audioSource.PlayOneShot(_audioSource.clip, _audioSource.volume);
        SoundUtils.PlayScene2DSFX(_gunshotSD);
    }



    private RaycastHit[] GetShotHits(Vector3 viewportPoint, out Ray ray)
    {
        ray = Camera.main.ViewportPointToRay(viewportPoint);
        RaycastHit[] hits = Physics.RaycastAll(ray, _maxDistance, _targetLayer | _penetrableLayer);
        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        return hits;
    }



    private void TryDamageTargets(RaycastHit[] _targetHits, Ray ray)
    {
        foreach (var hit in _targetHits)
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
                SoundUtils.PlayScene3DSFX(_shotHitSD, hitInfo.hitPoint);
            }
        }
    }



    private void PlayMuzzleParticles()
    {
        foreach (var particle in _muzzlaFlashParticles)
        {
            particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particle.Play();
        }
    }



    private void MakeBulletsHoles(RaycastHit[] _penetrableHits)
    {
        foreach (var hit in _penetrableHits)
        {
            if (_bulletHolePrefab == null) return;
            GameObject hole = Instantiate(_bulletHolePrefab, hit.point + (hit.normal * 0.01f), Quaternion.LookRotation(-hit.normal));
            hole.transform.SetParent(hit.transform);
            Destroy(hole, 5f);
        }
    }
}
