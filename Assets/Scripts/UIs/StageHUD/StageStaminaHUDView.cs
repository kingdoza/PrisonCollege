using UnityEngine;
using UnityEngine.UI;

public class StageStaminaHUDView : MonoBehaviour
{
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private Image _staminaFill;
    [Tooltip("스태미나 고갈 시 잠시 빨간색으로 표시할 모든 Graphic")]
    [SerializeField] private Graphic[] _depletionFlashGraphics;
    [SerializeField, Min(0f)] private float _fullHoldDuration = 0.5f;
    [SerializeField, Min(0f)] private float _fullFadeDuration = 0.25f;
    [SerializeField, Min(0f)] private float _depletionFlashDuration = 0.25f;
    [SerializeField] private Color _depletionColor = Color.red;

    [Header("Shake Feedback")]
    [SerializeField] private UIRectShakeFeedback _depletionShake = new();
    [Tooltip("실제 고갈과 공격 거부가 함께 발생할 때 중복 Shake를 막는 최소 간격")]
    [SerializeField, Min(0f)] private float _shakeCooldown = 0.25f;

    private Professor _source;
    private Color[] _originalGraphicColors;
    private float _originalCanvasAlpha;
    private float _fullHoldRemaining;
    private float _flashRemaining;
    private float _nextShakeTime;
    private bool _wasFull;
    private bool _wasDepleted;
    private bool _flashApplied;
    private bool _initialized;

    public bool Initialize(Professor source)
    {
        Shutdown();
        if (source == null
            || _canvasGroup == null
            || _staminaFill == null
            || _depletionShake == null
            || !_depletionShake.IsValid)
        {
            Debug.LogError("StageStaminaHUDView의 Professor, CanvasGroup, Fill Image 또는 Shake Visual 참조가 누락됐습니다.", this);
            return false;
        }

        _source = source;
        _depletionShake.Initialize();
        _originalCanvasAlpha = _canvasGroup.alpha;
        _originalGraphicColors = new Color[_depletionFlashGraphics == null ? 0 : _depletionFlashGraphics.Length];
        for (int i = 0; i < _originalGraphicColors.Length; i++)
        {
            if (_depletionFlashGraphics[i] != null)
                _originalGraphicColors[i] = _depletionFlashGraphics[i].color;
        }

        float ratio = Mathf.Clamp01(_source.StaminaRatio);
        _wasFull = ratio >= 0.999f;
        _wasDepleted = ratio <= 0f;
        _fullHoldRemaining = _wasFull ? _fullHoldDuration : 0f;
        _canvasGroup.alpha = 1f;
        _staminaFill.fillAmount = ratio;
        _nextShakeTime = float.NegativeInfinity;
        _source.StaminaRunoutEvent.AddListener(OnStaminaFeedbackRequested);
        _source.StaminaDepleted += OnStaminaFeedbackRequested;
        _initialized = true;
        return true;
    }

    public void Shutdown()
    {
        if (_source != null)
        {
            _source.StaminaRunoutEvent.RemoveListener(OnStaminaFeedbackRequested);
            _source.StaminaDepleted -= OnStaminaFeedbackRequested;
        }
        _depletionShake?.Shutdown();

        if (_flashApplied)
            RestoreGraphicColors();
        if (_canvasGroup != null && _initialized)
            _canvasGroup.alpha = _originalCanvasAlpha;

        _source = null;
        _originalGraphicColors = null;
        _flashRemaining = 0f;
        _nextShakeTime = float.NegativeInfinity;
        _flashApplied = false;
        _initialized = false;
    }

    private void Update()
    {
        if (!_initialized) return;

        float deltaTime = Time.unscaledDeltaTime;
        float ratio = Mathf.Clamp01(_source.StaminaRatio);
        bool isFull = ratio >= 0.999f;
        bool isDepleted = ratio <= 0f;
        _staminaFill.fillAmount = ratio;

        if (!isFull)
        {
            _canvasGroup.alpha = 1f;
            _fullHoldRemaining = 0f;
        }
        else
        {
            if (!_wasFull)
            {
                _canvasGroup.alpha = 1f;
                _fullHoldRemaining = _fullHoldDuration;
            }

            if (_fullHoldRemaining > 0f)
            {
                _fullHoldRemaining = Mathf.Max(0f, _fullHoldRemaining - deltaTime);
            }
            else if (_fullFadeDuration <= 0f)
            {
                _canvasGroup.alpha = 0f;
            }
            else
            {
                _canvasGroup.alpha = Mathf.MoveTowards(
                    _canvasGroup.alpha,
                    0f,
                    deltaTime / _fullFadeDuration);
            }
        }

        if (isDepleted && !_wasDepleted)
        {
            _flashRemaining = _depletionFlashDuration;
            ApplyDepletionColor();
        }

        if (_flashRemaining > 0f)
        {
            _flashRemaining = Mathf.Max(0f, _flashRemaining - deltaTime);
            if (_flashRemaining <= 0f)
                RestoreGraphicColors();
        }

        _wasFull = isFull;
        _wasDepleted = isDepleted;
    }

    private void OnStaminaFeedbackRequested()
    {
        if (!_initialized) return;

        float now = Time.unscaledTime;
        if (now < _nextShakeTime) return;

        _depletionShake.Play();
        _nextShakeTime = now + _shakeCooldown;
    }

    private void ApplyDepletionColor()
    {
        if (_depletionFlashGraphics == null) return;
        for (int i = 0; i < _depletionFlashGraphics.Length; i++)
        {
            Graphic graphic = _depletionFlashGraphics[i];
            if (graphic == null) continue;
            Color color = _depletionColor;
            color.a = _originalGraphicColors[i].a;
            graphic.color = color;
        }
        _flashApplied = true;
    }

    private void RestoreGraphicColors()
    {
        if (_depletionFlashGraphics == null || _originalGraphicColors == null) return;
        for (int i = 0; i < _depletionFlashGraphics.Length; i++)
        {
            if (_depletionFlashGraphics[i] != null)
                _depletionFlashGraphics[i].color = _originalGraphicColors[i];
        }
        _flashApplied = false;
    }
}
