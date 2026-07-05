using System.Collections.Generic;
using UnityEngine;

public class DamageRecharger : Recharger
{
    protected override string GetActionName()
    {
        return "무기 장탄 보충";
    }

    protected override List<WeaponBase> GetTargetWeapons()
    {
        return _weaponCtrl.GetDamageWeapons();
    }
}
