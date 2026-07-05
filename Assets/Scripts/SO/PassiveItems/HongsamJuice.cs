using UnityEngine;

[CreateAssetMenu(fileName = "NewHongsamJuice", menuName = "Item/HongsamJuice")]
public class HongsamJuice : PassiveItem
{
    public float staminaCostPercent;
    public override void Activate()
    {
        AttributeSystem.Instance.StaminaCostMod.AddPercent(staminaCostPercent);
    }
}
