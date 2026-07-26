using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemInfoPanel : MonoBehaviour
{
    [SerializeField] private Image _iconImg;
    [SerializeField] private TextMeshProUGUI _nameTmp;
    [SerializeField] private TextMeshProUGUI _typeTmp;
    [SerializeField] private TextMeshProUGUI _priceTmp;
    [SerializeField] private TextMeshProUGUI _effectTmp;
    [SerializeField] private TextMeshProUGUI _descriptionTmp;
    [SerializeField] private GameObject _purchaseBtnObject;
    [SerializeField] private TextMeshProUGUI _purchaseBtnTmp;

    [Header("Item Type Color")]
    [Tooltip("Optional. If unassigned, item type color changes are skipped.")]
    [SerializeField] private Image _itemTypeColorTarget;
    [SerializeField] private Color _weaponItemColor = new Color(1f, 0.55f, 0.1f, 1f);
    [SerializeField] private Color _passiveItemColor = new Color(0.25f, 0.85f, 0.35f, 1f);

    private CanvasGroup _canvasGroup;



    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
    }




    public void ShowPanel(ItemSlot itemSlot)
    {
        ApplyItemTypeColor(itemSlot.Item);
        _iconImg.sprite = itemSlot.Item.icon;
        _typeTmp.text = itemSlot.Item.Type;
        _nameTmp.text = itemSlot.Item.name;
        _priceTmp.text = $"$ {itemSlot.Item.price}";
        _effectTmp.text = itemSlot.Item.effect;
        _descriptionTmp.text = itemSlot.Item.description;
        _canvasGroup.alpha = 1;
        _purchaseBtnTmp.text = $"±¸¸Å <size=80%>${itemSlot.Item.price}</size>";
        _purchaseBtnObject.SetActive(itemSlot is ShopSlot);
    }



    private void ApplyItemTypeColor(Item item)
    {
        if (_itemTypeColorTarget == null || item == null)
            return;

        Color targetColor;
        if (item is WeaponItem)
            targetColor = _weaponItemColor;
        else if (item is PassiveItem)
            targetColor = _passiveItemColor;
        else
            return;

        targetColor.a = _itemTypeColorTarget.color.a;
        _itemTypeColorTarget.color = targetColor;
    }



    public void HidePanel()
    {
        _canvasGroup.alpha = 0;
    }
}
