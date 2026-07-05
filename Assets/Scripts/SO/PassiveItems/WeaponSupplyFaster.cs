using UnityEngine;

[CreateAssetMenu(fileName = "NewRechargeReducer", menuName = "Item/RechargeReducer")]
public class WeaponSupplyFaster : PassiveItem
{
    public float supplySpeedPercent;


    public override void Activate()
    {
        AttributeSystem.Instance.WeaponSupplySpeedMod.AddPercent(supplySpeedPercent);
    }
}
