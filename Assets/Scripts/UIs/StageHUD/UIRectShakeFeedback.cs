using System;
using DG.Tweening;
using UnityEngine;

[Serializable]
public sealed class UIRectShakeFeedback
{
    [Tooltip("LayoutGroup이 직접 제어하지 않는 내부 Visual RectTransform을 연결합니다.")]
    [SerializeField] private RectTransform _target;
    [SerializeField, Min(0f)] private float _duration = 0.25f;
    [SerializeField, Min(0f)] private float _strength = 10f;
    [SerializeField, Min(1)] private int _vibrato = 12;
    [SerializeField, Range(0f, 180f)] private float _randomness = 90f;
    [SerializeField] private bool _fadeOut = true;

    private Tween _tween;
    private Vector2 _originalAnchoredPosition;
    private bool _initialized;

    public bool IsValid => _target != null;

    public void Initialize()
    {
        Shutdown();
        if (_target == null) return;

        _originalAnchoredPosition = _target.anchoredPosition;
        _initialized = true;
    }

    public void Play()
    {
        if (_target == null) return;
        if (!_initialized)
            Initialize();

        StopAndRestore();
        if (_duration <= 0f || _strength <= 0f) return;

        Tween shake = _target
            .DOShakeAnchorPos(
                _duration,
                _strength,
                Mathf.Max(1, _vibrato),
                Mathf.Clamp(_randomness, 0f, 180f),
                false,
                _fadeOut)
            .SetUpdate(true)
            .SetLink(_target.gameObject, LinkBehaviour.KillOnDisable);

        _tween = shake;
        shake.OnComplete(() => Finish(shake));
        shake.OnKill(() => Finish(shake));
    }

    public void Shutdown()
    {
        StopAndRestore();
        _initialized = false;
    }

    private void StopAndRestore()
    {
        Tween tween = _tween;
        _tween = null;
        if (tween != null && tween.IsActive())
            tween.Kill(false);

        RestoreOriginalPosition();
    }

    private void Finish(Tween tween)
    {
        if (_tween != tween) return;

        _tween = null;
        RestoreOriginalPosition();
    }

    private void RestoreOriginalPosition()
    {
        if (_initialized && _target != null)
            _target.anchoredPosition = _originalAnchoredPosition;
    }
}
