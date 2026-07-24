using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StageTimerHUDView : MonoBehaviour
{
    [SerializeField] private TMP_Text _remainingTimeText;
    [SerializeField] private Image _remainingTimeFill;
    [Tooltip("정규 스테이지에서 정확한 전체 제한 시간을 읽을 Timer Stat입니다. 비워 두면 최초 관측 시간을 전체 시간으로 사용합니다.")]
    [SerializeField] private Stat _normalTimerStat;
    [SerializeField, Min(0f)] private float _warningThreshold = 10f;
    [SerializeField] private Color _warningColor = Color.red;

    private StageController _source;
    private Color _originalTextColor;
    private Color _originalFillColor;
    private float _totalTime;
    private bool _tutorialTimerStarted;
    private bool _initialized;

    public bool Initialize(StageController source)
    {
        Shutdown();
        if (source == null || _remainingTimeText == null || _remainingTimeFill == null)
        {
            Debug.LogError("StageTimerHUDView의 StageController, 시간 TMP 또는 Fill Image 참조가 누락됐습니다.", this);
            return false;
        }

        _source = source;
        _originalTextColor = _remainingTimeText.color;
        _originalFillColor = _remainingTimeFill.color;
        _tutorialTimerStarted = !source.IsTutorialRuntime;
        _totalTime = _normalTimerStat != null && !source.IsTutorialRuntime
            ? Mathf.Max(0f, _normalTimerStat.Max)
            : Mathf.Max(0f, source.TimerRemaining);
        _initialized = true;
        Refresh();
        return true;
    }

    public void Shutdown()
    {
        if (_initialized)
        {
            _remainingTimeText.color = _originalTextColor;
            _remainingTimeFill.color = _originalFillColor;
        }

        _source = null;
        _initialized = false;
    }

    private void Update()
    {
        if (_initialized)
            Refresh();
    }

    private void Refresh()
    {
        float remaining = Mathf.Max(0f, _source.TimerRemaining);

        if (_source.IsTutorialRuntime && !_tutorialTimerStarted)
        {
            if (remaining <= 0f)
            {
                _remainingTimeText.text = "-";
                _remainingTimeText.color = _originalTextColor;
                _remainingTimeFill.color = _originalFillColor;
                _remainingTimeFill.fillAmount = 0f;
                return;
            }

            _tutorialTimerStarted = true;
            _totalTime = remaining;
        }

        if (!_source.IsTutorialRuntime && _normalTimerStat != null)
            _totalTime = Mathf.Max(0f, _normalTimerStat.Max);
        else if (remaining > _totalTime)
            _totalTime = remaining;

        _remainingTimeText.text = Mathf.CeilToInt(remaining).ToString();
        _remainingTimeFill.fillAmount = _totalTime > 0f
            ? Mathf.Clamp01(remaining / _totalTime)
            : 0f;

        bool warning = remaining <= _warningThreshold;
        _remainingTimeText.color = warning ? _warningColor : _originalTextColor;
        _remainingTimeFill.color = warning ? _warningColor : _originalFillColor;
    }
}
