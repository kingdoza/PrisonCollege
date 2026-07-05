using UnityEngine;

public class TaskCameraRotator : MonoBehaviour
{
    [Header("Settings")]
    public float sensitivity = 2f;
    public float xLimit = 90f;
    public float yLimit = 90f;

    private float _rotationX = 0f;
    private float _rotationY = 0f;
    private Quaternion _originRotation;



    public void Initialize(Quaternion originRotation)
    {
        _originRotation = originRotation;
        _rotationX = 0f;
        _rotationY = 0f;
        transform.localRotation = _originRotation;
    }

    void Update()
    {
        return;
        //if (Time.timeScale <= 0) return;
        // 마우스 버튼을 누르고 있을 때만 각도 누적
        if (Input.GetMouseButton(1) && Time.timeScale > 0)
        {
            _rotationY += Input.GetAxis("Mouse X") * sensitivity;
            _rotationX -= Input.GetAxis("Mouse Y") * sensitivity;

            // 절대 기준(0,0)에서 ±90도 제한
            _rotationY = Mathf.Clamp(_rotationY, -yLimit, yLimit);
            _rotationX = Mathf.Clamp(_rotationX, -xLimit, xLimit);
        }

        // [수정된 핵심 로직] 
        // 비틀림(Roll)을 방지하기 위해 쿼터니언을 먼저 합친 뒤 기준점에 곱함
        // 마우스를 떼도 이 값은 업데이트되지 않으므로 위치가 유지됨
        Quaternion targetRotation = Quaternion.Euler(_rotationX, _rotationY, 0);
        transform.localRotation = _originRotation * targetRotation;
    }
}
