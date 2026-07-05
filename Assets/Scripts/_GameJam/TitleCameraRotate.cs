using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TitleCameraRotate : MonoBehaviour
{
    public float rotationSpeed;

    float angleY = 0f;
    float initialX;

    void Start()
    {
        // 시작 X 회전값 저장 (예: 5.307)
        initialX = transform.rotation.eulerAngles.x;
    }

    void Update()
    {
        angleY += rotationSpeed * Time.deltaTime;
        
        // X는 고정, Y만 증가시키기
        transform.rotation = Quaternion.Euler(initialX, angleY, 0f);
    }
}
