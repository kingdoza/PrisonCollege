using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DamageReceiver : EffectReceiver
{
    protected Health _health;
    public override Stat EffectedStat => _health;
    public override bool CanEffect => base.CanEffect && _health != null && !_health.IsDepleted;

    private bool IsExitGateReceiver => GetComponent<ExitGate>() != null;
    private AttributeModifier _attributeModifier;
    public Health Health => _health;



    protected virtual void Awake()
    {
        _health = GetComponent<Health>();
        _health.Initialize();
    }



    private void Start()
    {
        if (IsExitGateReceiver)
        {
            _attributeModifier = AttributeSystem.Instance.BarricadeHitAmountMod;
        }
    }



    protected override void ApplyEffect(EffectData data, HitInfo hitInfo)
    {
        DamageData damageData = data as DamageData;
        if (!damageData) return;
        Debug.Log("ApplyEffect");
        if (IsExitGateReceiver)
        {
            DecreaseStat(hitInfo, data.value * _attributeModifier.GetFinalValue());
        }
        else
        {
            DecreaseStat(hitInfo, data.value);
        }
    }
}
