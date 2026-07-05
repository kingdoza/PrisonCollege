using UnityEngine;

public class CoffeeDecorator : ItemDecorator
{
    protected override bool GetItemActivation()
    {
        return AttributeSystem.Instance.IsDeskCoffee;
    }
}
