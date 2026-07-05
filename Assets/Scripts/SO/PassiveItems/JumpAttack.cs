using UnityEngine;

[CreateAssetMenu(fileName = "NewJumpAttack", menuName = "Item/JumpAttack")]
public class JumpAttack : PassiveItem
{
    public float jumpDamageScale = 2f;
    public bool isZeroJumpStamina;


    public override void Activate()
    {
        AttributeSystem.Instance.JumpDamageMod.AddPercent(jumpDamageScale);
        if (isZeroJumpStamina)
        {
            AttributeSystem.Instance.JumpStaminaMod.AddPercent(-1);
        }
    }
}
