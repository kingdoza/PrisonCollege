using UnityEngine;

[CreateAssetMenu(fileName = "NewMetalBarri", menuName = "Item/MetalBarri")]
public class MetalBarri : PassiveItem
{
    public bool isMetalBarricade;
    public float damageRatePercent;
    public override void Activate()
    {
        AttributeSystem.Instance.IsMetalBarricade = isMetalBarricade;
        AttributeSystem.Instance.BarricadeHitAmountMod.AddPercent(damageRatePercent);
    }
}
