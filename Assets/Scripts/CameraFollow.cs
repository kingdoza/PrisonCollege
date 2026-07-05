using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform _target;
    [HideInInspector] public float currentPitch = 0f; // 컨트롤러에서 넘겨줄 상하 각도

    private void LateUpdate()
    {
        // 플레이어의 위치는 따라가되, 물리적인 떨림을 한 단계 걸러줌
        transform.localPosition = _target.position;
        transform.localRotation = Quaternion.Euler(currentPitch, _target.eulerAngles.y, 0);
    }
}
