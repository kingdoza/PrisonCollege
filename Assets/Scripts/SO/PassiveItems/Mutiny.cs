using UnityEngine;

[CreateAssetMenu(fileName = "NewMutiny", menuName = "Item/Mutiny")]
public class Mutiny : PassiveItem
{
    public int moneyAmount;
    public override void Activate()
    {
        AttributeSystem.Instance.MutinyMoneyMod.AddFlat(moneyAmount);
    }
}
