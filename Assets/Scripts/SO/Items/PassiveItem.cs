using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewPassiveItem", menuName = "Item/PassiveItem")]
public class PassiveItem : Item
{
    public override string Type => "ÆÐ½Ãºê";


    public virtual void Activate()
    {
        
    }
}



[System.Serializable]
public class AttributeData
{
    public float flat;
    public float percent;
}