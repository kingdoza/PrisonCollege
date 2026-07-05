using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Stat : MonoBehaviour
{
    [SerializeField] protected float _maxStat = 100f;
    [SerializeField] protected float _currentStat;

    public float Current => _currentStat;
    public float Max => _maxStat;
    public float Ratio => _currentStat / _maxStat;
    public bool IsDepleted => _currentStat <= 0;
    public bool IsMax => _currentStat >= _maxStat;

    [HideInInspector] public UnityEvent<float> IncreaseEvent = new UnityEvent<float>();
    [HideInInspector] public UnityEvent<float> DecreaseEvent = new UnityEvent<float>();
    [HideInInspector] public UnityEvent DepletedEvent = new UnityEvent();
    [HideInInspector] public UnityEvent MaxReachEvent = new UnityEvent();
    [HideInInspector] public UnityEvent<float> ResetEvent = new UnityEvent<float>();

    //protected virtual void Awake() => Initialize();

    private bool IsPlayerStamina => GetComponent<Professor>() != null && this is Stamina;
    private AttributeModifier _attributeModifier;



    public virtual void Initialize(bool issetToZero = false)
    {
        _currentStat = issetToZero ? 0 : _maxStat;
        ResetEvent?.Invoke(_currentStat);
        if (IsPlayerStamina)
        {
            _attributeModifier = AttributeSystem.Instance.StaminaCostMod;
        }
    }



    public void Decrease(float amount)
    {
        if (IsDepleted) return;
        if (IsPlayerStamina)
        {
            amount *= _attributeModifier.GetFinalValue();
        }
        _currentStat = Mathf.Max(0, _currentStat - amount);
        DecreaseEvent?.Invoke(amount);
        if (IsDepleted)
        {
            Depleted();
        }
    }

    public void Increase(float amount)
    {
        if (IsMax) return;
        _currentStat = Mathf.Min(_maxStat, _currentStat + amount);
        IncreaseEvent?.Invoke(amount);
        if (IsMax)
        {
            MaxReached();
        }
    }

    private void Depleted()
    {
        DepletedEvent?.Invoke();
    }

    private void MaxReached()
    {
        MaxReachEvent?.Invoke();
    }
}
