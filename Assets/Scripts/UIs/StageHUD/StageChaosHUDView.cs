using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StageChaosHUDView : MonoBehaviour
{
    [SerializeField] private Stat _chaosStat;
    [SerializeField] private Image _chaosProgressFill;
    [SerializeField] private TMP_Text _increaseArrowsText;
    [SerializeField] private TMP_Text _decreaseArrowsText;
    [SerializeField, Min(1)] private int _arrowCount = 5;
    [Tooltip("초당 혼란 변화량 1당 화살표가 이동하는 칸 수/초")]
    [SerializeField, Min(0f)] private float _rateStepCoefficient = 1f;
    [SerializeField] private Color _normalArrowColor = Color.white;
    [SerializeField] private Color _increaseHighlightColor = Color.red;
    [SerializeField] private Color _decreaseHighlightColor = Color.green;

    [Header("Shake Feedback")]
    [SerializeField] private UIRectShakeFeedback _increaseShake = new();
    [Tooltip("지속 혼란 증가 중 Shake를 다시 실행할 수 있는 최소 간격")]
    [SerializeField, Min(0f)] private float _continuousShakeCooldown = 0.8f;

    private StageController _source;
    private int _direction;
    private int _highlightIndex;
    private float _stepAccumulator;
    private float _nextContinuousShakeTime;
    private int _lastRenderedDirection;
    private int _lastRenderedIndex = -1;
    private int _lastRenderedArrowCount = -1;
    private Color _lastRenderedNormalColor;
    private Color _lastRenderedHighlightColor;
    private bool _continuousIncreaseActive;
    private bool _initialized;

    public bool Initialize(StageController source)
    {
        Shutdown();
        if (source == null
            || _chaosStat == null
            || _chaosProgressFill == null
            || _increaseArrowsText == null
            || _decreaseArrowsText == null
            || _increaseShake == null
            || !_increaseShake.IsValid)
        {
            Debug.LogError("StageChaosHUDView의 StageController, Chaos Stat, Fill, 화살표 TMP 또는 Shake Visual 참조가 누락됐습니다.", this);
            return false;
        }

        _source = source;
        _increaseArrowsText.richText = true;
        _decreaseArrowsText.richText = true;
        _increaseShake.Initialize();
        _initialized = true;
        Refresh();
        _source.ChaosChanged += OnChaosChanged;
        return true;
    }

    public void Shutdown()
    {
        if (_source != null)
            _source.ChaosChanged -= OnChaosChanged;
        _increaseShake?.Shutdown();

        if (_increaseArrowsText != null)
            _increaseArrowsText.gameObject.SetActive(false);
        if (_decreaseArrowsText != null)
            _decreaseArrowsText.gameObject.SetActive(false);

        _source = null;
        _direction = 0;
        _stepAccumulator = 0f;
        _nextContinuousShakeTime = 0f;
        _continuousIncreaseActive = false;
        _initialized = false;
        InvalidateRender();
    }

    private void Update()
    {
        if (_initialized)
            Refresh();
    }

    private void Refresh()
    {
        _chaosProgressFill.fillAmount = _chaosStat.Max > 0f
            ? Mathf.Clamp01(_chaosStat.Current / _chaosStat.Max)
            : 0f;

        float rate = _source.ChaosRate;
        int nextDirection = Mathf.Approximately(rate, 0f) ? 0 : (rate > 0f ? 1 : -1);
        int count = Mathf.Max(1, _arrowCount);

        if (_continuousIncreaseActive && nextDirection <= 0)
        {
            _continuousIncreaseActive = false;
            _nextContinuousShakeTime = 0f;
        }

        if (nextDirection == 0)
        {
            if (_direction != 0)
            {
                _increaseArrowsText.gameObject.SetActive(false);
                _decreaseArrowsText.gameObject.SetActive(false);
                _direction = 0;
                _stepAccumulator = 0f;
                InvalidateRender();
            }
            return;
        }

        if (_direction != nextDirection || _highlightIndex >= count)
        {
            _direction = nextDirection;
            _highlightIndex = nextDirection > 0 ? 0 : count - 1;
            _stepAccumulator = 0f;
            InvalidateRender();
        }

        float stepsPerSecond = Mathf.Abs(rate) * _rateStepCoefficient;
        _stepAccumulator += stepsPerSecond * Time.deltaTime;
        int completedSteps = Mathf.FloorToInt(_stepAccumulator);
        if (completedSteps > 0)
        {
            _stepAccumulator -= completedSteps;
            int signedSteps = _direction > 0 ? completedSteps : -completedSteps;
            _highlightIndex = PositiveModulo(_highlightIndex + signedSteps, count);
        }

        RenderArrows(count);
    }

    private void OnChaosChanged(ChaosChangedData data)
    {
        if (!_initialized || data.delta <= 0f || data.reason == ChaosChangeReason.Reset)
            return;

        if (data.reason != ChaosChangeReason.ContinuousHazard)
        {
            _increaseShake.Play();
            return;
        }

        float now = Time.unscaledTime;
        if (!_continuousIncreaseActive || now >= _nextContinuousShakeTime)
        {
            _increaseShake.Play();
            _nextContinuousShakeTime = now + _continuousShakeCooldown;
        }
        _continuousIncreaseActive = true;
    }

    private void RenderArrows(int count)
    {
        bool increasing = _direction > 0;
        Color highlight = increasing ? _increaseHighlightColor : _decreaseHighlightColor;

        if (_lastRenderedDirection == _direction
            && _lastRenderedIndex == _highlightIndex
            && _lastRenderedArrowCount == count
            && _lastRenderedNormalColor == _normalArrowColor
            && _lastRenderedHighlightColor == highlight)
        {
            return;
        }

        TMP_Text activeText = increasing ? _increaseArrowsText : _decreaseArrowsText;
        TMP_Text inactiveText = increasing ? _decreaseArrowsText : _increaseArrowsText;
        char arrow = increasing ? '▶' : '◀';
        string normalHex = ColorUtility.ToHtmlStringRGBA(_normalArrowColor);
        string highlightHex = ColorUtility.ToHtmlStringRGBA(highlight);
        StringBuilder builder = new StringBuilder(count * 32);

        for (int i = 0; i < count; i++)
        {
            string colorHex = i == _highlightIndex ? highlightHex : normalHex;
            builder.Append("<color=#").Append(colorHex).Append('>').Append(arrow).Append("</color>");
        }

        inactiveText.gameObject.SetActive(false);
        activeText.gameObject.SetActive(true);
        activeText.text = builder.ToString();
        _lastRenderedDirection = _direction;
        _lastRenderedIndex = _highlightIndex;
        _lastRenderedArrowCount = count;
        _lastRenderedNormalColor = _normalArrowColor;
        _lastRenderedHighlightColor = highlight;
    }

    private void InvalidateRender()
    {
        _lastRenderedDirection = 0;
        _lastRenderedIndex = -1;
        _lastRenderedArrowCount = -1;
    }

    private static int PositiveModulo(int value, int divisor)
    {
        int result = value % divisor;
        return result < 0 ? result + divisor : result;
    }
}
