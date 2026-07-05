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
        _targetBarricadePrefab = AttributeSystem.Instance.IsMetalBarricade ? _reinforcedBarricadePrefab : _barricadePrefab;
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
        _interaction.SetInteractable(false);
        _barricadePlaced = Instantiate(_targetBarricadePrefab, _barricadeParent);
        _damageReceiver.SetStatFull();
        _statRecovery.CanRecover = true;
    }



    protected virtual void BreakBarricade()
    {
        _interaction.SetInteractable(true);
        Destroy(_barricadePlaced);
        _barricadePlaced = null;
        _damageReceiver.SetStatEmpty();
        _statRecovery.CanRecover = false;
    }

    public virtual void Open() { }

    public virtual void Close() { }
}
