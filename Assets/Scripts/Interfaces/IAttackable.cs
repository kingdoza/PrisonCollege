using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IAttackable
{
    void Attack();
    bool IsAttacking { get; }
    int CurrentAttackID { get; }
}
