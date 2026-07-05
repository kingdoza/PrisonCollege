using UnityEngine;
using DG.Tweening;
using DOTweenSeq = DG.Tweening.Sequence;

public class WeaponAnimator : MonoBehaviour
{
    [Header("--- Idle (Breathing) ---")]
    [SerializeField] private float _idleBobAmount = 0.01f; // 아주 미세하게
    [SerializeField] private float _idleBobSpeed = 2f;    // 천천히

    [Header("--- Walk/Run Bobbing ---")]
    [SerializeField] private float _walkBobAmount = 0.05f;
    [SerializeField] private float _walkBobSpeed = 10f;

    [Header("--- Mouse Sway ---")]
    [SerializeField] private float _swayAmount = 0.02f;
    [SerializeField] private float _smoothAmount = 6f;
    [Header("--- Attack ---")]
    //[SerializeField] protected float _attackDuration = 1f;

    protected Vector3 _originPos;
    protected Vector3 _originRot;
    private Vector3 _currentBobOffset; // Bobbing 계산값 저장용
    private float _bobTimer;

    protected WeaponController _weaponController;
    private bool _isWalking;
    private bool _isSprinting;
    protected bool _isPlayAttackAnim;
    public bool IsPlayAttackAnim => _isPlayAttackAnim;

    protected virtual void Awake()
    {
        _weaponController = GetComponentInParent<WeaponController>();
        _originPos = transform.localPosition;
        _originRot = transform.localEulerAngles;
    }

    void LateUpdate()
    {
        if (_weaponController.FirstPersonController == null)
        {
            _isWalking = false;
            _isSprinting = false;
        }
        else
        {
            _isWalking = _weaponController.FirstPersonController.IsWalking;
            _isSprinting = _weaponController.FirstPersonController.IsSprinting;
        }
        if (_isPlayAttackAnim) return;
        UpdateBobbing();
        UpdateMouseSway();
    }

    private void UpdateBobbing()
    {
        // 1. 상태에 따른 속도와 폭 결정
        float targetSpeed = _idleBobSpeed;
        float targetAmount = _idleBobAmount;

        if (_isWalking)
        {
            targetSpeed = _isSprinting ? _walkBobSpeed * 2.5f : _walkBobSpeed;
            targetAmount = _isSprinting ? _walkBobAmount * 5f : _walkBobAmount;
        }

        // 2. 타이머 누적
        _bobTimer += Time.deltaTime * targetSpeed;

        // 3. 사인파를 이용한 좌표 계산 (Idle일 땐 부드럽게, Walk일 땐 8자)
        float xOffset = Mathf.Cos(_bobTimer * 0.5f) * targetAmount;
        float yOffset = Mathf.Sin(_bobTimer) * targetAmount;
        
        _currentBobOffset = new Vector3(xOffset, yOffset, 0);

        // 4. 최종 위치 적용 (Sway는 MouseSway 함수에서 보간 처리)
        // transform.localPosition은 MouseSway에서 부드럽게 밀어주고 있으므로 
        // 여기서는 타겟 베이스 위치에 Bobbing만 더해줍니다.
    }

    private void UpdateMouseSway()
    {
        float mouseX = 0;
        float mouseY = 0;
        if (_weaponController.FirstPersonController != null)
        {
            mouseX = -Input.GetAxis("Mouse X") * _swayAmount;
            mouseY = -Input.GetAxis("Mouse Y") * _swayAmount;

        }

        Vector3 targetPos = _originPos + _currentBobOffset + new Vector3(mouseX, mouseY, 0);
        
        // 모든 움직임을 합쳐서 최종적으로 한 번만 Lerp (끊김 방지의 핵심)
        transform.localPosition = Vector3.Lerp(transform.localPosition, targetPos, Time.deltaTime * _smoothAmount);
    }

    // Sprint 포즈 전환 시에는 DOTween을 사용하되, _originPos 자체를 옮겨주는 게 안전합니다.
    public void SetSprintPose(bool sprint)
    {
        // 기존 트윈 삭제 후 새로운 목표 지점으로 이동
        DOTween.Kill("SprintPose");
        
        Vector3 targetPos = sprint ? new Vector3(0.3f, -0.4f, 0.2f) : Vector3.zero;
        Vector3 targetRot = sprint ? new Vector3(10, -30, 0) : Vector3.zero;

        // 실제 _originPos를 옮기지 말고, 추가적인 Offset 변수를 하나 더 쓰는 게 좋지만
        // 간단하게 구현하기 위해 Rotate만 트윈을 주고 Position은 계산식에 맡깁니다.
        transform.DOLocalRotate(targetRot, 0.4f).SetEase(Ease.OutQuad).SetId("SprintPose");
    }



    // 무기 꺼내기 애니메이션
    public void Draw(float duration)
    {
        transform.DOKill(true);
        gameObject.SetActive(true);
        // 아래에서 위로 올라오는 연출
        transform.localPosition = _originPos + new Vector3(0, -0.5f, 0);
        transform.DOLocalMove(_originPos, duration).SetEase(Ease.OutBack);
    }

    // 무기 넣기 애니메이션
    public void Holster(float duration, System.Action onComplete)
    {
        transform.DOKill(true);
        transform.DOLocalMove(_originPos + new Vector3(0, -0.5f, 0), duration)
            .SetEase(Ease.InQuad)
            .OnComplete(() => 
            {
                gameObject.SetActive(false);
                onComplete?.Invoke();
            }
        );
    }


    public void StartAttack(System.Action attackExecution, float attackDuration)
    {
        if (_isPlayAttackAnim) return; // 중복 실행 방지
        _isPlayAttackAnim = true;

        attackDuration /= AttributeSystem.Instance.MeleeAttackSpeedMod.GetFinalValue(1);
        DOTweenSeq attackAnimSeq = DOTween.Sequence();
        AddAttackFrames(attackAnimSeq, attackExecution, attackDuration);
        float defaultDuration = attackAnimSeq.Duration(); // 현재 시퀀스의 기본 시간 합계 (1.0f)
        attackAnimSeq.timeScale = defaultDuration / attackDuration;
        attackAnimSeq.OnComplete(() => 
        {
            _isPlayAttackAnim = false;
        });
    }


    protected virtual void AddAttackFrames(DOTweenSeq attackAnimSeq, System.Action attackExecution, float attackDuration) 
    {
        attackAnimSeq.Append(transform.DOLocalMoveZ(_originPos.z + 0.1f, 0.1f));
        attackAnimSeq.Append(transform.DOLocalMoveZ(_originPos.z, 0.1f));
    }
}