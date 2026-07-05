using UnityEngine;

public class StudentDetector : MonoBehaviour
{
    [SerializeField] private StudentInfo _studentInfo;
    [SerializeField] private float _detectionRange = 50f;
    [SerializeField] private LayerMask _targetLayer;
    [SerializeField] private LayerMask _blockLayer;
    [SerializeField] private float _detectSphereRadius; 

    private PostStudent _currentDetectedStudent;
    private Camera _mainCam;

    private void Awake()
    {
        // 성능을 위해 메인 카메라를 미리 찾아둡니다.
        _mainCam = Camera.main;
    }

    private void Update()
    {
        DetectStudent();
    }

    private void DetectStudent()
    {
        if (_mainCam == null) return;

        RaycastHit hit;
        LayerMask combinedLayer = _targetLayer | _blockLayer;

        // 카메라 위치에서 정면 방향으로 구를 발사
        Vector3 origin = _mainCam.transform.position;
        Vector3 direction = _mainCam.transform.forward;

        // Raycast를 SphereCast로 교체
        if (Physics.SphereCast(origin, _detectSphereRadius, direction, out hit, _detectionRange, combinedLayer))
        {
            // 1. 장애물에 가려졌는지 확인
            if (hit.collider.gameObject.IsInLayerMask(_blockLayer))
            {
                ClearDetection();
                return;
            }

            // 2. 학생인지 확인 (SphereCast는 히트된 콜라이더 정보를 정확히 반환함)
            PostStudent student = hit.collider.GetComponentInParent<PostStudent>();
            if (student != null)
            {
                if (_currentDetectedStudent != student)
                {
                    _currentDetectedStudent = student;
                    _studentInfo.Show(student);
                }
                return;
            }
        }

        // 3. 아무것도 맞지 않았거나 학생이 아닌 경우
        ClearDetection();
    }

    private void ClearDetection()
    {
        if (_currentDetectedStudent != null)
        {
            _studentInfo.Hide();
            _currentDetectedStudent = null;
        }
    }
}
