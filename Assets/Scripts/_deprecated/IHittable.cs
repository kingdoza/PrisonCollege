using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IHittable
{
    void TakeHit(EffectData data, Vector3 hitPoint, Quaternion hitRotation, GameObject attacker);
    bool CanHit { get; }
    bool IsInvincible { get; }
    Vector3 Position { get; }
}
