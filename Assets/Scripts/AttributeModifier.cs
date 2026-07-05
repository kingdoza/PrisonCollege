using System.Collections.Generic;
using UnityEngine;

public class AttributeModifier
{
    private float _flat;
    private float _percent;



    public AttributeModifier()
    {
        _flat = 0;
        _percent = 1;
    }



    private void ApplyPassiveItem(PassiveItem passiveItem)
    {

    }



    public void AddFlat(float additionFlat)
    {
        _flat += additionFlat;
    }



    public void AddPercent(float additionalPercent)
    {
        _percent += additionalPercent;
    }



    public float GetFinalValue(float original = 1)
    {
        return (original + _flat) * _percent;
    }
}
