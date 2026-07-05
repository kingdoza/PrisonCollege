using UnityEngine;
using UnityEngine.EventSystems;

public class DragSlot : MonoBehaviour, IDropHandler
{
    public void OnDrop(PointerEventData eventData)
    {
        // 드래그 중인 오브젝트를 내 자식으로 편입
        if (eventData.pointerDrag != null)
        {
            // 드래그 대상의 부모를 이 슬롯으로 변경
            eventData.pointerDrag.transform.SetParent(transform);
            Debug.Log($"{gameObject.name}에 아이템이 드롭됨!");
        }
    }
}
