using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class InteractionUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _promptText;
    [SerializeField] private Image _fillImage;
    [Range(0, 1)] private float _fullAlpha = 1;
    [SerializeField] private float _fadeDuration = 0.2f;
    private CanvasGroup _canvasGroup;
    private Vector3 _fullScale;



    private void Awake()
    {
        _fullScale = transform.localScale;
        _canvasGroup = GetComponent<CanvasGroup>();
        _canvasGroup.alpha = 0f;
    }



    public void Show(string message)
    {
        _promptText.text = message;
        Fill(0);
        _canvasGroup.DOKill();
        transform.DOKill();
        transform.localScale = _fullScale * 0.8f;
        transform.DOScale(_fullScale, _fadeDuration).SetEase(Ease.OutBack);
        _canvasGroup.DOFade(_fullAlpha, _fadeDuration)
                    .SetUpdate(true);
    }


    public void Hide()
    {
        _canvasGroup.DOKill();
        _canvasGroup.DOFade(0f, _fadeDuration)
                    .SetUpdate(true);
    }



    public void Fill(float amount)
    {
        _fillImage.fillAmount = amount;
    }
}
