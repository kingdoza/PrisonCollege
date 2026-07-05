using UnityEngine;

public class Cartridge : MonoBehaviour
{
    [SerializeField] private float _ejectForce = 5f;    // 튀어 나가는 힘
    [SerializeField] private float _torqueForce = 10f;   // 회전하는 힘
    [SerializeField] private float _lifeTime = 3f;      // 사라지는 시간


    private void Awake()
    {
        
    }


    public void Eject(Vector3 direction)
    {
        Rigidbody rb = GetComponent<Rigidbody>();

        // 1. 특정 방향으로 힘 가하기 (랜덤성을 살짝 섞으면 더 자연스러움)
        Vector3 randomDir = direction + (Random.insideUnitSphere * 0.1f);
        rb.AddForce(randomDir * _ejectForce, ForceMode.Impulse);

        // 2. 무작위 회전 추가
        rb.AddTorque(Random.insideUnitSphere * _torqueForce, ForceMode.Impulse);

        // 3. 성능을 위해 일정 시간 뒤 삭제
        Destroy(gameObject, _lifeTime);
    }
}
