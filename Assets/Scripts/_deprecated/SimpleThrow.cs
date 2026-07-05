using UnityEngine;

public class SimpleThrow : MonoBehaviour
{
    [Header("설정")]
    public GameObject throwPrefab;    // 던질 물체 프리팹 (Rigidbody가 있어야 함)
    public Transform spawnPoint;      // 물체가 생성될 위치 (카메라 앞 추천)
    public float throwForce = 20f;    // 던지는 힘

    void Update()
    {
        // 마우스 왼쪽 버튼 클릭 시 즉시 투척
        if (Input.GetMouseButtonDown(0))
        {
            Throw();
        }
    }

    void Throw()
    {
        GameObject obj = Instantiate(throwPrefab, spawnPoint.position, spawnPoint.rotation);
        
        // 1. 리지드바디가 있는지 확인하고, 없으면 새로 붙임
        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = obj.AddComponent<Rigidbody>();
        }

        // 2. 물리 설정 (필요 시)
        rb.mass = 1f; // 무게 설정
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous; // 빠른 물체 통과 방지

        // 3. 발사
        rb.AddForce(spawnPoint.forward * throwForce, ForceMode.Impulse);
    }
}