using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using DOTweenSeq = DG.Tweening.Sequence;
using System; // 별명 만들기

public class AttackAnimator : MonoBehaviour
{
    [Header("Weapon Transform")]
    [SerializeField] private Transform _weaponHandle; 
    
    [Header("Settings")]
    [SerializeField] private float _attackDelay = 0.5f;
    public bool IsSwinging { private set; get; } = false;

    private Vector3 _originPos;
    private Vector3 _originRot;

    void Start()
    {
        _originPos = _weaponHandle.localPosition;
        _originRot = _weaponHandle.localEulerAngles;
    }

    void Update()
    {
        // 마우스 왼쪽 버튼(0) 클릭 감지
        // if (Input.GetMouseButtonDown(0))
        // {
        //     TrySwing();
        // }
    }

    private void TrySwing()
    {
        // 이미 공격 중이면 무시
        if (IsSwinging) return;
        
        PlayMeleeSwing(CheckHit);
    }

    public void PlayMeleeSwing(Action attackAction)
    {
        if (IsSwinging) return;
        IsSwinging = true;

        DOTweenSeq swingSeq = DOTween.Sequence();

        // 전체 시간을 1.0f로 잡고 비율로 구성한 뒤, 
        // 마지막에 전체 재생 시간을 _attackDelay로 맞춥니다.
        
        // 1. 예비 동작 (전체의 약 20%)
        swingSeq.Append(_weaponHandle.DOLocalMove(new Vector3(0.2f, 0.2f, -0.4f), 0.2f).SetEase(Ease.OutQuad));
        swingSeq.Join(_weaponHandle.DOLocalRotate(new Vector3(-20f, 60f, 0f), 0.2f).SetEase(Ease.OutQuad));

        // 2. 휘두르기 (전체의 약 15% - 매우 빠르게)
        swingSeq.Append(_weaponHandle.DOLocalMove(new Vector3(-0.5f, -0f, -0.3f), 0.15f).SetEase(Ease.InExpo));
        swingSeq.Join(_weaponHandle.DOLocalRotate(new Vector3(10f, -90f, -40f), 0.15f).SetEase(Ease.InExpo));

        // 타격 판정 (공격 시작 후 약 35% 시점)
        swingSeq.AppendCallback(() => attackAction.Invoke());

        // 3. 복귀 (전체의 약 65%)
        swingSeq.Append(_weaponHandle.DOLocalMove(_originPos, 0.65f).SetEase(Ease.OutBack));
        swingSeq.Join(_weaponHandle.DOLocalRotate(_originRot, 0.65f).SetEase(Ease.OutBack));

        // [중요] 전체 시퀀스의 길이를 _attackDelay로 강제 고정합니다.
        // 이렇게 하면 각 수치를 일일이 계산할 필요 없이 _attackDelay가 0.3초면 0.3초만에 모든 동작이 끝납니다.
        float defaultDuration = swingSeq.Duration(); // 현재 시퀀스의 기본 시간 합계 (1.0f)
        swingSeq.timeScale = defaultDuration / _attackDelay;

        swingSeq.OnComplete(() => IsSwinging = false);
    }

    private void CheckHit()
    {
        // 레이캐스트 등을 이용한 공격 판정 로직
        Debug.Log("Baseball Bat Hit!");
    }
}
