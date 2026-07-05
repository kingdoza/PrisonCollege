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
    private CanvasGroup _canvasGroup;



    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
    }




    public void ShowPanel(ItemSlot itemSlot)
    {
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



    public void HidePanel()
    {
        _canvasGroup.alpha = 0;
    }
}
