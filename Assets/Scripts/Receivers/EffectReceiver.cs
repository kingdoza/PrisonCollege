using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public abstract class EffectReceiver : MonoBehaviour, IEffectable
{
    //[SerializeField] private Stat _effectStat;
    public Func<bool> CanEffectChecker;
    public abstract Stat EffectedStat { get; }
    public virtual bool CanEffect => (CanEffectChecker == null || CanEffectChecker());
    public virtual bool IsInvincible { get; set; }
    public virtual Vector3 Position => transform.position;

    public UnityEvent<HitInfo, float> StatUpEvent = new UnityEvent<HitInfo, float>();
    public UnityEvent<HitInfo, float> StatDownEvent = new UnityEvent<HitInfo, float>();
    public UnityEvent<HitInfo> DepletedEvent = new UnityEvent<HitInfo>();
    public UnityEvent<HitInfo> MaxReachEvent = new UnityEvent<HitInfo>();



    public void SetStatFull()
    {
        EffectedStat.Initialize();
    }



    public void SetStatEmpty()
    {
        EffectedStat.Initialize(true);
    }



    protected virtual float DecreaseStat(HitInfo hitInfo, float amount)
    {
        float previousStat = EffectedStat.Current;
        EffectedStat.Decrease(amount);
        float statDecreased = previousStat - EffectedStat.Current;
        StatDownEvent?.Invoke(hitInfo, statDecreased);
        if (EffectedStat.IsDepleted)
        {
            DepletedEvent?.Invoke(hitInfo);
        }
        return statDecreased;
    }
    



    protected virtual float IncreaseStat(HitInfo hitInfo, float amount)
    {
        float previousStat = EffectedStat.Current;
        EffectedStat.Decrease(amount);
        float statIncreased = EffectedStat.Current - previousStat;
        StatDownEvent?.Invoke(hitInfo, statIncreased);
        if (EffectedStat.IsMax)
        {
            MaxReachEvent?.Invoke(hitInfo);
        }
        return statIncreased;
    }



    public void TakeEffect(EffectData data, HitInfo hitInfo)
    {
        if (!CanEffect || IsInvincible) return;

        if (data.effectVisualPrefab != null)
            Instantiate(data.effectVisualPrefab, hitInfo.hitPoint, hitInfo.hitRotation);

        ApplyEffect(data, hitInfo);
        //data.ApplyEffect(gameObject, causer);
    }



    protected abstract void ApplyEffect(EffectData data, HitInfo hitInfo);
}
