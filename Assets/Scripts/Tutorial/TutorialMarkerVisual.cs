using UnityEngine;

/// <summary>
/// 튜토리얼 월드 마커의 시각 애니메이션만 담당합니다.
/// 이동 목표 Trigger나 학생 대상 판정과 분리해 같은 Visual prefab을 재사용할 수 있습니다.
/// </summary>
public sealed class TutorialMarkerVisual : MonoBehaviour
{
    [Header("Motion Root")]
    [Tooltip("부유와 회전을 적용할 전용 Transform입니다. 비어 있으면 이 오브젝트의 Transform을 사용합니다.")]
    [SerializeField] private Transform _motionRoot;

    [Header("Floating")]
    [SerializeField] private bool _enableFloating = true;
    [Min(0f)]
    [SerializeField] private float _floatingHeight = 0.18f;
    [Min(0f)]
    [Tooltip("초당 부유 반복 횟수입니다.")]
    [SerializeField] private float _floatingCyclesPerSecond = 0.8f;
    [Tooltip("Motion Root의 로컬 좌표 기준 부유 방향입니다.")]
    [SerializeField] private Vector3 _floatingAxis = Vector3.up;

    [Header("Rotation")]
    [SerializeField] private bool _enableRotation = true;
    [Tooltip("Motion Root의 로컬 좌표 기준 회전축입니다.")]
    [SerializeField] private Vector3 _rotationAxis = Vector3.up;
    [Tooltip("초당 회전 각도입니다. 음수면 반대 방향으로 회전합니다.")]
    [SerializeField] private float _rotationDegreesPerSecond = 60f;

    private Transform _resolvedMotionRoot;
    private Vector3 _baseLocalPosition;
    private Quaternion _baseLocalRotation;
    private float _elapsed;
    private bool _hasBasePose;



    private void Awake()
    {
        ResolveMotionRoot();
    }



    private void OnEnable()
    {
        ResolveMotionRoot();
        CaptureBasePose();
        _elapsed = 0f;
        ApplyMotion();
    }



    private void Update()
    {
        if (_resolvedMotionRoot == null) return;
        _elapsed += Time.deltaTime;
        ApplyMotion();
    }



    private void OnDisable()
    {
        RestoreBasePose();
    }



    public void RestartMotion()
    {
        ResolveMotionRoot();
        if (_hasBasePose)
            RestoreBasePose();
        else
            CaptureBasePose();
        _elapsed = 0f;
        ApplyMotion();
    }



    private void ResolveMotionRoot()
    {
        _resolvedMotionRoot = _motionRoot != null ? _motionRoot : transform;
    }



    private void CaptureBasePose()
    {
        if (_resolvedMotionRoot == null) return;
        _baseLocalPosition = _resolvedMotionRoot.localPosition;
        _baseLocalRotation = _resolvedMotionRoot.localRotation;
        _hasBasePose = true;
    }



    private void ApplyMotion()
    {
        if (!_hasBasePose || _resolvedMotionRoot == null) return;

        Vector3 floatingAxis = _floatingAxis.sqrMagnitude > 0f
            ? _floatingAxis.normalized
            : Vector3.up;
        float floatingOffset = _enableFloating
            ? Mathf.Sin(_elapsed * Mathf.PI * 2f * _floatingCyclesPerSecond) * _floatingHeight
            : 0f;
        _resolvedMotionRoot.localPosition = _baseLocalPosition + floatingAxis * floatingOffset;

        Vector3 rotationAxis = _rotationAxis.sqrMagnitude > 0f
            ? _rotationAxis.normalized
            : Vector3.up;
        float rotationAngle = _enableRotation ? _elapsed * _rotationDegreesPerSecond : 0f;
        _resolvedMotionRoot.localRotation = _baseLocalRotation * Quaternion.AngleAxis(rotationAngle, rotationAxis);
    }



    private void RestoreBasePose()
    {
        if (!_hasBasePose || _resolvedMotionRoot == null) return;
        _resolvedMotionRoot.localPosition = _baseLocalPosition;
        _resolvedMotionRoot.localRotation = _baseLocalRotation;
    }
}
