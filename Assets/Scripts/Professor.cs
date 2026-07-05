using UnityEngine;
using UnityEngine.Events;

public class Professor : MonoBehaviour, IAttackable
{

    public bool IsAttacking => throw new System.NotImplementedException();

    public int CurrentAttackID => throw new System.NotImplementedException();
    [SerializeField] private WeaponController _weaponController;
    [SerializeField] private bool _isSwapWheelnvert = false; // true면 방향이 반대가 됨
    [SerializeField] private float _sprintStaminaDrain = 20f;
    [SerializeField] private float _staminaRegenRate = 5f;
    [SerializeField] private float _jumpStamina = 5f;
    [SerializeField] private PlayerCamera _playerCamera;
    [SerializeField] private Transform _taskEndTransform;

    [SerializeField] private CanvasGroup _aliveCanvas;
    [SerializeField] private CanvasGroup _deadCanvas;

    private Rigidbody _rigidbody;
    private FirstPersonController _controller;
    private PlayerInteraction _playerInteraction;
    private Stamina _stamina;
    private HealthVolume _healthVolume;
    private Health _health;
    private DamageReceiver _damageReceiver;
    private Collider _collider;
    private StatRecovery _statRecovery;
    private AttributeModifier _staminaCostMod;

    public UnityEvent<string> DieEvent = new();
    public UnityEvent StaminaRunoutEvent = new();
    public float JumpStamina => _jumpStamina;

    private void Awake()
    {
        _healthVolume = GetComponent<HealthVolume>();
        _health = GetComponent<Health>();
        _health.DecreaseEvent.AddListener(_ => _healthVolume.AdjustVolume(_health.Ratio));
        _health.IncreaseEvent.AddListener(_ => _healthVolume.AdjustVolume(_health.Ratio));
        _damageReceiver = GetComponent<DamageReceiver>();
        _damageReceiver.StatDownEvent.AddListener(OnDamaged);
        _damageReceiver.DepletedEvent.AddListener(Die);
        _rigidbody = GetComponent<Rigidbody>();
        _controller = GetComponent<FirstPersonController>();
        _playerInteraction = GetComponent<PlayerInteraction>();
        _stamina = GetComponent<Stamina>();
        _stamina.Initialize();
        _collider = GetComponent<Collider>();
        _statRecovery = GetComponent<StatRecovery>();
        _staminaCostMod = AttributeSystem.Instance.StaminaCostMod;
    }


    private void Start()
    {
        _aliveCanvas.alpha = 1;
        _deadCanvas.alpha = 0;
        _health.ResetEvent.AddListener(_ => _healthVolume.AdjustVolume(_health.Ratio));
        _playerCamera.DisablePhysics();
        _weaponController.EquipWeapon(0, gameObject);
        _healthVolume.AdjustVolume(_health.Ratio);
    }



    private void Update()
    {
        // if (Input.GetMouseButtonDown(0) && CanAttack())
        // {
        //     attackAnimator.PlayMeleeSwing(Attack);
        // }
        if (Time.timeScale == 0) return;
        if (_health.IsDepleted) return;
        CheckFallDown();
        HandleSprintStamina();
        HandleWeaponAttack();
        HandleWeaponSwap();
    }



    private void CheckFallDown()
    {
        if (transform.position.y < -50)
        {
            transform.position = _taskEndTransform.position;
        }
    }



    public void Revive()
    {
        _rigidbody.isKinematic = false;
        transform.forward = _playerCamera.transform.forward;
        transform.position = _playerCamera.transform.position + Vector3.up * 1f;
        _health.Initialize();
        _stamina.Initialize();
        _aliveCanvas.alpha = 1;
        _deadCanvas.alpha = 0;
        _statRecovery.enabled = true;
        _collider.enabled = true;
        _controller.enabled = true;
        _weaponController.Show();
        _playerCamera.DisablePhysics();
        transform.localScale = Vector3.one;
    }



    private void Die(HitInfo hitInfo)
    {
        _rigidbody.isKinematic = true;
        _aliveCanvas.alpha = 0;
        _deadCanvas.alpha = 1;
        _statRecovery.enabled = false;
        _collider.enabled = false;
        _controller.enabled = false;
        _weaponController.Hide();
        _playerCamera.ApplyDeathPhysics(hitInfo);

        PostStudent attackerStudent = hitInfo.attacker.GetComponent<PostStudent>();
        //attackerStudent.UnFocusProfessorAttack();
        DieEvent?.Invoke(attackerStudent.Name);
    }



    private void OnDamaged(HitInfo hitInfo, float amount)
    {
        CameraShaker.Instance.DoDamagedShake(amount);
    }



    public void UnsetTaskPose()
    {
        transform.SetParent(null);
        if (_taskEndTransform != null)
        {
            _controller.enabled = false;
            _rigidbody.isKinematic = true;
            transform.position = _taskEndTransform.position;
            transform.rotation = _taskEndTransform.rotation;
            _rigidbody.position = _taskEndTransform.position;
            _rigidbody.rotation = _taskEndTransform.rotation;
        }
        _rigidbody.isKinematic = false;
        _weaponController.Show();
        _playerCamera.DisableTaskMode();
        _controller.SetOriginYaw();
        _controller.enabled = true;
        transform.localScale = Vector3.one;
    }


    public void DisableController()
    {
        _controller.enabled = false;
    }



    public void SetTaskPose()
    {
        _controller.StopSprinting();
        _controller.SetOriginalFOV();
        _controller.enabled = false;
        _rigidbody.isKinematic = true;
        _weaponController.Hide();
        _playerCamera.EnableTaskMode(transform.forward);
    }



    private void HandleSprintStamina()
    {
        if (_controller && _controller.IsSprinting)
        {
            _stamina.Decrease(_sprintStaminaDrain *  Time.deltaTime);
        }
        else
        {
            _stamina.Increase(_staminaRegenRate * Time.deltaTime);
        }
        //else if (_weaponController.CurrentWeapon.IsPlayingAttackAnim == false)
        //{
        //    _stamina.Increase(_staminaRegenRate * Time.deltaTime);
        //}
    }



    private void HandleWeaponAttack()
    {
        if (_weaponController.IsHiding) return;
        if (Input.GetMouseButtonDown(0))
        {
            float currentWeaponStaminaCost = _weaponController.CurrentWeapon.StaminaCost;
            if (_stamina.Current < currentWeaponStaminaCost * _staminaCostMod.GetFinalValue(1))
            {
                StaminaRunout();
                Debug.Log("스테미나가 부족합니다!");
                return;
            }
            if (_weaponController.TryAttack())
            {
                _stamina.Decrease(currentWeaponStaminaCost);
                _playerInteraction.CancelActiveInteraction();
            }
        }
    }


    public void StaminaRunout()
    {
        StaminaRunoutEvent?.Invoke();
    }



    private void HandleWeaponSwap()
    {
        if (_weaponController.IsHiding) return;
        // 숫자키 입력 예시
        for (int i = 0; i < _weaponController.WeaponCount; i++)
        {
            // KeyCode.Alpha1에 i를 더하면 Alpha2, Alpha3... 순서로 체크 가능합니다.
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                _weaponController.ChangeWeapon(i);
                break; // 해당 프레임에서 무기를 바꿨다면 루프 탈출
            }
        }
        
        // 휠 입력 예시
        float wheel = Input.GetAxis("Mouse ScrollWheel");
        if (wheel != 0)
        {
            bool isScrollDown = wheel < 0; 
            bool finalNext = isScrollDown ^ _isSwapWheelnvert;
            _weaponController.ChangeWeaponByWheel(finalNext);
        }
    }



    public void TakeDamage(float amount, Vector3 hitPoint, GameObject attacker)
    {
        throw new System.NotImplementedException();
    }

    public void Attack()
    {
        
    }
}