using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    private TaskCameraRotator _taskCameraRotator;
    private CameraFollow _cameraFollow;
    private Rigidbody _rigidbody;
    private Collider _collider;



    private void Awake()
    {
        _taskCameraRotator = GetComponent<TaskCameraRotator>();
        _cameraFollow = GetComponent<CameraFollow>();
        _rigidbody = GetComponent<Rigidbody>();
        _collider = GetComponent<Collider>();
    }



    public void ApplyDeathPhysics(HitInfo hitInfo)
    {
        EnablePhysics();
        Vector3 pushDir = (transform.position - hitInfo.hitPoint).normalized;
        pushDir += Vector3.up * 0.4f; // 바닥에 바로 박히지 않게 살짝 위로 띄움

        _rigidbody.AddForce(pushDir * hitInfo.impulse, ForceMode.Impulse);
        _rigidbody.AddTorque(Random.insideUnitSphere * hitInfo.impulse * 0.01f, ForceMode.Impulse);
    }



    public void EnablePhysics()
    {
        _taskCameraRotator.enabled = false;
        _cameraFollow.enabled = false;
        _rigidbody.isKinematic = false;
        _collider.isTrigger = false;
        _collider.enabled = true;
    }



    public void DisablePhysics()
    {
        //_cameraFollow.currentPitch = 0;
        _taskCameraRotator.enabled = false;
        _cameraFollow.enabled = true;
        _rigidbody.isKinematic = true;
        _collider.isTrigger = true;
        _collider.enabled = false;
    }



    public void EnableTaskMode(Vector3 playerForward)
    {
        DisablePhysics();
        _cameraFollow.currentPitch = 0;
        _taskCameraRotator.Initialize(Quaternion.LookRotation(playerForward));
        _taskCameraRotator.enabled = true;
    }



    public void DisableTaskMode()
    {
        DisablePhysics();
        _cameraFollow.currentPitch = 0;
        _taskCameraRotator.enabled = false;
    }
}
