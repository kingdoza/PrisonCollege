using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class HitMarkerUI : MonoBehaviour
{
    public static HitMarkerUI Instance { get; private set; }

    [Header("References")]
    [SerializeField] private RectTransform _markerRoot;
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private Image[] _markerImages;

    [Header("Hit")]
    [SerializeField] private Color _hitColor = Color.white;
    [SerializeField] private float _hitDuration = 0.12f;
    [SerializeField] private float _hitStartScale = 1.1f;

    [Header("Kill")]
    [SerializeField] private Color _killColor = new Color(1f, 0.15f, 0.05f, 1f);
    [SerializeField] private float _killDuration = 0.3f;
    [SerializeField] private float _killStartScale = 1.45f;

    private Tween _feedbackTween;



    private void Reset()
    {
        _markerRoot = GetComponent<RectTransform>();
        _canvasGroup = GetComponent<CanvasGroup>();
        _markerImages = GetComponentsInChildren<Image>(true);
    }



    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[HitMarkerUI] Duplicate instance was destroyed.");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        ResolveReferences();
        HideImmediate();
    }



    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        _feedbackTween?.Kill();
    }



    public void PlayHit()
    {
        Play(_hitColor, _hitDuration, _hitStartScale);
    }



    public void PlayKill()
    {
        Play(_killColor, _killDuration, _killStartScale);
    }



    private void Play(Color color, float duration, float startScale)
    {
        if (!HasRequiredReferences()) return;

        _feedbackTween?.Kill();
        SetColor(color);

        _canvasGroup.alpha = 1f;
        _markerRoot.localScale = Vector3.one * startScale;

        _feedbackTween = DOTween.Sequence()
            .SetUpdate(true)
            .Append(_markerRoot.DOScale(Vector3.one, duration).SetEase(Ease.OutCubic))
            .Join(_canvasGroup.DOFade(0f, duration).SetEase(Ease.InQuad));
    }



    private void ResolveReferences()
    {
        if (_markerRoot == null)
            _markerRoot = GetComponent<RectTransform>();

        if (_canvasGroup == null)
            _canvasGroup = GetComponent<CanvasGroup>();

        if (_canvasGroup == null)
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();

        if (_markerImages == null || _markerImages.Length == 0)
            _markerImages = GetComponentsInChildren<Image>(true);
    }



    private bool HasRequiredReferences()
    {
        ResolveReferences();
        return _markerRoot != null
            && _canvasGroup != null
            && _markerImages != null
            && _markerImages.Length > 0;
    }



    private void SetColor(Color color)
    {
        foreach (Image image in _markerImages)
        {
            if (image != null)
                image.color = color;
        }
    }



    private void HideImmediate()
    {
        if (_canvasGroup == null) return;

        _canvasGroup.alpha = 0f;
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;
    }
}
