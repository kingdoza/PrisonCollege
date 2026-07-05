using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.UI.GridLayoutGroup;
using static SoundUtils;

public class AnimAttack : MonoBehaviour
{
    [SerializeField] public DamageData _damageData;
    [SerializeField] private float _hitImpulse = 100;
    [SerializeField] private float _attackRadius = 1.5f;   // 구체의 반지름
    [SerializeField] private float _attackDistance = 2.0f; // 캐릭터 정면으로 뻗어나갈 거리
    [SerializeField] private LayerMask targetLayer;
    [SerializeField] private SoundData _bodyHitSD;
    [SerializeField] private SoundData _woodHitSD;
    [SerializeField] private SoundData _metalHitSD;



    private void Start()
    {
        _damageData = Instantiate(_damageData);
        _damageData.value =  AttributeSystem.Instance.StudDamageMod.GetFinalValue(_damageData.value);
    }



    public void OnAttackHit()
    {
        // 1. 캐릭터 정면 방향으로 SphereCastAll 실행
        // 시작 지점: 현재 위치 + 약간 위(허리 높이), 방향: 정면
        Vector3 origin = transform.position + Vector3.up * 1.0f;
        Vector3 direction = transform.forward;

        _lastOrigin = origin;
        _lastDirection = direction;
        _lastRadius = _attackRadius;
        _lastDistance = _attackDistance;

        RaycastHit[] hits = Physics.SphereCastAll(origin, _attackRadius, direction, _attackDistance, targetLayer);

        // 2. 검색된 모든 대상에게 데미지 적용
        foreach (var hit in hits)
        {
            // 자기 자신 제외 (혹시 레이어 설정이 겹칠 경우)
            if (hit.collider.gameObject == gameObject) continue;

            if (hit.collider.TryGetComponent(out DamageReceiver receiver))
            {
                // 공통 정보를 담은 HitInfo 생성
                Vector3 contactPoint = hit.GetContactPoint(origin);
                Vector3 normal = hit.GetNormal(direction);
                HitInfo hitInfo = new HitInfo(contactPoint, Quaternion.LookRotation(normal), gameObject, _hitImpulse);

                // 효과 적용 (다형성 실행)
                receiver.TakeEffect(_damageData, hitInfo);

                GameObject hitObject = hit.collider.gameObject;
                if (hitObject.IsInLayerMask(Global.STUDENT_LAYER_NAME))
                {
                    PlayScene3DSFX(_bodyHitSD, hitInfo.hitPoint);
                }
                else if (hitObject.IsInLayerMask(Global.PLAYER_LAYER_NAME))
                {
                    PlayScene2DSFX(_bodyHitSD);
                }
                else if (hitObject.IsInLayerMask("Exit"))
                {
                    ExitGate exitGate = hitObject.GetComponent<ExitGate>();
                    if (exitGate && exitGate.IsUpgraded)
                    {
                        PlayScene3DSFX(_metalHitSD, hitInfo.hitPoint, isLongDistance: true);
                    }
                    else
                    {
                        PlayScene3DSFX(_woodHitSD, hitInfo.hitPoint, isLongDistance: true);
                    }
                }
            }
        }
    }


    [SerializeField] private bool _drawAttackGizmo = true;
    [SerializeField] private Color _gizmoColor = new Color(1, 0, 0, 0.5f); // 반투명 빨강
    private Vector3 _lastOrigin;
    private Vector3 _lastDirection;
    private float _lastRadius;
    private float _lastDistance;
    private bool _hasHitLastTime = false; // 히트 여부에 따라 색상을 바꾸고 싶을 때



    private void OnDrawGizmos()
    {
        if (!_drawAttackGizmo || _lastRadius <= 0) return;

        // 히트 여부에 따라 색상 변경 (맞았으면 빨강, 안 맞았으면 녹색)
        Gizmos.color = _hasHitLastTime ? _gizmoColor : Color.green;

        // 1. 시작 지점 구체
        Gizmos.DrawWireSphere(_lastOrigin, _lastRadius);

        // 2. 끝 지점 계산 및 구체
        Vector3 endPoint = _lastOrigin + _lastDirection * _lastDistance;
        Gizmos.DrawWireSphere(endPoint, _lastRadius);

        // 3. 구체 사이를 잇는 4개의 선 (원통 모양 형성)
        // 캐릭터의 우측(Right)과 위쪽(Up) 벡터를 활용해 선의 위치를 잡습니다.
        Vector3 up = Vector3.up * _lastRadius;
        Vector3 right = transform.right * _lastRadius;

        Gizmos.DrawLine(_lastOrigin + up, endPoint + up);
        Gizmos.DrawLine(_lastOrigin - up, endPoint - up);
        Gizmos.DrawLine(_lastOrigin + right, endPoint + right);
        Gizmos.DrawLine(_lastOrigin - right, endPoint - right);

        // 추가: 실제 Cast 방향 화살표
        Gizmos.DrawRay(_lastOrigin, _lastDirection * _lastDistance);
    }


    //[Header("Debug Settings")]
    //public Camera weaponCamera; // 여기에 무기 카메라를 드래그해서 넣어주세요

    //private void OnDrawGizmos()
    //{
    //    if (!_drawAttackGizmo || _lastRadius <= 0) return;

    //    // 1. 현재 그리는 카메라가 '무기 카메라'라면 절대 그리지 마라
    //    // Camera.current는 유니티가 기즈모를 그릴 때마다 해당 카메라로 바뀝니다.
    //    if (Camera.current == weaponCamera) return;

    //    // 2. 월드 좌표 강제 고정
    //    Matrix4x4 oldMatrix = Gizmos.matrix;
    //    Gizmos.matrix = Matrix4x4.identity;

    //    // 3. 드로잉 (메인 카메라와 씬 뷰에서만 실행됨)
    //    Gizmos.color = _hasHitLastTime ? _gizmoColor : Color.green;
    //    Vector3 endPoint = _lastOrigin + (_lastDirection * _lastDistance);

    //    Gizmos.DrawWireSphere(_lastOrigin, _lastRadius);
    //    Gizmos.DrawWireSphere(endPoint, _lastRadius);
    //    Gizmos.DrawLine(_lastOrigin, endPoint);

    //    Gizmos.matrix = oldMatrix;
    //}
}
