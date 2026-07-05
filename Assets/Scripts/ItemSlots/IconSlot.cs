using UnityEngine;
using UnityEngine.UI;

public class IconSlot : ItemSlot
{
    [SerializeField] private Image _iconImg;


    protected override void UpdateSlotUI()
    {
        if (_item != null)
        {
            _iconImg.sprite = _item.icon;
            _iconImg.enabled = true;
        }
        else
        {
            _iconImg.sprite = null;
            _iconImg.enabled = false;
        }
    }
}
