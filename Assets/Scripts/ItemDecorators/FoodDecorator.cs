using UnityEngine;

public class FoodDecorator : ItemDecorator
{
    protected override bool GetItemActivation()
    {
        return AttributeSystem.Instance.IsDeskFood;
    }
}
