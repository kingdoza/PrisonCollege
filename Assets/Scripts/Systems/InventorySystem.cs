using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;

public class InventorySystem : PersistentSingleton<InventorySystem>
{
    [SerializeField] private List<Item> _totalItemList;
    [SerializeField] private List<WeaponItem> _defaultEquipedItemList;
    [SerializeField] private int _equipLimit = 4;
    [SerializeField] private int _money;
    private HashSet<Item> _nonPurchasedItemSet = new();
    private HashSet<Item> _purchasedItemSet = new();
    [SerializeField] private List<WeaponItem> _equipedItemList;


    public int Money => _money;
    public List<WeaponItem> EquipedItemList => _equipedItemList;



    protected override void Awake()
    {
        base.Awake();
        //if (_totalItemList == null) return;
        //foreach (var item in _totalItemList)
        //{
        //    _nonPurchasedItemSet.Add(item);
        //}

        //_equipedItemList = new List<WeaponItem>(new WeaponItem[_equipLimit]);
        //for (int i = 0; i < _defaultEquipedItemList.Count; i++)
        //{
        //    _purchasedItemSet.Add(_defaultEquipedItemList[i]);
        //    _equipedItemList[i] = _defaultEquipedItemList[i];
        //}
    }



    public void ResetInventory(bool resetMoney = true)
    {
        if (resetMoney)
            _money = 0;
        _nonPurchasedItemSet.Clear();
        _purchasedItemSet.Clear();
        foreach (var item in _totalItemList)
        {
            _nonPurchasedItemSet.Add(item);
        }

        _equipedItemList = new List<WeaponItem>(new WeaponItem[_equipLimit]);
        for (int i = 0; i < _defaultEquipedItemList.Count; i++)
        {
            _purchasedItemSet.Add(_defaultEquipedItemList[i]);
            _equipedItemList[i] = _defaultEquipedItemList[i];
        }
    }



    public void ActivatePassiveItems()
    {
        List<PassiveItem> passiveItemList = _purchasedItemSet.OfType<PassiveItem>().ToList();
        foreach (var item in passiveItemList)
        {
            item.Activate();
        }
    }



    public void Purchase(Item item)
    {
        _money -= item.price;
        _nonPurchasedItemSet.Remove(item);
        _purchasedItemSet.Add(item);
    }



    public void ConstructShopSlots(SlotEntry slotEntry)
    {
        foreach (var item in _nonPurchasedItemSet)
        {
            GameObject slotObject = Instantiate(slotEntry.prefab, slotEntry.parent);
            ItemSlot itemSlot = slotObject.GetComponent<ItemSlot>();
            itemSlot.SetItem(item);
        }
    }



    public void ConstructPassiveSlots(SlotEntry slotEntry, out List<ItemSlot> itemSlots)
    {
        itemSlots = new List<ItemSlot>();
        foreach (var item in _purchasedItemSet)
        {
            if (item is PassiveItem == false) continue;
            GameObject slotObject = Instantiate(slotEntry.prefab, slotEntry.parent);
            ItemSlot itemSlot = slotObject.GetComponent<ItemSlot>();
            itemSlots.Add(itemSlot);
            itemSlot.SetItem(item);
        }
    }



    private void ClearItemSlots(List<ItemSlot> itemSlots)
    {
        foreach (var slot in itemSlots)
        {
            slot.ClearItem();
        }
    }



    public void FillWeaponSlots(List<ItemSlot> itemSlots)
    {
        ClearItemSlots(itemSlots);
        List<WeaponItem> weaponItemList = _purchasedItemSet.OfType<WeaponItem>().ToList();
        weaponItemList.RemoveAll(item => _equipedItemList.Contains(item));

        for (int i = 0; i < weaponItemList.Count; ++i)
        {
            Item weaponItem = weaponItemList[i];
            itemSlots[i].SetItem(weaponItem);
        }
    }



    public void FillEquipSlots(List<ItemSlot> itemSlots)
    {
        ClearItemSlots(itemSlots);
        if (_equipedItemList == null) return;
        for (int i = 0; i < _equipedItemList.Count; ++i)
        {
            Item equipedItem = _equipedItemList[i];
            if (equipedItem == null)
            {
                itemSlots[i].ClearItem();
            }
            else
            {
                itemSlots[i].SetItem(equipedItem);
            }
        }
    }



    public void UpdateEquipState(ItemSlot[] equipSlots)
    {
        for(int i = 0; i < equipSlots.Length; ++i)
        {
            if (equipSlots[i] != null)
            {
                _equipedItemList[i] = equipSlots[i].Item as WeaponItem;
            }
            else
            {
                _equipedItemList[i] = null;
            }
        }
    }



    public void SetMoney(int money)
    {
        _money = money;
    }
}
