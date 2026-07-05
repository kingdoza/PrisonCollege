using UnityEngine;

[CreateAssetMenu(fileName = "NewWeaponItem", menuName = "Item/WeaponItem")]
public class WeaponItem : Item
{
    public int inStageIndex;
    public override string Type => "¹«±â";
}
