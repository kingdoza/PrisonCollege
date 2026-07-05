using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DragItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private GameObject dragIcon;
    private Image sourceImage;
    public Item item; // 이 스크립트가 들고 있는 아이템 데이터 
    private ItemSlot _itemSlot;

    public UnityEvent ItemDropEvent = new();

    private void Awake()
    {
        sourceImage = GetComponent<Image>();
        _itemSlot = GetComponentInParent<ItemSlot>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (item == null) return;

        // 1. 드래그 아이콘 생성 (전과 동일)
        dragIcon = new GameObject("DragIcon");
        dragIcon.transform.SetParent(transform.root);
        dragIcon.transform.SetAsLastSibling();

        Image dragImage = dragIcon.AddComponent<Image>();
        dragImage.sprite = sourceImage.sprite;
        dragImage.rectTransform.sizeDelta = GetComponent<RectTransform>().sizeDelta;
        dragImage.color = new Color(1, 1, 1, 1f);

        CanvasGroup group = dragIcon.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false; // 드롭 감지를 위해 필수!

        sourceImage.color = new Color(1, 1, 1, 0f);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (dragIcon != null) dragIcon.transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // 1. 드래그 아이콘 즉시 파괴
        if (dragIcon != null) Destroy(dragIcon);
        sourceImage.color = Color.white;

        // 2. 드롭된 지점에 무엇이 있는지 확인
        // pointerEnter는 현재 마우스 아래에 있는 가장 위의 UI를 가리킵니다.
        GameObject target = eventData.pointerEnter;

        if (target != null)
        {
            ItemSlot targetSlot = target.GetComponentInParent<ItemSlot>();

            // 3. 대상이 IconSlot이고, 자기 자신이 아닐 때만 실행
            if (targetSlot != null && targetSlot.gameObject != transform.parent.gameObject)
            {
                // 새 슬롯에 아이템 설정
                Item targetSlotItem = targetSlot.Item;
                targetSlot.SetItem(this.item);
                if (targetSlotItem != null)
                {
                    _itemSlot.SetItem(targetSlotItem);
                }
                else
                {
                    _itemSlot.ClearItem();
                }
                ItemDropEvent?.Invoke();
            }
        }
    }
}