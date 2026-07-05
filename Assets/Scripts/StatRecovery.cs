using UnityEngine;

public class StatRecovery : MonoBehaviour
{
    [SerializeField] private Stat _targetStat;
    [SerializeField] private float _recoveryDelay;
    [SerializeField] private float _recoverySpeed;

    private float _statDecreaseElapsed = 0;

    public bool CanRecover { get; set; } = true;
    private AttributeModifier _attributeModifier;
    private bool IsPlayerHealthRecovery => GetComponent<Professor>() != null && _targetStat is Health;



    private void Awake()
    {
        _targetStat.DecreaseEvent.AddListener(_ => OnStatDecreased());
        if (IsPlayerHealthRecovery)
        {
            _attributeModifier = AttributeSystem.Instance.HealDelaySpeedMod;
        }
    }



    private void Update()
    {
        if (!CanRecover) return;
        if (IsPlayerHealthRecovery)
        {
            _statDecreaseElapsed += Time.deltaTime * _attributeModifier.GetFinalValue();
        }
        else
        {
            _statDecreaseElapsed += Time.deltaTime;
        }
        if (_statDecreaseElapsed >= _recoveryDelay && !_targetStat.IsMax)
        {
            _targetStat.Increase(_recoverySpeed * Time.deltaTime);
        }
    }



    private void OnStatDecreased()
    {
        _statDecreaseElapsed = 0;
    }
}
