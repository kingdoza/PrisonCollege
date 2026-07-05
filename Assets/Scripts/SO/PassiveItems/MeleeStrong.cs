using UnityEngine;

[CreateAssetMenu(fileName = "NewMeleeStrong", menuName = "Item/MeleeStrong")]
public class MeleeStrong : PassiveItem
{
    public float meleeSpeedPercent;
    public float meleeDamagePercent;


    public override void Activate()
    {
        AttributeSystem.Instance.MeleeAttackSpeedMod.AddPercent(meleeSpeedPercent);
        AttributeSystem.Instance.MeleeDamageMod.AddPercent(meleeDamagePercent);
    }
}
