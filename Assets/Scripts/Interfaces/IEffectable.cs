using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IEffectable
{
    //void TakeEffect(EffectData data, HitInfo hitInfo);
    bool CanEffect { get; }
    public bool IsInvincible { get; set; }
    Vector3 Position { get; }
}
