using UnityEngine;

[CreateAssetMenu(fileName = "NewThrowStrong", menuName = "Item/ThrowStrong")]
public class ThrowStrong : PassiveItem
{
    public float throwVelocityPercent;
    public float throwDamagePercent;


    public override void Activate()
    {
        AttributeSystem.Instance.ThrowVelocityMod.AddPercent(throwVelocityPercent);
        AttributeSystem.Instance.ThrowDamageMod.AddPercent(throwDamagePercent);
    }
}
