using System;
using UnityEngine;

public class ExitGate : MonoBehaviour
{
    [SerializeField] private Transform _barricadeParent;
    [SerializeField] private GameObject _barricadePrefab;
    [SerializeField] private GameObject _reinforcedBarricadePrefab;
    [SerializeField] private bool _isbarricadeEnabled;
    private GameObject _targetBarricadePrefab;

    protected DamageReceiver _damageReceiver;
    protected ClickAndWait _interaction;
    protected GameObject _barricadePlaced;
    protected StatRecovery _statRecovery;
    protected ExplosionShacker _explosionShacker;
    private Health _health;
    public bool IsUpgraded => AttributeSystem.Instance.IsMetalBarricade;

    public bool IsBarricadePlaced => _barricadePlaced != null;
    public virtual ExitGateType GateType => ExitGateType.None;
    public float HealthRatio => _health ? _health.Ratio : 0.0f;
    public float CurrentHealth => _health ? _health.Current : 0.0f;
    public event Action<ExitGate> BarricadePlacedEvent;
    public event Action<ExitGate> BarricadeBrokenEvent;



    protected virtual void Awake()
    {
        _health = GetComponent<Health>();
        _explosionShacker = GetComponent<ExplosionShacker>();
        _damageReceiver = GetComponent<DamageReceiver>();
        _interaction = GetComponent<ClickAndWait>();
        _statRecovery = GetComponent<StatRecovery>();

        _interaction.ProgressCompleteEvent.AddListener(PlaceBarricade);
        _damageReceiver.StatDownEvent.AddListener((_, decreasion) => OnDamaged(decreasion));
        _damageReceiver.DepletedEvent.AddListener(_ => OnHealthDepleted());
        Close();
    }



    private void Start()
    {
        EnsureTargetBarricadePrefab();
        if (StageController.Instance != null && StageController.Instance.IsTutorialRuntime)
        {
            BreakBarricade();
            return;
        }
        if (!_isbarricadeEnabled)
            BreakBarricade();
        //else
        //    PlaceBarricade();
        else
        {
            float randValue = UnityEngine.Random.value;
            if (randValue < 0.25f)
            {
                BreakBarricade();
            }
            else
            {
                PlaceBarricade();
            }
        }
    }



    private void OnDamaged(float decreasion)
    {
        if (decreasion / _health.Max > 0.99)
        {
            _explosionShacker.PlayShake();
        }
    }



    private void OnHealthDepleted()
    {
        if (_barricadePlaced == null) return;
        SoundUtils.PlayScene3DSFX(_barricadePlaced.GetComponent<Barricade>().BreakSD, transform.position, isLongDistance: true);
        BreakBarricade();
    }



    protected virtual void PlaceBarricade()
    {
        bool wasPlaced = IsBarricadePlaced;
        EnsureTargetBarricadePrefab();
        _interaction.SetInteractable(false);
        _barricadePlaced = Instantiate(_targetBarricadePrefab, _barricadeParent);
        _damageReceiver.SetStatFull();
        _statRecovery.CanRecover = true;
        if (!wasPlaced && IsBarricadePlaced)
            BarricadePlacedEvent?.Invoke(this);
    }



    protected virtual void BreakBarricade()
    {
        bool wasPlaced = IsBarricadePlaced;
        _interaction.SetInteractable(true);
        Destroy(_barricadePlaced);
        _barricadePlaced = null;
        _damageReceiver.SetStatEmpty();
        _statRecovery.CanRecover = false;
        if (wasPlaced && !IsBarricadePlaced)
            BarricadeBrokenEvent?.Invoke(this);
    }



    public void SetBarricadeStateForSetup(bool isPlaced)
    {
        SetBarricadeStateForSetup(isPlaced, isPlaced && _health != null ? _health.Max : 0f);
    }



    public void SetBarricadeStateForSetup(bool isPlaced, float health)
    {
        if (StageController.Instance == null || !StageController.Instance.IsTutorialRuntime)
        {
            Debug.LogError($"[{name}] SetBarricadeStateForSetup은 튜토리얼 runtime에서만 사용할 수 있습니다.", this);
            return;
        }

        if (isPlaced && !IsBarricadePlaced)
            PlaceBarricade();
        else if (!isPlaced && IsBarricadePlaced)
            BreakBarricade();

        if (isPlaced && _health != null)
        {
            _health.Initialize(true);
            _health.Increase(Mathf.Clamp(health, 0f, _health.Max));
        }
    }



    private void EnsureTargetBarricadePrefab()
    {
        if (_targetBarricadePrefab != null) return;
        _targetBarricadePrefab = AttributeSystem.Instance.IsMetalBarricade
            ? _reinforcedBarricadePrefab
            : _barricadePrefab;
    }

    public virtual void Open() { }

    public virtual void Close() { }
}
