using UnityEngine;

public class BaldModifier : ScaleModifer
{
    public GameObject HairObject => _targetPart.gameObject;
    protected override AttributeModifier GetItemAttribute()
    {
        return AttributeSystem.Instance.StudHairScaleMod;
    }



    protected override void ModifyAppearance()
    {
        if (_attributeModifier.GetFinalValue() < 0.5f)
        {
            _targetPart.gameObject.SetActive(false);
        }
        else
        {
            _targetPart.gameObject.SetActive(true);
        }
    }
}
