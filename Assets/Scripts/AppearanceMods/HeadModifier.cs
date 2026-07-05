using UnityEngine;

public class HeadModifier : ScaleModifer
{
    protected override AttributeModifier GetItemAttribute()
    {
        return AttributeSystem.Instance.StudHeadScaleMod;
    }
}
