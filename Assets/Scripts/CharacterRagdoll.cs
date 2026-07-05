using DG.Tweening;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

public class CharacterRagdoll : MonoBehaviour
{
    private class BoneTransform
    {
        public Vector3 position;
        public Quaternion rotation;
    }

    [Serializable]
    private class BoneSnapshot
    {
        public string stateName;
        public string clipName;
        public bool isFacingUp;
        [HideInInspector] public BoneTransform[] bones;
    }

    [Header("StandUp Settings")]
    [SerializeField] public bool _isAutoStandUp = true;
    [SerializeField] private float _velocityThreshold = 0.1f;
    [SerializeField] private float _standUpDelay = 2f;
    [SerializeField] private float _forcedStandUpTime = 10f;
    [SerializeField] private float _timeToLerpBones = 0.1f;

    [Header("StandUp Animation")]
    [SerializeField] private BoneSnapshot[] _boneSnapshots;

    private Animator _anim;
    private NavMeshAgent _agent;
    private Rigidbody _rootRigidbody;
    private Collider _rootCollider;
    private Transform _hipsBone;
    private Rigidbody[] _boneRigidbodies;

    private float _stopTimer = 0f;
    private float _totalTimer = 0f;
    private bool _isRagdollActive = false;
    private bool _isStandingUp = false;
    private BoneTransform[] _ragdollBones;
    private Transform[] _bones;
    private Tween _standUpTween;

    public UnityEvent StandUpStartEvent = new();
    public UnityEvent StandUpCompleteEvent = new();



    private void Awake()
    {
        _rootRigidbody = GetComponent<Rigidbody>();
        _rootCollider = GetComponent<Collider>();
        _agent = GetComponent<NavMeshAgent>();
        _anim = GetComponent<Animator>();
        _hipsBone = _anim.GetBoneTransform(HumanBodyBones.Hips);
        //_boneRigidbodies = _hipsBone.GetComponentsInChildren<Rigidbody>();
        _boneRigidbodies = _hipsBone.GetComponentsInChildren<Rigidbody>()
            .Where(rb => rb.gameObject.layer == LayerMask.NameToLayer("StudentBone"))
            .ToArray();

        _bones = new Transform[_boneRigidbodies.Length];
        _ragdollBones = new BoneTransform[_bones.Length];

        for (int i = 0; i < _boneRigidbodies.Length; i++)
        {
            _bones[i] = _boneRigidbodies[i].transform;
            _ragdollBones[i] = new BoneTransform();
        }

        foreach (var snapshot in _boneSnapshots)
        {
            snapshot.bones = PopulateAnimationStartBoneTransform(snapshot.clipName);
        }  
    }



    public void TriggerRagdoll()
    {
        _isRagdollActive = true;
        _isStandingUp = false;
        _stopTimer = 0f;
        _totalTimer = 0f;
        DOTween.Kill(this);
        SetRagdoll(true);
    }



    public void UnTriggerRagdoll()
    {
        _isRagdollActive = false;
        _stopTimer = 0f;
        _totalTimer = 0f;
        DOTween.Kill(this);
        SetRagdoll(false);
    }



    private void Update()
    {
        if (!_isRagdollActive || !_isAutoStandUp || _isStandingUp) return;

        _totalTimer += Time.deltaTime;
        if (_totalTimer >= _forcedStandUpTime)
        {
            Debug.Log("최대 시간 도달: 강제 기상");
            WakeUp();
            return;
        }

        if (_hipsBone.GetComponent<Rigidbody>().linearVelocity.sqrMagnitude < _velocityThreshold * _velocityThreshold)
        {
            _stopTimer += Time.deltaTime;
            if (_stopTimer >= _standUpDelay)
            {
                Debug.Log("속도 저하 확인: 정상 기상");
                WakeUp();
            }
        }
        else
        {
            _stopTimer = 0f; // 다시 움직이면 정지 타이머만 초기화
        }
    }



    private void WakeUp()
    {
        _isStandingUp = true;
        StandUpStartEvent?.Invoke();
        DOTween.Kill(this);

        bool isFacingUp = _hipsBone.forward.y > 0;
        BoneSnapshot targetBoneSnapshot = GetRandomStandStateName(isFacingUp);
        string targetStandUpStateName = targetBoneSnapshot.stateName;
        Vector3 targetBoneHipPos = targetBoneSnapshot.bones[0].position;

        AlignRotationToHips(isFacingUp);
        AlignPositonToHips(targetBoneHipPos);
        _ragdollBones = PopulateBoneTransform();
        BoneTransform[] targetStandUpBones = CaptureStandUpAnimPose(targetStandUpStateName);
        BlendToStandUpAnimation(targetStandUpBones, targetStandUpStateName);

        //SetRagdoll(false);
        //_anim.Rebind();
        //_anim.Update(0f);
        //_anim.Play(GetStandUpStateName(), 0, 0);
        //float animLength = _anim.GetCurrentAnimatorStateInfo(0).length;
        //DOVirtual.DelayedCall(animLength, OnStandUpComplete).SetTarget(this);
    }



    private BoneTransform[] CaptureStandUpAnimPose(string targetStandUpStateName)
    {
        _anim.Play(targetStandUpStateName, 0, 0f);
        _anim.Update(0f);
        return PopulateBoneTransform();
    }



    private BoneSnapshot GetRandomStandStateName(bool isFacingUp)
    {
        // 1. 배열 자체가 null이거나 비었는지 확인
        if (_boneSnapshots == null || _boneSnapshots.Length == 0)
        {
            Debug.LogError("데이터가 비어있습니다! 인스펙터에서 Size를 확인하세요.");
            return null;
        }

        // 2. 조건에 맞는 놈들 찾기 (s != null 체크 필수)
        var filtered = _boneSnapshots.Where(s => s != null && s.isFacingUp == isFacingUp).ToList();

        if (filtered.Count == 0) return null;

        int randomIndex = UnityEngine.Random.Range(0, filtered.Count);
        return filtered[randomIndex];
    }




    private BoneTransform[] PopulateBoneTransform()
    {
        BoneTransform[] boneTransforms = new BoneTransform[_bones.Length];
        for (int i = 0; i < boneTransforms.Length; i++)
        {
            boneTransforms[i] = new BoneTransform();
            boneTransforms[i].position = _bones[i].localPosition;
            boneTransforms[i].rotation = _bones[i].localRotation;
        }
        return boneTransforms;
    }



    private void AlignPositonToHips(Vector3 targetStandUpHipPos)
    {
        Vector3 originalHipsPos = _hipsBone.position;
        transform.position = _hipsBone.position;

        Vector3 positonOffset = targetStandUpHipPos;
        positonOffset.y = 0;
        positonOffset = transform.rotation * positonOffset;
        transform.position -= positonOffset;

        if (Physics.Raycast(_hipsBone.position + Vector3.up * 0.5f, Vector3.down, out RaycastHit hitInfo, 5f))
        {
            transform.position = new Vector3(transform.position.x, hitInfo.point.y, transform.position.z);
        }
        _hipsBone.position = originalHipsPos;
    }



    private void AlignRotationToHips(bool isFacingUp)
    {
        Vector3 originalHipsPosition = _hipsBone.position;
        Quaternion originalHipsRotation = _hipsBone.rotation;

        //Vector3 desiredForward = -_hipsBone.up;
        Vector3 desiredForward = _hipsBone.up;
        if (isFacingUp)
        {
            desiredForward *= -1;
        }
        desiredForward.y = 0;

        if (desiredForward.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.LookRotation(desiredForward);
        }

        _hipsBone.position = originalHipsPosition;
        _hipsBone.rotation = originalHipsRotation;
    }



    private void BlendToStandUpAnimation(BoneTransform[] targetStandUpBones, string targetStandUpStateName)
    {
        DOVirtual.Float(0f, 1f, _timeToLerpBones, (float value) =>
        {
            for (int i = 0; i < _bones.Length; i++)
            {
                _bones[i].localPosition = Vector3.Lerp(_ragdollBones[i].position, targetStandUpBones[i].position, value);
                _bones[i].localRotation = Quaternion.Lerp(_ragdollBones[i].rotation, targetStandUpBones[i].rotation, value);
            }
        })
        .SetEase(Ease.InQuad)
        .SetTarget(this)
        .OnComplete(() => StartStandUp(targetStandUpStateName));
    }



    private void StartStandUp(string targetStandUpStateName)
    {
        _standUpTween?.Kill();
        UnTriggerRagdoll();
        _anim.Rebind();
        _anim.Update(0f);
        _anim.Play(targetStandUpStateName, 0, 0);
        var stateInfo = _anim.GetCurrentAnimatorStateInfo(0);
        float animLength = stateInfo.length / stateInfo.speed;
        _standUpTween = DOVirtual.DelayedCall(animLength, StandUpCompleted, false).SetTarget(this);
    }



    public void StandUpCompleted()
    {
        _standUpTween?.Kill();
        _standUpTween = null;
        _isStandingUp = false;
        StandUpCompleteEvent?.Invoke();
    }



    private void SetRagdoll(bool isActive)
    {
        _anim.enabled = !isActive;
        _agent.enabled = !isActive;

        _rootCollider.enabled = !isActive;
        _rootRigidbody.useGravity = !isActive;

        foreach (Rigidbody rb in _boneRigidbodies)
        {
            rb.isKinematic = !isActive;

            if (isActive) rb.linearVelocity = Vector3.zero;

            if (isActive)
            {
                // 래그돌이 될 때
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero; // 회전 속도도 초기화해야 안 튐
                rb.interpolation = RigidbodyInterpolation.Interpolate; // 부드럽게
                rb.collisionDetectionMode = CollisionDetectionMode.Continuous; // 바닥 뚫기 방지
            }
            else
            {
                // 일어날 때 (물리 연산 최소화)
                rb.interpolation = RigidbodyInterpolation.None;
                rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
            }

            if (rb.TryGetComponent(out Collider col))
            {
                col.isTrigger = !isActive;
            }
        }
    }



    public void ApplyBoneImpact(Vector3 hitPoint, Quaternion hitRotation, float impulse)
    {
        Rigidbody closestRb = null;
        float closestDistance = float.MaxValue;

        foreach (var rb in _boneRigidbodies)
        {
            float dist = Vector3.Distance(rb.position, hitPoint);
            if (dist < closestDistance)
            {
                closestDistance = dist;
                closestRb = rb;
            }
        }

        // 2. 해당 부위에 물리 충격을 가합니다.
        if (closestRb != null)
        {
            // hitRotation의 forward 방향으로 힘을 전달
            Vector3 forceDir = hitRotation * Vector3.back;

            // AddForceAtPosition을 쓰면 피격 지점 기준으로 회전력까지 발생해서 더 사실적입니다.
            closestRb.AddForceAtPosition(forceDir * impulse, hitPoint, ForceMode.Impulse);
        }
    }



    private BoneTransform[] PopulateAnimationStartBoneTransform(string clipName)
    {
        Vector3 positionBeforeSampling = transform.position;
        Quaternion rotationBeforeSampling = transform.rotation;
        BoneTransform[] boneTransforms = null;

        foreach (AnimationClip clip in _anim.runtimeAnimatorController.animationClips)
        {
            if (clip.name == clipName)
            {
                clip.SampleAnimation(gameObject, 0);
                boneTransforms = PopulateBoneTransform();
                break;
            }
        }

        transform.position = positionBeforeSampling;
        transform.rotation = rotationBeforeSampling;
        return boneTransforms;
    }
}
