using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Hover : MonoBehaviour, IPointerEnterHandler ,IPointerExitHandler
{
    [System.Serializable]
    public class ColorChangeEntry
    {
        public Graphic graphic;
        public Color hoverColor;
    }


    [SerializeField] private ColorChangeEntry[] _colorChangeEntries;
    private Color[] _originColors;



    private void Awake()
    {
        _originColors = new Color[_colorChangeEntries.Length];
        for (int i = 0;  i < _colorChangeEntries.Length; i++)
        {
            _originColors[i] = _colorChangeEntries[i].graphic.color;
        }
    }




    public void OnPointerExit(PointerEventData eventData)
    {
        for (int i = 0; i < _colorChangeEntries.Length; i++)
        {
            _colorChangeEntries[i].graphic.color = _originColors[i];
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        for (int i = 0; i < _colorChangeEntries.Length; i++)
        {
            _colorChangeEntries[i].graphic.color = _colorChangeEntries[i].hoverColor;
        }
    }
}