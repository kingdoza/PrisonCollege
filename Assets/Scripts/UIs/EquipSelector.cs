using TMPro;
using UnityEngine;

public class EquipSelector : SlotSelector
{
    [SerializeField] private TextMeshProUGUI _targetTmp;
    [SerializeField] private Color _selectedTextColor;
    private Color _originTextColor;

    protected override void Awake()
    {
        base.Awake();
        _originTextColor = _targetTmp.color;
    }


    public override void HighLight()
    {
        base.HighLight();
        _targetTmp.color = _selectedTextColor;
    }



    public override void Darken()
    {
        base.Darken();
        _targetTmp.color = _originTextColor;
    }
}
