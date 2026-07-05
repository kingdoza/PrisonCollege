using UnityEngine;

public class Crosshair : MonoBehaviour
{
    [SerializeField] private RectTransform _rangeCrosshairParent;
    private float sizeMultiplier;
    private RectTransform[] _rangeParts;



    private void Awake()
    {
        _rangeParts = new RectTransform[_rangeCrosshairParent.childCount];
        for (int i = 0; i < _rangeCrosshairParent.childCount; i++)
        {
            _rangeParts[i] = _rangeCrosshairParent.GetChild(i).GetComponent<RectTransform>();
        }
        sizeMultiplier = Screen.height * 1.5f;
    }



    public void ShowRanged(float spreadIntensity)
    {
        float currentSpread = spreadIntensity * sizeMultiplier;
        _rangeParts[0].anchoredPosition = new Vector2(0, currentSpread);  // Top
        _rangeParts[1].anchoredPosition = new Vector2(0, -currentSpread); // Bottom
        _rangeParts[2].anchoredPosition = new Vector2(-currentSpread, 0); // Left
        _rangeParts[3].anchoredPosition = new Vector2(currentSpread, 0);  // Right
        _rangeCrosshairParent.gameObject.SetActive(true);
    }



    public void HideRanged()
    {
        _rangeCrosshairParent.gameObject.SetActive(false);
    }
}
