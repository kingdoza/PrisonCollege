using UnityEngine;

[CreateAssetMenu(fileName = "NewStudBaldHead", menuName = "Item/StudBaldHead")]
public class StudBaldHead : PassiveItem
{
    public bool isBald = true;
    public bool isOutline = true;




    public override void Activate()
    {
        if (isBald)
            AttributeSystem.Instance.StudHairScaleMod.AddPercent(-1);
        AttributeSystem.Instance.IsStudOutline = isOutline;
    }
}
