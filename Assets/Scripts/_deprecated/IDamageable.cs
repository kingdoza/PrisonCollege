using UnityEngine;

public interface IDamageable
{
    void TakeDamage(float amount, Vector3 hitPoint, GameObject attacker);
    bool IsDead { get; }
    bool IsInvincible { get; }
    Vector3 Position { get; }
}
