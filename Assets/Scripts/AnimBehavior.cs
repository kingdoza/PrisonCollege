using UnityEngine;

public class AnimBehavior : StateMachineBehaviour
{
    private bool _exitActionDone = false;
    private int _myStateHash;

    [Range(0f, 1f)]
    private float _triggerThreshold = 0.25f; // 0.5면 트랜지션 딱 중간

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        _myStateHash = stateInfo.fullPathHash;
        _exitActionDone = false;

        // 진입 시점 로직
        animator.gameObject.GetComponent<PlateAttacher>()?.LiftPlate();
    }

    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // 현재 레이어가 전환 중인지 확인
        if (animator.IsInTransition(layerIndex))
        {
            // 현재 상태가 '나'에서 '다른 곳'으로 나가는 전환인지 확인
            var currentState = animator.GetCurrentAnimatorStateInfo(layerIndex);
            if (currentState.fullPathHash == _myStateHash && !_exitActionDone)
            {
                // 현재 진행 중인 '전환(Transition)' 자체의 정보를 가져옴
                var transitionInfo = animator.GetAnimatorTransitionInfo(layerIndex);

                // 전환 진행도가 설정한 임계값(중반)을 넘었을 때 실행
                if (transitionInfo.normalizedTime >= _triggerThreshold)
                {
                    _exitActionDone = true;
                    Debug.Log($"Exit Transition {_triggerThreshold * 100}% 지점: HideAll 실행");
                    animator.gameObject.GetComponent<PlateAttacher>()?.HideAll();
                }
            }
        }
    }
}