using UnityEngine;

public class TurtleNeckModifier : DistanceModifier
{
    protected override AttributeModifier GetItemAttribute()
    {
        return AttributeSystem.Instance.TurtleNeckDistanceMod;
    }
}
