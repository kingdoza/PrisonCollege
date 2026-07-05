using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopSlot : ItemSlot
{
    [SerializeField] private Image _IconImg;
    [SerializeField] private Image _borderImg;
    [SerializeField] private TextMeshProUGUI _nameTmp;
    [SerializeField] private TextMeshProUGUI _typeTmp;
    [SerializeField] private TextMeshProUGUI _priceTmp;
    [SerializeField] private Color _passiveColor;
    [SerializeField] private Color _weaponColor;



    protected override void UpdateSlotUI()
    {
        _IconImg.sprite = _item.icon;
        _nameTmp.text = _item.name;
        _typeTmp.text = _item.Type;
        _priceTmp.text = $"$ {_item.price}";
        if (_item is WeaponItem)
        {
            _borderImg.color = _weaponColor;
        }
        else if (_item is PassiveItem)
        {
            _borderImg.color = _passiveColor;
        }
    }
}
