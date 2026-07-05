using DG.Tweening;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class BettingHelper : MonoBehaviour
{
    [SerializeField] private Image _leftBtnBackImg;
    [SerializeField] private TextMeshProUGUI _leftBtnTmp;
    [SerializeField] private Color _leftHightlightColor;
    [SerializeField] private GameObject _leftChooseBorder;

    [SerializeField] private Image _rightBtnBackImg;
    [SerializeField] private TextMeshProUGUI _rightBtnTmp;
    [SerializeField] private Color _rightHightlightColor;
    [SerializeField] private GameObject _rightChooseBorder;

    [SerializeField] private int _betAmount = 50;
    [SerializeField] private TextMeshProUGUI _continueTmp;
    [SerializeField] private TextMeshProUGUI _totalMoneyTmp;
    [SerializeField] private TextMeshProUGUI _betMoneyTmp;
    private Color _originalLeftColor;
    private Color _originalRightColor;
    private SelectedSide _selectedSide = SelectedSide.None;
    private bool _isStarted = false;

    public UnityEvent<SelectedSide, int> FightStartEvent = new();
    public UnityEvent<SelectedSide> SelectEvent = new();

    private int _totalMoney;
    private int _betMoney;



    private void Awake()
    {
        _betMoney = 0;
        _originalLeftColor = _leftBtnBackImg.color;
        _originalRightColor = _rightBtnBackImg.color;
        _totalMoney = InventorySystem.Instance.Money;
    }



    private void Start()
    {
        UpdateUIs();
        UpdateReaminedMoneyUI();

    }



    public void WriteButtonNameTmp(string leftName, string rightName)
    {
        _leftBtnTmp.text = leftName;
        _rightBtnTmp.text = rightName;
    }



    private void Update()
    {
        if ((Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) && _selectedSide != SelectedSide.None)
        {
            _isStarted = true;
            FightStartEvent?.Invoke(_selectedSide, _betMoney);
            gameObject.SetActive(false);
        }
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            IncreaseBet();
        }
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            DecreaseBet();
        }
    }



    public void UpdateTotalMoneyUI()
    {
        _totalMoney = InventorySystem.Instance.Money;
        UpdateReaminedMoneyUI();
    }



    public void IncreaseBet()
    {
        //if (_selectedSide == SelectedSide.None) return;
        if (_isStarted) return;
        _betMoney = Mathf.Min(_totalMoney, _betMoney + _betAmount);
        _betMoneyTmp.text = _betMoney.ToString("N0");
        UpdateReaminedMoneyUI();
        UpdateContinueTextUI();
    }



    public void DecreaseBet()
    {
        //if (_selectedSide == SelectedSide.None) return;
        if (_isStarted) return;
        int decreaseAmount = _betMoney % _betAmount == 0 ? _betAmount : _betMoney % _betAmount;
        _betMoney = Mathf.Max(0, _betMoney - decreaseAmount);
        _betMoneyTmp.text = _betMoney.ToString("N0");
        UpdateReaminedMoneyUI();
        UpdateContinueTextUI();
    }



    public void LeftSelected_Btn()
    {
        if (_selectedSide == SelectedSide.Left) return;
        if (_isStarted) return;
        _selectedSide = SelectedSide.Left;
        SelectEvent?.Invoke(_selectedSide);
        UpdateUIs();
    }



    public void RightSelected_Btn()
    {
        if (_selectedSide == SelectedSide.Right) return;
        if (_isStarted) return;
        _selectedSide = SelectedSide.Right;
        SelectEvent?.Invoke(_selectedSide);
        UpdateUIs();
    }



    private void UpdateUIs()
    {
        if (_selectedSide == SelectedSide.Left)
        {
            HighlightLeftButton();
            UnhighlightRightButton();
        }
        else if (_selectedSide == SelectedSide.Right)
        {
            UnhighlightLeftButton();
            HighlightRightButton();
        }
        else
        {
            UnhighlightLeftButton();
            UnhighlightRightButton();
        }
        UpdateContinueTextUI();
    }


    private void UpdateContinueTextUI()
    {
        if (_selectedSide == SelectedSide.None)
        {
            _continueTmp.text = "베팅 대상 선택하기";
            return;
        }
        string targetName = _selectedSide == SelectedSide.Left ? _leftBtnTmp.text : _rightBtnTmp.text;
        _continueTmp.text = $"Enter로 막고라 시작하기\r\n<size=80%>{targetName} 승리 시 : +{(_betMoney * 2).ToString("N0")}";//\r\n{targetName} 패배 시 : -{_betMoney.ToString("N0")}</size>";
    }



    private void UpdateReaminedMoneyUI()
    {
        _totalMoneyTmp.text = $"잔여 금액\r\n{(_totalMoney - _betMoney).ToString("N0")}";
    }



    private void HighlightLeftButton()
    {
        _leftBtnBackImg.color = _leftHightlightColor;
        _leftBtnTmp.color = Color.white;
        _leftChooseBorder.SetActive(true);
    }



    private void UnhighlightLeftButton()
    {
        _leftBtnBackImg.color = _originalLeftColor;
        _leftBtnTmp.color = Color.black;
        _leftChooseBorder.SetActive(false);
    }



    private void HighlightRightButton()
    {
        _rightBtnBackImg.color = _rightHightlightColor;
        _rightBtnTmp.color = Color.white;
        _rightChooseBorder.SetActive(true);
    }



    private void UnhighlightRightButton()
    {
        _rightBtnBackImg.color = _originalRightColor;
        _rightBtnTmp.color = Color.black;
        _rightChooseBorder.SetActive(false);
    }
}


[System.Serializable]
public enum SelectedSide
{
    None, Left, Right
}