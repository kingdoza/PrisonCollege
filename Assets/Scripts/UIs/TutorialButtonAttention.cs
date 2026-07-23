using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class TutorialButtonAttention : MonoBehaviour
{
    [Header("9-Slice Frame")]
    [Tooltip("튜토리얼 버튼의 Stretch 자식으로 배치한 9-Slice 테두리 RectTransform입니다.")]
    [SerializeField] private RectTransform _frameRect;
    [Tooltip("Type을 Sliced로, Raycast Target과 Fill Center를 끈 테두리 Image입니다.")]
    [SerializeField] private Image _frameImage;

    [Header("Unscaled Animation")]
    [Tooltip("애니메이션 시작 시 버튼 바깥쪽으로 벌어지는 여백입니다.")]
    [Min(0f)] [SerializeField] private float _outerPadding = 28f;
    [Tooltip("테두리가 버튼에 도착했을 때 남기는 여백입니다.")]
    [Min(0f)] [SerializeField] private float _targetPadding = 6f;
    [Min(0f)] [SerializeField] private float _convergeDuration = 0.55f;
    [Min(0f)] [SerializeField] private float _fadeInDuration = 0.1f;
    [Min(0f)] [SerializeField] private float _targetHoldDuration = 0.1f;
    [Min(0f)] [SerializeField] private float _fadeOutDuration = 0.3f;
    [Tooltip("강조 활성화 직후와 각 반복 사이에 기다리는 시간입니다.")]
    [Min(0f)] [SerializeField] private float _repeatDelay = 0.6f;
    [Range(0f, 1f)] [SerializeField] private float _maximumAlpha = 0.9f;

    private Coroutine _attentionRoutine;
    private Color _configuredColor;
    private bool _attentionActive;
    private bool _hasValidReferences;



    private void Awake()
    {
        _hasValidReferences = ValidateReferences(true);
        if (!_hasValidReferences) return;

        _configuredColor = _frameImage.color;
        _frameImage.gameObject.SetActive(true);
        _frameImage.raycastTarget = false;
        _frameImage.fillCenter = false;
        HideFrame();
    }



    private void OnEnable()
    {
        if (_attentionActive && _hasValidReferences)
            StartAttentionRoutine();
    }



    private void OnDisable()
    {
        StopAttentionRoutine();
        HideFrame();
    }



    public bool SetAttentionActive(bool active)
    {
        _attentionActive = active;
        if (!_hasValidReferences)
        {
            StopAttentionRoutine();
            return !active;
        }

        if (!active)
        {
            StopAttentionRoutine();
            HideFrame();
            return true;
        }

        if (isActiveAndEnabled)
            StartAttentionRoutine();
        return true;
    }



    private void StartAttentionRoutine()
    {
        StopAttentionRoutine();
        _attentionRoutine = StartCoroutine(AttentionLoop());
    }



    private void StopAttentionRoutine()
    {
        if (_attentionRoutine == null) return;
        StopCoroutine(_attentionRoutine);
        _attentionRoutine = null;
    }



    private IEnumerator AttentionLoop()
    {
        ApplyFrame(_outerPadding, 0f);
        yield return WaitUnscaled(_repeatDelay);

        while (_attentionActive)
        {
            ApplyFrame(_outerPadding, 0f);

            float elapsed = 0f;
            while (elapsed < _convergeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float normalized = _convergeDuration <= 0f
                    ? 1f
                    : Mathf.Clamp01(elapsed / _convergeDuration);
                float eased = 1f - Mathf.Pow(1f - normalized, 3f);
                float fadeIn = _fadeInDuration <= 0f
                    ? 1f
                    : Mathf.Clamp01(elapsed / _fadeInDuration);
                ApplyFrame(
                    Mathf.LerpUnclamped(_outerPadding, _targetPadding, eased),
                    Mathf.Lerp(0f, _maximumAlpha, fadeIn));
                yield return null;
            }

            ApplyFrame(_targetPadding, _maximumAlpha);
            yield return WaitUnscaled(_targetHoldDuration);

            elapsed = 0f;
            while (elapsed < _fadeOutDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float normalized = _fadeOutDuration <= 0f
                    ? 1f
                    : Mathf.Clamp01(elapsed / _fadeOutDuration);
                ApplyFrame(_targetPadding, Mathf.Lerp(_maximumAlpha, 0f, normalized));
                yield return null;
            }

            ApplyFrame(_targetPadding, 0f);
            yield return WaitUnscaled(_repeatDelay);
        }

        _attentionRoutine = null;
        HideFrame();
    }



    private static IEnumerator WaitUnscaled(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }



    private void ApplyFrame(float padding, float alpha)
    {
        if (!_hasValidReferences) return;

        _frameRect.offsetMin = new Vector2(-padding, -padding);
        _frameRect.offsetMax = new Vector2(padding, padding);
        Color color = _configuredColor;
        color.a = Mathf.Clamp01(alpha);
        _frameImage.color = color;
    }



    private void HideFrame()
    {
        if (!_hasValidReferences) return;
        ApplyFrame(_outerPadding, 0f);
    }



    private bool ValidateReferences(bool logErrors)
    {
        if (_frameRect == null || _frameImage == null)
        {
            if (logErrors)
                Debug.LogError("TutorialButtonAttention의 9-Slice Frame Rect와 Image를 연결해야 합니다.", this);
            return false;
        }

        if (_outerPadding < _targetPadding)
        {
            if (logErrors)
                Debug.LogError("TutorialButtonAttention의 Outer Padding은 Target Padding 이상이어야 합니다.", this);
            return false;
        }

        if (_frameImage.type != Image.Type.Sliced && logErrors)
            Debug.LogWarning("TutorialButtonAttention Frame Image Type은 Sliced 사용을 권장합니다.", _frameImage);

        if ((_frameRect.anchorMin != Vector2.zero || _frameRect.anchorMax != Vector2.one)
            && logErrors)
        {
            Debug.LogWarning(
                "TutorialButtonAttention Frame Rect는 튜토리얼 버튼에 Stretch Anchor로 맞춰야 합니다.",
                _frameRect);
        }

        return true;
    }
}
