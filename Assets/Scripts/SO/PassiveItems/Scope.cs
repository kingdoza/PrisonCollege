using UnityEngine;

[CreateAssetMenu(fileName = "NewScope", menuName = "Item/Scope")]
public class Scope : PassiveItem
{
    public float shotSpreadPercent;

    public override void Activate()
    {
        AttributeSystem.Instance.ShotSpreadMod.AddPercent(shotSpreadPercent);
    }
}
