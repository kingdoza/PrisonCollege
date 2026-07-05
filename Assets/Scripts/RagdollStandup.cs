using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

public class RagdollStandup : MonoBehaviour
{
    private class BoneTransform
    {
        public Vector3 position;
        public Quaternion rotation;
    }

    [Header("Settings")]
    [SerializeField] private float _timeToWakeup = 3f; // ��� ����? (Blend �ð����� ��ü�ȵ�)
    [SerializeField] private string _faceupStandupStateName = "StandUp";
    [SerializeField] private string _facedownStandupStateName = "StandUp";
    [SerializeField] private string _faceupStandupClipName = "StandUpClip";
    [SerializeField] private string _facedownStandupClipName = "StandUpClip";
    [SerializeField] private float _timeToResetBones = 0.5f;

    [Header("Components")]
    private Transform _hipsBone;
    private Animator _anim;
    private NavMeshAgent _agent;
    private Rigidbody[] _boneRigidBodies;
    private Rigidbody _rootRigidbody;
    private Collider _rootCollider;

    private BoneTransform[] _faceupStandupBones;
    private BoneTransform[] _facedownStandupBones;
    private BoneTransform[] _ragdollBones;
    private Transform[] _bones;
    private bool _isFacingUp = false;

    public UnityEvent StandUpCompleteEvent = new();

    private void Awake()
    {
        _anim = GetComponent<Animator>();
        _agent = GetComponent<NavMeshAgent>();
        _rootRigidbody = GetComponent<Rigidbody>();
        _rootCollider = GetComponent<Collider>();

        _hipsBone = _anim.GetBoneTransform(HumanBodyBones.Hips);
        _boneRigidBodies = _hipsBone.GetComponentsInChildren<Rigidbody>();

        // ���� �迭 �ʱ�ȭ
        _bones = new Transform[_boneRigidBodies.Length];
        _faceupStandupBones = new BoneTransform[_bones.Length];
        _facedownStandupBones = new BoneTransform[_bones.Length];
        _ragdollBones = new BoneTransform[_bones.Length];

        for (int i = 0; i < _boneRigidBodies.Length; i++)
        {
            _bones[i] = _boneRigidBodies[i].transform;
            _faceupStandupBones[i] = new BoneTransform();
            _facedownStandupBones[i] = new BoneTransform();
            _ragdollBones[i] = new BoneTransform();
        }

        // Awake���� �̸� �Ͼ�� �ִϸ��̼��� 'ù ������' ��� �����صӴϴ�.
        PopulateAnimationStartBoneTransform(_faceupStandupClipName, _faceupStandupBones);
        PopulateAnimationStartBoneTransform(_facedownStandupClipName, _facedownStandupBones);
    }


    public void WakeUp()
    {
        DOTween.Kill(this);
        _isFacingUp = _hipsBone.forward.y > 0;
        AlignRotationToHips();
        AlignPositonToHips();
        PopulateBoneTransform(_ragdollBones);
        CaptureStandUpPose();
        BlendToAnimation(_timeToResetBones);

        //SetRagdoll(false);
        //_anim.Rebind();
        //_anim.Update(0f);
        //_anim.Play(GetStandUpStateName(), 0, 0);
        //float animLength = _anim.GetCurrentAnimatorStateInfo(0).length;
        //DOVirtual.DelayedCall(animLength, OnStandUpComplete).SetTarget(this);
    }

    // �ǽð� ���� ĸó�� ���� �Լ�
    private void CaptureStandUpPose()
    {
        _anim.Play(GetStandUpStateName(), 0, 0f);
        _anim.Update(0f);
        PopulateBoneTransform(GetStandUpBoneTransforms());
    }



    public void BlendToAnimation(float duration)
    {
        BoneTransform[] standUpBones = GetStandUpBoneTransforms();

        DOVirtual.Float(0f, 1f, duration, (float value) =>
        {
            for (int i = 0; i < _bones.Length; i++)
            {
                _bones[i].localPosition = Vector3.Lerp(_ragdollBones[i].position, standUpBones[i].position, value);
                _bones[i].localRotation = Quaternion.Lerp(_ragdollBones[i].rotation, standUpBones[i].rotation, value);
            }
        })
        .SetEase(Ease.InQuad)
        .SetTarget(this) // ��ũ��Ʈ�� �ı��Ǹ� Ʈ���� ����
        .OnComplete(() =>
        {
            // 6. ������ ������ ��μ� �ִϸ����͸� �Ѱ� �ִϸ��̼� ���
            //_anim.enabled = true;
            SetRagdoll(false);
            _anim.Rebind();
            _anim.Update(0f); // �ʱ�ȭ
            _anim.Play(GetStandUpStateName(), 0, 0);
            //_anim.CrossFadeInFixedTime(_standupStateName, 0.15f, 0, 0f);

            // 7. �ִϸ��̼� ���̸�ŭ ��� �� �Ϸ� �̺�Ʈ ����
            float animLength = _anim.GetCurrentAnimatorStateInfo(0).length;
            DOVirtual.DelayedCall(animLength, OnStandUpComplete, false).SetTarget(this);
        });
    }

    private void OnStandUpComplete()
    {
        Debug.Log("DoTween: ĳ���Ͱ� ������ �Ͼ���ϴ�.");

        // �̵� �����ϵ��� NavMeshAgent Ȱ��ȭ
        if (_agent != null)
        {
            // 1. 에이전트를 켜기 전, 현재 캐릭터 위치로 에이전트 데이터를 강제 이동(Warp)
            _agent.Warp(transform.position);

            // 2. 그 다음 에이전트 활성화
            _agent.enabled = true;
        }

        StandUpCompleteEvent?.Invoke();
    }

    public void SetRagdoll(bool isActive)
    {
        _anim.enabled = !isActive;
        if (_agent != null) _agent.enabled = !isActive;

        _rootCollider.enabled = !isActive;
        _rootRigidbody.useGravity = !isActive;

        foreach (Rigidbody rb in _boneRigidBodies)
        {
            rb.isKinematic = !isActive;

            if (isActive) rb.linearVelocity = Vector3.zero;

            if (rb.TryGetComponent(out Collider col))
            {
                col.isTrigger = !isActive;
            }
        }
    }

    private void AlignPositonToHips()
    {
        Vector3 originalHipsPos = _hipsBone.position;

        transform.position = _hipsBone.position;

        Vector3 positonOffset = GetStandUpBoneTransforms()[0].position;
        positonOffset.y = 0;
        positonOffset = transform.rotation * positonOffset;
        transform.position -= positonOffset;

        if (Physics.Raycast(_hipsBone.position + Vector3.up * 0.5f, Vector3.down, out RaycastHit hitInfo, 5f))
        {
            transform.position = new Vector3(transform.position.x, hitInfo.point.y, transform.position.z);
        }
        _hipsBone.position = originalHipsPos;
    }

    private void AlignRotationToHips()
    {
        Vector3 originalHipsPosition = _hipsBone.position;
        Quaternion originalHipsRotation = _hipsBone.rotation;

        //Vector3 desiredForward = -_hipsBone.up;
        Vector3 desiredForward = _hipsBone.up;
        if (_isFacingUp)
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


    private string GetStandUpStateName()
    {
        return _isFacingUp ? _faceupStandupStateName : _facedownStandupStateName;
    }



    private BoneTransform[] GetStandUpBoneTransforms()
    {
        return _isFacingUp ? _faceupStandupBones : _facedownStandupBones;
    }


    private void PopulateBoneTransform(BoneTransform[] boneTransforms)
    {
        for (int i = 0; i < _bones.Length; i++)
        {
            boneTransforms[i].position = _bones[i].localPosition;
            boneTransforms[i].rotation = _bones[i].localRotation;
        }
    }

    private void PopulateAnimationStartBoneTransform(string clipName, BoneTransform[] boneTransforms)
    {
        Vector3 positionBeforeSampling = transform.position;
        Quaternion rotationBeforeSampling = transform.rotation;

        foreach (AnimationClip clip in _anim.runtimeAnimatorController.animationClips)
        {
            if (clip.name == clipName)
            {
                clip.SampleAnimation(gameObject, 0);
                PopulateBoneTransform(boneTransforms);
                break;
            }
        }

        transform.position = positionBeforeSampling;
        transform.rotation = rotationBeforeSampling;
    }
}