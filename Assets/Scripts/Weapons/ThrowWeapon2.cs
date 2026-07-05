using DigitalRuby.RainMaker;
using Unity.VisualScripting;
using UnityEngine;

public class ThrowWeapon2 : RangedWeapon
{
    [SerializeField] private float _throwVelocity;
    [SerializeField] private float _flipVelocity;
    [SerializeField] private float _torqueRandomness;
    [SerializeField] private bool _enableCorrection = true;
    private ThrowAnimator _throwAnimator;

    public override string TypeName => "ÅõÃ´";



    protected override void Awake()
    {
        base.Awake();
        _throwAnimator = GetComponent<ThrowAnimator>();
    }


    protected override void Shot(Vector3 viewportPoint)
    {
        Vector3 shotDestination = GetShotDestination(viewportPoint);
        Vector3 shotDirection = (shotDestination - _spawnPoint.position).normalized;
        Quaternion projectileRot = Camera.main.transform.rotation * _spawnPoint.localRotation;
        GameObject projectileSpawned = Instantiate(_projectilePrefab, _spawnPoint.position, projectileRot);
        projectileSpawned.transform.localScale = _spawnPoint.localScale;
        Projectile projectile = projectileSpawned.GetComponent<Projectile>();
        projectile.WeaponData = DeepCopyWeaponData(_weaponData);
        projectile.Owner = _owner;
        projectile.IsStage = _controller._isStage;
        projectile.ResetForce();
        float velocityFactor = _controller._isStage ? AttributeSystem.Instance.ThrowVelocityMod.GetFinalValue(1) : 1f;
        projectile.AddVelocityForce(shotDirection, _throwVelocity * velocityFactor);
        projectile.AddTorqueForce(GetRandomTorgue(), _flipVelocity);

        //Debug.DrawRay(shotDestination, Vector3.up * 0.5f, Color.green, 1.0f);
        //Debug.DrawRay(shotDestination, Vector3.right * 0.5f, Color.green, 1.0f);
    }



    private Vector3 GetRandomTorgue()
    {
        Vector3 randomTorque = new Vector3(
                Random.Range(-_torqueRandomness, _torqueRandomness),
                Random.Range(-_torqueRandomness, _torqueRandomness),
                Random.Range(-_torqueRandomness, _torqueRandomness)
            );
        return randomTorque + Camera.main.transform.right;
    }



    private Vector3 GetShotDestination(Vector3 viewportPoint)
    {
        Ray ray = Camera.main.ViewportPointToRay(viewportPoint);

        Vector3 targetPoint;
        if (Physics.Raycast(ray, out RaycastHit hit, _maxDistance))
        {
            float throwDistance = Vector3.Distance(_spawnPoint.position, hit.point);
            //targetPoint = hit.point + -ray.direction * Mathf.InverseLerp(1f, 5f, throwDistance);
            float throwCorrection = _enableCorrection ? throwDistance * 0.15f : 0f;
            targetPoint = hit.point + -ray.direction * throwCorrection;
            //if (Vector3.Distance(_spawnPoint.position, hit.point) > 3f)
            //{
            //    targetPoint = hit.point + -ray.direction * 1f;
            //}
            //else
            //{
            //    targetPoint = hit.point;
            //}
        }
        else
        {
            // Çã°øÀ» ½úÀ» ¶§
            targetPoint = ray.GetPoint(_maxDistance);
        }
        return targetPoint;
    }



    protected override bool Acquire(int count)
    {
        if (base.Acquire(count) == false) return false;
        _throwAnimator.PlayRefillAnimation();
        return true;
    }


    protected override void CheckBullet()
    {
        base.CheckBullet();
        if (!_magazine.IsDepleted)
        {
            _throwAnimator.PlayRefillAnimation();
        }
        else
        {
            _spawnPoint.gameObject.SetActive(false);
        }
    }
}
