using DG.Tweening;
using UnityEngine;

public class FightFocusCamera : MonoBehaviour
{
    public Transform target;
    [SerializeField] private float rotationSpeed = 5f;
    [SerializeField] private float zoomDuration = 1.5f; // 이동에 걸리는 시간
    [SerializeField] private Ease zoomEase = Ease.OutCubic;



    private void Update()
    {
        if (target == null) return;
        Vector3 direction = target.position - transform.position;
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
    }



    public void ZoomInToTarget(float targetDistance = 2f)
    {
        if (target == null) return;

        // 1. 타겟을 향한 방향 벡터 구하기
        Vector3 directionToTarget = (target.position - transform.position).normalized;

        // 2. 목표 지점 계산 (타겟 위치에서 내 방향으로 targetDistance만큼 떨어진 곳)
        Vector3 destination = target.position - (directionToTarget * targetDistance);

        // 3. DOTween으로 부드럽게 이동
        transform.DOMove(destination, zoomDuration)
                 .SetEase(zoomEase)
                 .OnUpdate(() => {
                     // 이동하면서 계속 타겟을 바라보게 하고 싶다면 추가
                     transform.LookAt(target);
                 });
    }
}
