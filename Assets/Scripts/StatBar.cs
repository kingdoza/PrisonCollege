using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;



public class StatBar : MonoBehaviour
{
    [SerializeField] protected Stat _targetStat;
    [SerializeField] protected Image _fillImage;
    [SerializeField] protected Gradient _colorGradient;



    //protected virtual void Start()
    //{
    //    OnStatChanged(0);
    //    _targetStat?.IncreaseEvent.AddListener(OnStatChanged);
    //    _targetStat?.DecreaseEvent.AddListener(OnStatChanged);
    //}



    protected virtual void Start()
    {
        // 초기 설정이 되어있다면 구독, 없으면 나중에 SetTarget으로 설정
        if (_targetStat != null) BindEvents();
        UpdateUI(_targetStat != null ? _targetStat.Ratio : 0);
    }


    public virtual void SetTarget(Stat newStat)
    {
        // 기존 스탯 구독 해제 (중요! 메모리 누수 방지)
        if (_targetStat != null)
        {
            _targetStat.IncreaseEvent.RemoveListener(_ => OnStatChanged());
            _targetStat.DecreaseEvent.RemoveListener(_ => OnStatChanged());
            _targetStat.ResetEvent.RemoveListener(_ => OnStatChanged());
        }

        _targetStat = newStat;

        if (_targetStat != null)
        {
            BindEvents();
            UpdateUI(_targetStat.Ratio);
        }
        else
        {
            UpdateUI(0);
        }
    }

    private void BindEvents()
    {
        _targetStat.IncreaseEvent.AddListener(_ => OnStatChanged());
        _targetStat.DecreaseEvent.AddListener(_ => OnStatChanged());
        _targetStat.ResetEvent.AddListener(_ => OnStatChanged());
    }



    protected virtual void OnStatChanged()
    {
        UpdateUI(_targetStat.Ratio);
    }



    protected void UpdateUI(float ratio)
    {
        float clampedRatio = Mathf.Clamp01(ratio);
        _fillImage.fillAmount = clampedRatio;
        _fillImage.color = _colorGradient.Evaluate(clampedRatio);
    }
}
