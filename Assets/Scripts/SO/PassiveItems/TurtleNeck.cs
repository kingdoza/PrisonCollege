using UnityEngine;

[CreateAssetMenu(fileName = "NewTurtleNeck", menuName = "Item/TurtleNeck")]
public class TurtleNeck : PassiveItem
{
    public float turtleNeckDistance;
    public float escapeChancePercent;
    public float studDamagePercent;


    public override void Activate()
    {
        AttributeSystem.Instance.TurtleNeckDistanceMod.AddFlat(turtleNeckDistance);
        AttributeSystem.Instance.StudEscapeChanceMod.AddPercent(escapeChancePercent);
        AttributeSystem.Instance.StudDamageMod.AddPercent(studDamagePercent);
    }
}
