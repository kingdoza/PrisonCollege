using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class StageSlot : MonoBehaviour, IPointerClickHandler
{
    [Header("Base")]
    //[SerializeField] private int _stageNumber = 0;
    [SerializeField] private Image _mainImg;
    [SerializeField] private Image _borderImg;
    [SerializeField] private TextMeshProUGUI _titleTmp;
    [Header("Focus Settings")]
    [SerializeField] private CanvasGroup _focusGroup;
    [SerializeField] private Color _borderFocusColor;
    [Header("Lock Settings")]
    [SerializeField] private CanvasGroup _lockGroup;
    [SerializeField] private Color _borderLockColor;
    [Header("ClearUI Settings")]
    [SerializeField] private CanvasGroup _clearGroup;
    [SerializeField] private TextMeshProUGUI _clearTmp;
    [SerializeField] private Image _clearIconImg;
    [SerializeField] private Color _hardClearColor;
    [Header("Dev Only")]
    //[SerializeField] private bool _isLocked = false;
    //[SerializeField] private DifficultyLevel _clearState = DifficultyLevel.None;
    private Color _originBorderColor;
    private StageInfo _stageInfo;
    private Hover _hover;

    //public DifficultyLevel ClearState { set { _clearState = (DifficultyLevel)Mathf.Max((int)_clearState, (int)value); } }
    [HideInInspector] public UnityEvent<StageSlot> MouseClickEvent = new();




    private void Awake()
    {
        _hover = GetComponent<Hover>();
        _originBorderColor = _borderImg.color;
        _focusGroup.gameObject.SetActive(true);
        _lockGroup.gameObject.SetActive(true);
        _clearGroup.gameObject.SetActive(true);
        //Unfocus();
    }



    //private void Start()
    //{
    //    SetLockUI();
    //    SetClearUI();
    //}



    public void Init(StageInfo stageInfo)
    {
        _stageInfo = stageInfo;
        _titleTmp.text = $"{stageInfo.number}. {stageInfo.name}";
        _mainImg.sprite = stageInfo.sprite;
        SetLockUI();
        SetClearUI();
        Unfocus();
    }


    public void OnPointerClick(PointerEventData eventData)
    {
        if (_stageInfo.isLocked) return;
        MouseClickEvent?.Invoke(this);
    }



    public void Focus()
    {
        _focusGroup.alpha = 1;
        _focusGroup.interactable = true;
        _focusGroup.blocksRaycasts = true;
        _borderImg.color = _borderFocusColor;
    }



    public void Unfocus()
    {
        _focusGroup.alpha = 0;
        _focusGroup.interactable = false;
        _focusGroup.blocksRaycasts = false;
        _borderImg.color = _originBorderColor;
    }



    private void SetLockUI()
    {
        _hover.enabled = !_stageInfo.isLocked;
        if (_stageInfo.isLocked)
        {
            _lockGroup.alpha = 1;
            _lockGroup.interactable = true;
            _lockGroup.blocksRaycasts = true;
            _borderImg.color = _borderLockColor;
        }
        else
        {
            _lockGroup.alpha = 0;
            _lockGroup.interactable = false;
            _lockGroup.blocksRaycasts = false;
            _borderImg.color = _originBorderColor;
        }
    }


    private void SetClearUI()
    {
        switch (_stageInfo.maxClearDifficulty)
        {
            case DifficultyLevel.None:
                _clearGroup.alpha = 0;
                _clearTmp.text = "";
                _clearTmp.color = Color.clear;
                _clearIconImg.color = Color.clear;
                break;
            case DifficultyLevel.Normal:
                _clearGroup.alpha = 1;
                _clearTmp.text = "일반";
                _clearTmp.color = Color.white;
                _clearIconImg.color = Color.white;
                break;
            case DifficultyLevel.Hard:
                _clearGroup.alpha = 1;
                _clearTmp.text = "어려움";
                _clearTmp.color = _hardClearColor;
                _clearIconImg.color = _hardClearColor;
                break;
        }
    }



    public void Normal_Btn()
    {
        GameManager.Instance.PrepareStage(_stageInfo.number, DifficultyLevel.Normal);
    }



    public void Hard_Btn()
    {
        GameManager.Instance.PrepareStage(_stageInfo.number, DifficultyLevel.Hard);
    }
}



public enum DifficultyLevel
{
    None, Normal, Hard
}