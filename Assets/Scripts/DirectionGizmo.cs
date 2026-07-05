using UnityEngine;

public class DirectionGizmo : MonoBehaviour
{
    public float arrowLength = 1.0f;
    public Color gizmoColor = Color.blue;

    private void OnDrawGizmos()
    {
        // 정면 방향으로 선 그리기
        Gizmos.color = gizmoColor;
        Vector3 direction = transform.forward * arrowLength;
        Gizmos.DrawRay(transform.position, direction);

        // 화살표 머리 (간단하게 구체로 표시)
        Gizmos.DrawSphere(transform.position + direction, 0.1f);
    }
}