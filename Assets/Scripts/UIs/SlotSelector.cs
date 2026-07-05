using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;

public class SlotSelector : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private Image _targetImage;
    [SerializeField] private Color _selectedColor;
    private Color _originColor;
    private ItemSlot _itemSlot;

    [HideInInspector] public UnityEvent<SlotSelector> PointerClickEvent = new();


    protected virtual void Awake()
    {
        _originColor = _targetImage.color;
        _itemSlot = GetComponent<ItemSlot>();
    }



    public void OnPointerClick(PointerEventData eventData)
    {
        if (_itemSlot.Item == null) return;
        PointerClickEvent?.Invoke(this);
    }



    public virtual void HighLight()
    {
        _targetImage.color = _selectedColor;
    }



    public virtual void Darken()
    {
        _targetImage.color = _originColor;
    }
}
