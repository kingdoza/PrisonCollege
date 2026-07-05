using UnityEngine;

[CreateAssetMenu(fileName = "NewRepairFaster", menuName = "Item/RepairFaster")]
public class BarricadeFaster : PassiveItem
{
    public float repairSpeedPercent;



    public override void Activate()
    {
        AttributeSystem.Instance.BarricadeInstallSpeedMod.AddPercent(repairSpeedPercent);
        AttributeSystem.Instance.HackRepairSpeedMod.AddPercent(repairSpeedPercent);
    }
}
