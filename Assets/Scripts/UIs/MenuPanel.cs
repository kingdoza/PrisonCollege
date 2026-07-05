using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class MenuPanel : MonoBehaviour, IEscapeControllable
{
    [SerializeField] private TextMeshProUGUI _titleTmp;
    [SerializeField] private SlotEntry _passiveSlotsEntry;
    [SerializeField] private SettingPanel _settingPanel;
    [SerializeField] private SimplePanel _restartCheckPanel;
    [SerializeField] private SimplePanel _exitCheckPanel;
    [SerializeField] private ItemInfoPanel _itemInfoPanel;
    private SlotSelector _selectedSlot;
    private CanvasGroup _canvasGroup;
    private bool _isActive = false;
    private float _originTimeScale = 1;
    private bool _originCursorVisible = false;



    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        _settingPanel.gameObject.SetActive(true);
        _restartCheckPanel.gameObject.SetActive(true);
        _exitCheckPanel.gameObject.SetActive(true);
    }



    private void Start()
    {
        _settingPanel.Hide();
        _restartCheckPanel.Hide();
        _exitCheckPanel.Hide();
    }



    public void Init()
    {
        _itemInfoPanel.HidePanel();
        InventorySystem.Instance.ConstructPassiveSlots(_passiveSlotsEntry, out List<ItemSlot> _passiveSlotList);
        Hide();
        _titleTmp.text = $"{GameManager.Instance.StageTitle}\r\n";
        if (GameManager.Instance.Difficulty == DifficultyLevel.Hard)
        {
            _titleTmp.text += $"<size=70%><color=red>웨이브 {WaveSystem.Instance.CurrentWave}</color></size>";
        }
        else
        {
            _titleTmp.text += $"<size=70%>웨이브 {WaveSystem.Instance.CurrentWave}</size>";
        }

            foreach (ItemSlot slot in _passiveSlotList)
            {
                slot.GetComponent<SlotSelector>().PointerClickEvent.AddListener(SlotPointerClicked);
            }
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



    //private void Update()
    //{
    //    if (Input.GetKeyDown(KeyCode.Escape))
    //    {
    //        if (_isActive)
    //        {
    //            Hide();
    //        }
    //        else
    //        {
    //            Show();
    //        }
    //    }
    //}



    public void Show()
    {
        _isActive = true;
        _originCursorVisible = Cursor.visible;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        //Cursor.lockState = CursorLockMode.Locked;
        _originTimeScale = Time.timeScale;
        Time.timeScale = 0;
        _canvasGroup.alpha = 1;
        _canvasGroup.interactable = true;
        _canvasGroup.blocksRaycasts = true;
    }



    public void Hide()
    {
        _isActive = false;
        //Cursor.lockState = CursorLockMode.None;
        Cursor.visible = _originCursorVisible;
        Cursor.lockState = _originCursorVisible ? CursorLockMode.None : CursorLockMode.Locked;
        Time.timeScale = _originTimeScale;
        _canvasGroup.alpha = 0;
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;
    }



    public void Resume_Btn()
    {
        Hide();
        EscapeInputSystem.Instance.DisablePanel(this);
    }



    public void Restart_Btn()
    {
        EscapeInputSystem.Instance.EnablePanel(_restartCheckPanel);
    }



    public void Settings_Btn()
    {
        EscapeInputSystem.Instance.EnablePanel(_settingPanel);
    }



    public void Exit_Btn()
    {
        EscapeInputSystem.Instance.EnablePanel(_exitCheckPanel);
    }

    public void Activate()
    {
        Show();
    }

    public void Deactivate()
    {
        Hide();
    }
}
