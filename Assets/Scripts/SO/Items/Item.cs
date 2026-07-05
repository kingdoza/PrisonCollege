using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "Item/Item")]
public class Item : ScriptableObject
{
    public int id;
    public new string name;
    public Sprite icon;
    public int price;
    [TextArea] public string effect;
    [TextArea] public string description;
    public ItemRarity rarity;
    public virtual string Type => "-";
}



public enum ItemRarity
{
    Normal,
    Rare,
    Epic,
    Legendary,
}