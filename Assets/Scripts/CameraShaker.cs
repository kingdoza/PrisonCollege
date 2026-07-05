using DG.Tweening;
using UnityEngine;

public class CameraShaker : SceneSingleton<CameraShaker>
{
    [SerializeField] private Camera _mainCamera;
    [SerializeField] private Camera _weaponCamera;
    [SerializeField] private float _damageShakePosAmount = 1;
    [SerializeField] private float _recoilShakePosAmount = 1;
    [SerializeField] private float _recoilShakeRotAmount = 1;
    [SerializeField] private float _explosionShakePosAmount = 1;
    [SerializeField] private float _explosionShakeRotAmount = 1;
    [SerializeField] private float _meleeShakePosAmount = 1;
    [SerializeField] private float _meleeShakeRotAmount = 1;
    [SerializeField] private float _weaponCameraShakeAmount = 1;



    public void DoDamagedShake(float amount)
    {
        float shakeStrength = amount * _damageShakePosAmount;
        _mainCamera.transform.parent.DOComplete();
        _weaponCamera.transform.DOComplete();
        //_mainCamera.transform.parent.DOKill(true);
        //_weaponCamera.transform.DOKill(true);
        _mainCamera.transform.parent.DOShakePosition(0.25f, shakeStrength, 25, 90).SetRelative(true).SetUpdate(UpdateType.Late);
        _weaponCamera.transform.DOShakePosition(0.25f, shakeStrength * _weaponCameraShakeAmount, 25, 90).SetRelative(true).SetUpdate(UpdateType.Late);
    }



    public void DoRecoilShake(float amount)
    {
        _mainCamera.transform.parent.DOComplete();
        _weaponCamera.transform.DOComplete();
        //_mainCamera.transform.parent.DOKill(true);
        //_weaponCamera.transform.DOKill(true);
        _mainCamera.transform.parent.DOShakePosition(0.1f, amount * _recoilShakePosAmount, 80, 90).SetRelative(true).SetRelative(true).SetUpdate(UpdateType.Late);
        _mainCamera.transform.parent.DOShakeRotation(0.1f, amount * _recoilShakeRotAmount, 80, 90).SetRelative(true).SetUpdate(UpdateType.Late);
        _weaponCamera.transform.DOShakePosition(0.1f, amount * _recoilShakePosAmount * 0.05f, 80, 90).SetRelative(true).SetRelative(true).SetUpdate(UpdateType.Late);
        _weaponCamera.transform.DOShakeRotation(0.1f, amount * _recoilShakeRotAmount * 0.05f, 80, 90).SetRelative(true).SetUpdate(UpdateType.Late);
    }


    public void DoExplosionShake(float amount)
    {
        _mainCamera.transform.parent.DOComplete();
        _weaponCamera.transform.DOComplete();
        //_mainCamera.transform.parent.DOKill(true);
        //_weaponCamera.transform.DOKill(true);
        _mainCamera.transform.parent.DOShakePosition(0.3f, amount * _explosionShakePosAmount, 50, 90).SetRelative(true).SetRelative(true).SetUpdate(UpdateType.Late);
        _mainCamera.transform.parent.DOShakeRotation(0.3f, amount * _explosionShakeRotAmount, 50, 90).SetRelative(true).SetUpdate(UpdateType.Late);
        _weaponCamera.transform.DOShakePosition(0.3f, amount * _explosionShakePosAmount * _weaponCameraShakeAmount, 40, 90).SetRelative(true).SetRelative(true).SetUpdate(UpdateType.Late);
        _weaponCamera.transform.DOShakeRotation(0.3f, amount * _explosionShakeRotAmount * _weaponCameraShakeAmount, 40, 90).SetRelative(true).SetUpdate(UpdateType.Late);
    }



    public void DoMeleeShake(float amount)
    {
        _mainCamera.transform.parent.DOComplete();
        _weaponCamera.transform.DOComplete();
        //_mainCamera.transform.parent.DOKill(true);
        //_weaponCamera.transform.DOKill(true);
        //_mainCamera.transform.DOShakePosition(0.1f, amount * _meleeShakePosAmount, 80, 90).SetRelative(true);
        _mainCamera.transform.parent.DOShakeRotation(0.15f, amount * _meleeShakeRotAmount / 4, 80, 90).SetRelative(true).SetUpdate(UpdateType.Late);
        //_weaponCamera.transform.DOShakePosition(0.1f, amount * _meleeShakePosAmount * _weaponCameraShakeAmount, 60, 90).SetRelative(true);
        _weaponCamera.transform.DOShakeRotation(0.15f, amount * _meleeShakeRotAmount * _weaponCameraShakeAmount / 4, 60, 90).SetRelative(true).SetUpdate(UpdateType.Late);
    }
}
