using UnityEngine;

[CreateAssetMenu(fileName = "NewStudFood", menuName = "Item/StudFood")]
public class StudFood : PassiveItem
{
    public float studMoveSpeedPercent;
    public float studStomachScale;
    public bool isDeskFood;




    public override void Activate()
    {
        AttributeSystem.Instance.StudMoveSpeedMod.AddPercent(studMoveSpeedPercent);
        AttributeSystem.Instance.StudStomachScaleMod.AddPercent(studStomachScale);
        AttributeSystem.Instance.IsDeskFood = isDeskFood;
    }
}
