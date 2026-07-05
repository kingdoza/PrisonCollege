using UnityEngine;

public abstract class ItemSlot : MonoBehaviour
{
    protected Item _item;
    public Item Item => _item;
    private DragItem _dragItem;



    private void Awake()
    {
        _dragItem = GetComponentInChildren<DragItem>();
    }



    public void SetItem(Item item)
    {
        _item = item;
        if (_dragItem)
        {
            _dragItem.item = item;
        }
        UpdateSlotUI();
    }



    public void ClearItem()
    {
        _item = null;
        if (_dragItem)
        {
            _dragItem.item = null;
        }
        UpdateSlotUI();
    }



    protected abstract void UpdateSlotUI();
}
