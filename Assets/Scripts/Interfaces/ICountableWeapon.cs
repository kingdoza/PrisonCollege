using UnityEngine;

public interface ICountableWeapon
{
    int Amount { get; }
    void Acquire(int count);
}
