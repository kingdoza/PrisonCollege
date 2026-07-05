using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SlotPackage : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _titleTmp;
    [SerializeField] private SlotEntry _shopSlotEntry;
    [SerializeField] private SlotEntry _passiveSlotEntry;
    [SerializeField] private List<ItemSlot> _weaponSlotList;
    [SerializeField] private List<ItemSlot> _equipSlotList;
    [SerializeField] private ItemInfoPanel _itemInfoPanel;
    [SerializeField] private TextMeshProUGUI _moneyTmp;
    [SerializeField] private TextMeshProUGUI _waveExplanation;
    [SerializeField] private SoundData _purchaseSD;
    private List<ItemSlot> _passiveSlotList;
    private List<SlotSelector> _slotSelectorList = new();
    private SlotSelector _selectedSlot;
    private bool _isSelectedSlotFixed;



    private void Awake()
    {
    }



    private void Start()
    {
        //foreach (SlotEntry slotEntry in _slotEntries)
        //{
        //    for (int i = 0; i < slotEntry.count; i++)
        //    {
        //        GameObject slotObject = Instantiate(slotEntry.prefab, slotEntry.parent);
        //        SlotSelector slotSelector = slotObject.GetComponent<SlotSelector>();
        //        slotSelector.PointerClickEvent.AddListener(SlotPointerClicked);
        //        _slotList.Add(slotSelector);
        //    }
        //}
        WaveSystem.Instance.NewWaveEntered();
        _itemInfoPanel.HidePanel();
        _moneyTmp.text = $"$ {InventorySystem.Instance.Money.ToString("N0")}";
        if (_shopSlotEntry.parent != null)
            InventorySystem.Instance.ConstructShopSlots(_shopSlotEntry);
        if (_passiveSlotEntry.parent != null)
            InventorySystem.Instance.ConstructPassiveSlots(_passiveSlotEntry, out _passiveSlotList);
        InventorySystem.Instance.FillWeaponSlots(_weaponSlotList);
        InventorySystem.Instance.FillEquipSlots(_equipSlotList);
        _slotSelectorList = Object.FindObjectsByType<SlotSelector>(FindObjectsSortMode.None).ToList();
        foreach (var slot in _slotSelectorList)
        {
            slot.PointerClickEvent.AddListener(SlotPointerClicked);
            DragItem dragItem = slot.GetComponentInChildren<DragItem>();
            if (dragItem)
            {
                dragItem.ItemDropEvent.AddListener(OnWeaponDroped);
            }
        }
        _titleTmp.text = $"{GameManager.Instance.StageTitle}\r\n";
        if (GameManager.Instance.Difficulty == DifficultyLevel.Hard)
        {
            _titleTmp.text += $"<size=80%><color=red>웨이브 {WaveSystem.Instance.CurrentWave}</color></size>";
        }
        else
        {
            _titleTmp.text += $"<size=80%>웨이브 {WaveSystem.Instance.CurrentWave}</size>";
        }
        _waveExplanation.text = WaveSystem.Instance.WaveInfoExplanation;
    }



    private void SlotPointerClicked(SlotSelector targetSlot)
    {
        _selectedSlot?.Darken();
        if (_selectedSlot == targetSlot)
        {
            _selectedSlot = null;
            _itemInfoPanel.HidePanel();
        }
        else
        {
            _selectedSlot = targetSlot;
            _itemInfoPanel.ShowPanel(_selectedSlot.GetComponent<ItemSlot>());
        }
        _selectedSlot?.HighLight();
    }



    private void OnWeaponDroped()
    {
        InventorySystem.Instance.UpdateEquipState(_equipSlotList.ToArray());
        ResetSelectedSlot();
    }



    private void ResetSelectedSlot()
    {
        _selectedSlot?.Darken();
        _itemInfoPanel.HidePanel();
        _selectedSlot = null;
    }



    public void Purchase()
    {
        if (_selectedSlot == null) return;
        GameObject prevSlotObject = _selectedSlot.gameObject;
        Item selectedItem = _selectedSlot.GetComponent<ItemSlot>().Item;
        if (selectedItem.price > InventorySystem.Instance.Money) return;
        SoundUtils.PlayUISFX(_purchaseSD);
        InventorySystem.Instance.Purchase(selectedItem);
        _moneyTmp.text = InventorySystem.Instance.Money.ToString("N0");
        ResetSelectedSlot();

        if (selectedItem is PassiveItem)
        {
            AddPassiveItemSlot(selectedItem);
        }
        else if (selectedItem is WeaponItem)
        {
             AddWeaponItemSlot(selectedItem);
        }
        //_selectedSlot.HighLight();
        //_itemInfoPanel.ShowPanel(_selectedSlot.GetComponent<ItemSlot>());
        Destroy(prevSlotObject);
    }



    private SlotSelector AddPassiveItemSlot(Item item)
    {
        GameObject newSlotObject = Instantiate(_passiveSlotEntry.prefab, _passiveSlotEntry.parent);
        ItemSlot itemSlot = newSlotObject.GetComponent<ItemSlot>();
        _passiveSlotList.Add(itemSlot);
        itemSlot.SetItem(item);
        SlotSelector slotSelector = newSlotObject.GetComponent<SlotSelector>();
        _slotSelectorList.Add(_selectedSlot);
        slotSelector.PointerClickEvent.AddListener(SlotPointerClicked);
        return slotSelector;
    }



    private SlotSelector AddWeaponItemSlot(Item item)
    {
        foreach (ItemSlot slot in _weaponSlotList)
        {
            if (slot.Item == null)
            {
                slot.SetItem(item);
                return slot.GetComponent<SlotSelector>();
            }
        }
        return null;
    }


    public void StartWave_Btn()
    {
        //SceneManager.LoadScene("Level2_Range");
        GameManager.Instance.StartStage();
    }



    private void Update()
    {
        if (SceneManager.GetActiveScene().name.Equals("Prepare") && Input.GetKeyDown(KeyCode.Escape))
        {
            GameManager.Instance.ShowStageSelect();
        }
    }


    public void MainScreen_Btn()
    {
        GameManager.Instance.ShowStageSelect();
    }
}


[System.Serializable]
public class SlotEntry
{
    public Transform parent;
    public GameObject prefab;
}