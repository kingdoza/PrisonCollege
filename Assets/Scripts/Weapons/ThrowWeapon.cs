using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThrowWeapon : WeaponBase
{
    [Header("--- Throw ---")]
    [SerializeField] private GameObject _throwablePrefab; // ���� ��ü ������
    [SerializeField] private Transform _throwableModel; // ���� ��ü ������
    [SerializeField] private float _throwForce = 15f;     // ������ ��
    [SerializeField] private Vector3 _throwOffset = new Vector3(0.5f, -0.2f, 1.0f);
    [SerializeField] private float _flipSpeed = 20f; // ���Ʒ� ȸ�� �ӵ� (�������� ����)
    [Range(0f, 1f)] public float _spreadAmount = 0.02f; // ź���� ����
    [Range(0f, 1f)] public float _torqueRandomness = 0.5f; // ȸ�� �ұ�Ģ��
    private ThrowAnimator _throwAnimator;
    [SerializeField] private int _initialBullets = 0;
    private Stat _magazine;
    public override string TypeName => "투척";
    public override bool CanAttack => base.CanAttack && !_magazine.IsDepleted;
    public int CurrentBulltes => (int)Mathf.Round(_magazine.Current);
    public int MaxBullets => (int)Mathf.Round(_magazine.Max);



    protected override void Awake()
    {
        base.Awake();
        _magazine = GetComponent<Stat>();
        _magazine.Initialize(true);
        _magazine.Increase(_initialBullets);
        _throwAnimator = _animator as ThrowAnimator;
        _throwableModel.gameObject.SetActive(!_magazine.IsDepleted);
    }



    //private void Update()
    //{
    //    if (Input.GetKeyDown(KeyCode.Space))
    //    {
    //        Acquire(1);
    //    }
    //}

    protected override void ExecuteAttack()
    {
        ThrowProjectile();
    }




    private void ThrowProjectile()
    {
        Camera playerCamera = Camera.main;
        if (_throwablePrefab == null || playerCamera == null) return;

        // 1. ī�޶� ���� ������
        Vector3 camPos = playerCamera.transform.position;
        Vector3 camForward = playerCamera.transform.forward;
        Vector3 camRight = playerCamera.transform.right;
        Vector3 camUp = playerCamera.transform.up;
        Quaternion camRot = playerCamera.transform.rotation;
        Quaternion rotation = Quaternion.Euler(0, -0.5f, 0);
        camForward = rotation * camForward;

        // 2. ���� ��ġ ��� (ī�޶� �߽� ������)
        Vector3 spawnPos = camPos
                           + (camRight * _throwOffset.x)
                           + (camUp * _throwOffset.y)
                           + (camForward * _throwOffset.z);

        // 3. ���� ȸ�� (�� ���� ����)
        Quaternion finalRot = camRot * _throwableModel.localRotation;

        GameObject projectileObj = Instantiate(_throwablePrefab, spawnPos, finalRot);
        projectileObj.transform.localScale = _throwableModel.localScale;
        Projectile projectile = projectileObj.GetComponent<Projectile>();
        projectile.WeaponData = _weaponData;
        projectile.Owner = _owner;

        Rigidbody rb = projectileObj.GetComponent<Rigidbody>();
        if (rb == null) rb = projectileObj.AddComponent<Rigidbody>();

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.maxAngularVelocity = 1000f;

            // 4. [�������� �߰�] �߻� ���⿡ ��¦ ���� �ֱ�
            // camForward ���⿡ ���� �̼��ϰ� ��/�Ʒ�/�� ���� ���͸� �����ϴ�.
            Vector3 randomSpread = (camUp * Random.Range(-_spreadAmount, _spreadAmount))
                                 + (camRight * Random.Range(-_spreadAmount, _spreadAmount));
            Vector3 finalThrowDir = (camForward + randomSpread).normalized;

            rb.AddForce(finalThrowDir * _throwForce, ForceMode.Impulse);

            // 5. [�������� �߰�] ȸ�� ������ �ߵ��ϰ� �����
            // ������ ������ ȸ��(camRight)�� �ణ�� �����¿� ��Ʋ�⸦ �����ϴ�.
            Vector3 randomTorque = new Vector3(
                Random.Range(-_torqueRandomness, _torqueRandomness),
                Random.Range(-_torqueRandomness, _torqueRandomness),
                Random.Range(-_torqueRandomness, _torqueRandomness)
            );

            // �⺻ ȸ����(Right)�� ���� ��Ʋ�� �߰�
            Vector3 mixedAngularVelocity = (camRight + randomTorque) * _flipSpeed;
            rb.angularVelocity = mixedAngularVelocity;
        }

        // �浹 ����
        Collider playerCol = GetComponentInParent<Collider>();
        Collider projCol = projectileObj.GetComponent<Collider>();
        if (playerCol != null && projCol != null) Physics.IgnoreCollision(playerCol, projCol);
        _magazine.Decrease(1);
        CheckAmmoModel();
        InfoUpdateEvent?.Invoke(this);
    }
    


    public bool Acquire(int count)
    {
        if (_magazine.IsMax) return false;
        int prevCount = CurrentBulltes;
        _magazine.Increase(count);
        int currentCount = CurrentBulltes;
        if (prevCount <= 0 && currentCount > 0)
        {
            _throwAnimator.PlayRefillAnimation();
        }
        InfoUpdateEvent?.Invoke(this);
        return true;
    }



    private void CheckAmmoModel()
    {
        if (!_magazine.IsDepleted)
        {
            _throwAnimator.PlayRefillAnimation();
        }
        else
        {
            _throwableModel.gameObject.SetActive(false);
        }
    }



    public bool Fill()
    {
        int fillAmount = (int)(_magazine.Max - _magazine.Current);
        return Acquire(fillAmount);
    }
}
