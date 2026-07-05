using UnityEngine;

[CreateAssetMenu(fileName = "NewShoe", menuName = "Item/Shoe")]
public class Shoe : PassiveItem
{
    public float profMoveSpeedPercent;


    public override void Activate()
    {
        AttributeSystem.Instance.ProfMoveSpeedMod.AddPercent(profMoveSpeedPercent);
    }
}
