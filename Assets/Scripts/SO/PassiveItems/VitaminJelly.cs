using UnityEngine;

[CreateAssetMenu(fileName = "NewVitaminJelly", menuName = "Item/VitaminJelly")]
public class VitaminJelly : PassiveItem
{
    public float healDelayPercent;
    public override void Activate()
    {
        AttributeSystem.Instance.HealDelaySpeedMod.AddPercent(healDelayPercent);
    }
}
