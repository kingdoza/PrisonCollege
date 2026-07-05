using UnityEngine;

[CreateAssetMenu(fileName = "NewBoostTaskMore", menuName = "Item/BoostTaskMore")]
public class BoostTaskMore : PassiveItem
{
    public float boostTaskChanceFlat;
    public bool isDeskCoffee;



    public override void Activate()
    {
        AttributeSystem.Instance.BoostTaskChanceMod.AddFlat(boostTaskChanceFlat);
        AttributeSystem.Instance.IsDeskCoffee = isDeskCoffee;
    }
}
