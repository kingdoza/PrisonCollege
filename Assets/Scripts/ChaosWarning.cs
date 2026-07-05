using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class ChaosWarning : MonoBehaviour
{
    [SerializeField] private Image _panelImg;
    [SerializeField] private TextMeshProUGUI _statIncreaseTmp;
    [SerializeField] private TextMeshProUGUI _descriptionTmp;

    private float _playTimeElapsed = 0;
    private bool _isPlaying = false;
    private float _velocity = 0;
    private float _duration = 0;
    private CanvasGroup _canvasGroup;
    private RectTransform _rectTransform;



    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _canvasGroup = GetComponent<CanvasGroup>();
    }




    private void Update()
    {
        if (!_isPlaying) return;
        _playTimeElapsed += Time.deltaTime;
        _rectTransform.anchoredPosition += Vector2.down * _velocity * Time.deltaTime;
        float alphaRatio = 1 - (_playTimeElapsed / _duration );
        _canvasGroup.alpha = alphaRatio;
    }



    public void Play(Info info, float velocity, float duration)
    {
        Destroy(gameObject, duration);
        _panelImg.color = info.PanelColor;
        _statIncreaseTmp.text = info.StatText;
        _descriptionTmp.text = info.Description;
        _canvasGroup.alpha = 1;
        _playTimeElapsed = 0;
        _velocity = velocity;
        _duration = duration;
        gameObject.SetActive(true);
        _isPlaying = true;
    }
}
