using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ShopSelector : SlotSelector
{
    [System.Serializable]
    public class ColorChangeEntry
    {
        public Graphic graphic;
        public Color hoverColor;
    }


    [SerializeField] private ColorChangeEntry[] _colorChangeEntries;
    private Color[] _originColors;



    protected override void Awake()
    {
        base.Awake();
        _originColors = new Color[_colorChangeEntries.Length];
        for (int i = 0; i < _colorChangeEntries.Length; i++)
        {
            _originColors[i] = _colorChangeEntries[i].graphic.color;
        }
    }



    public override void HighLight()
    {
        base.HighLight();
        for (int i = 0; i < _colorChangeEntries.Length; i++)
        {
            _colorChangeEntries[i].graphic.color = _colorChangeEntries[i].hoverColor;
        }
    }



    public override void Darken()
    {
        base.Darken();
        for (int i = 0; i < _colorChangeEntries.Length; i++)
        {
            _colorChangeEntries[i].graphic.color = _originColors[i];
        }
    }
}
