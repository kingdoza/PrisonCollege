using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

public class FireSuppressionSystem : SceneSingleton<FireSuppressionSystem>
{
    [SerializeField] private SprinklerController _sprinklerCtrl;
    [SerializeField] private float _lowerPosY = -1.6f;
    [SerializeField] private float _holdDuration = 2f;
    [SerializeField] private Transform _floodTransform;
    [HideInInspector] public UnityEvent FireExtinguishEvent = new();

    private Stat _floodProgress;

    private DG.Tweening.Sequence _floodSequence;
    private bool _isFlooding = false;

    public float FloodFillRatio => _floodProgress.Ratio;



    protected override void Awake()
    {
        base.Awake();
        _floodTransform.gameObject.SetActive(false);
        _floodProgress = GetComponent<Stat>();
        _floodProgress.Initialize(true);
    }


    private void Start()
    {
        _floodTransform.gameObject.SetActive(false);
        _sprinklerCtrl.TurnOffImmediate();
    }


    public void StartSuppression()
    {
        if (_isFlooding) return;
        ExtinguishAllFires();
        RaiseWater();
    }



    private void RaiseWater()
    {
        _floodSequence?.Kill();

        _floodProgress.Initialize(issetToZero: true);
        float duration = _floodProgress.Max;

        _floodTransform.localPosition = new Vector3(_floodTransform.localPosition.x, _lowerPosY, _floodTransform.localPosition.z);

        _floodSequence = DOTween.Sequence();

        _floodSequence.OnStart(() => {
            _isFlooding = true;
            _sprinklerCtrl.TurnOn();
            _floodTransform.gameObject.SetActive(true);
            Debug.Log("침수 시작: _isFlooding = true");
        });

        _floodSequence.Append(DOTween.To(() => 0f,
            x => UpdateFlood(x),
            duration,
            duration).SetEase(Ease.Linear));

        _floodSequence.AppendInterval(_holdDuration);

        _floodSequence.AppendCallback(() => {
            _sprinklerCtrl.TurnOff();
        });

        _floodSequence.Append(DOTween.To(() => _floodProgress.Max,
            x => UpdateFlood(x),
            0f,
            duration).SetEase(Ease.Linear));

        _floodSequence.OnComplete(() => {
            _isFlooding = false;
            _floodTransform.gameObject.SetActive(false);
            Debug.Log("침수 사이클 종료: _isFlooding = false");
        });

        _floodSequence.SetTarget(this);
    }



    private void UpdateFlood(float currentVal)
    {
        float delta = currentVal - _floodProgress.Current;
        if (delta > 0)
        {
            _floodProgress.Increase(delta);
        }
        else
        {
            _floodProgress.Decrease(-delta);
        }

        float newY = Mathf.Lerp(_lowerPosY, 0f, _floodProgress.Ratio);
        _floodTransform.localPosition = new Vector3(_floodTransform.localPosition.x, newY, _floodTransform.localPosition.z);
    }




    private void ExtinguishAllFires()
    {
        FireExtinguishEvent?.Invoke();
    }
}
