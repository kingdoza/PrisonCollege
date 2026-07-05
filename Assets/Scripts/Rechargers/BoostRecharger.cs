using System.Collections.Generic;
using UnityEngine;

public class BoostRecharger : Recharger
{
    protected override string GetActionName()
    {
        return "에너지드링크 보충";
    }

    protected override List<WeaponBase> GetTargetWeapons()
    {
        return _weaponCtrl.GetBoostWeapons();
    }
}
