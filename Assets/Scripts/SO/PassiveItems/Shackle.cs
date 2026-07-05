using UnityEngine;

[CreateAssetMenu(fileName = "NewShackle", menuName = "Item/Shackle")]
public class Shackle : PassiveItem
{
    public float studMoveSpeedPercent;
    public float studDamagePercent;
    public bool isTiedShackle;

    public override void Activate()
    {
        AttributeSystem.Instance.StudMoveSpeedMod.AddPercent(studMoveSpeedPercent);
        AttributeSystem.Instance.StudDamageMod.AddPercent(studDamagePercent);
        AttributeSystem.Instance.IsStudShackle = isTiedShackle;
    }
}
