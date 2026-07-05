using UnityEngine;

[CreateAssetMenu(fileName = "NewHackRepairFaster", menuName = "Item/HackRepairFaster")]
public class HackRepairFaster : PassiveItem
{
    public float hackDefenseFlat;



    public override void Activate()
    {
        AttributeSystem.Instance.HackBlockChanceMod.AddFlat(hackDefenseFlat);
    }
}
