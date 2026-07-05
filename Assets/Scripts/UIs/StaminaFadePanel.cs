using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class StaminaFadePanel : MonoBehaviour
{
    [SerializeField] private Image _targetImg;
    [SerializeField] private float _fadeoutDuration;
    private Color _originColor;



    private void Awake()
    {
        _originColor = _targetImg.color;
        StageController.Instance.Player.StaminaRunoutEvent.AddListener(StartFadeout);
    }



    private void Start()
    {
        _targetImg.color = Color.clear;
    }



    private void StartFadeout()
    {
        _targetImg.DOKill();
        _targetImg.color = _originColor;
        _targetImg.DOFade(0f, _fadeoutDuration)
            .SetEase(Ease.InQuad) // 부드러운 가속도 추가
            .OnComplete(() => {
            });
    }
}
