using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public sealed class UIButtonHoverVisualController : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    ISelectHandler,
    IDeselectHandler
{
    [Serializable]
    private sealed class GraphicColorGroup
    {
        [SerializeField]
        [Tooltip("이 그룹에 속한 Graphic들이 Hover 상태에서 사용할 공통 색상입니다.")]
        private Color _hoverColor = Color.white;

        [SerializeField]
        [Tooltip("Image, RawImage, TMP_Text 등 색상을 변경할 Graphic 목록입니다.")]
        private Graphic[] _graphics = Array.Empty<Graphic>();

        public Color HoverColor => _hoverColor;
        public IReadOnlyList<Graphic> Graphics => _graphics;
    }

    [Header("Background")]
    [SerializeField]
    [Tooltip("버튼의 배경 Graphic입니다.")]
    private Graphic _background;

    [SerializeField]
    [Tooltip("배경이 Hover 상태에서 사용할 색상입니다.")]
    private Color _backgroundHoverColor = Color.white;

    [Header("Colored Graphics")]
    [SerializeField]
    [Tooltip("그룹마다 서로 다른 Hover 색상을 지정할 수 있습니다.")]
    private GraphicColorGroup[] _colorGroups = Array.Empty<GraphicColorGroup>();

    [Header("Decorations")]
    [SerializeField]
    [Tooltip("Hover 상태에서만 활성화할 데코 오브젝트입니다.")]
    private GameObject[] _hoverDecorations = Array.Empty<GameObject>();

    [Header("Transition")]
    [SerializeField, Min(0f)]
    [Tooltip("원래 색상과 Hover 색상 사이의 전환 시간입니다.")]
    private float _transitionDuration = 0.15f;

    [SerializeField]
    [Tooltip("색상 전환에 사용할 Ease입니다.")]
    private Ease _transitionEase = Ease.OutQuad;

    private readonly Dictionary<Graphic, Color> _normalColors = new();
    private readonly List<Tween> _activeTweens = new();
    private readonly HashSet<GameObject> _activeDecorations = new();

    private Selectable _selectable;
    private bool _pointerInside;
    private bool _selected;
    private bool _visualStateInitialized;
    private bool _isHoverVisualActive;
    private bool _wasInteractable;
    private bool _configurationDirty;

    private void Awake()
    {
        _selectable = GetComponent<Selectable>();
        CaptureNewNormalColors();
    }

    private void OnEnable()
    {
        if (_selectable == null)
        {
            _selectable = GetComponent<Selectable>();
        }

        CaptureNewNormalColors();

        _pointerInside = false;
        _selected = false;
        _configurationDirty = false;
        _wasInteractable = CanShowHover();
        SetHoverVisual(false, true, true);
    }

    private void Update()
    {
        if (_configurationDirty)
        {
            _configurationDirty = false;
            RefreshConfiguration();
        }

        bool isInteractable = CanShowHover();
        if (isInteractable != _wasInteractable)
        {
            _wasInteractable = isInteractable;
            RefreshVisualState();
        }

        if (!_isHoverVisualActive && !HasActiveTweens())
        {
            UpdateNormalColorsFromCurrentGraphics();
        }
    }

    private void OnDisable()
    {
        StopActiveTweens();
        RestoreNormalColorsImmediately();
        SetDecorationsActive(false);

        _pointerInside = false;
        _selected = false;
        _visualStateInitialized = false;
        _isHoverVisualActive = false;
        _configurationDirty = false;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        _transitionDuration = Mathf.Max(0f, _transitionDuration);

        if (Application.isPlaying)
        {
            _configurationDirty = true;
        }
    }
#endif

    public void OnPointerEnter(PointerEventData eventData)
    {
        _pointerInside = true;
        RefreshVisualState();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _pointerInside = false;
        RefreshVisualState();
    }

    public void OnSelect(BaseEventData eventData)
    {
        _selected = true;
        RefreshVisualState();
    }

    public void OnDeselect(BaseEventData eventData)
    {
        _selected = false;
        RefreshVisualState();
    }

    public void RefreshVisualState()
    {
        bool shouldShowHover = CanShowHover() && (_pointerInside || _selected);
        SetHoverVisual(shouldShowHover, false, false);
    }

    public void RefreshConfiguration()
    {
        CaptureNewNormalColors();

        bool shouldShowHover = CanShowHover() && (_pointerInside || _selected);
        SetHoverVisual(shouldShowHover, false, true);
    }

    private bool CanShowHover()
    {
        return _selectable != null &&
               _selectable.isActiveAndEnabled &&
               _selectable.IsInteractable();
    }

    private void CaptureNewNormalColors()
    {
        CaptureNormalColor(_background);

        if (_colorGroups != null)
        {
            for (int groupIndex = 0; groupIndex < _colorGroups.Length; groupIndex++)
            {
                GraphicColorGroup group = _colorGroups[groupIndex];
                if (group == null || group.Graphics == null)
                {
                    continue;
                }

                for (int graphicIndex = 0; graphicIndex < group.Graphics.Count; graphicIndex++)
                {
                    CaptureNormalColor(group.Graphics[graphicIndex]);
                }
            }
        }
    }

    private void CaptureNormalColor(Graphic graphic)
    {
        if (graphic == null || _normalColors.ContainsKey(graphic))
        {
            return;
        }

        _normalColors.Add(graphic, graphic.color);
    }

    private void SetHoverVisual(bool active, bool immediate, bool force)
    {
        if (!force && _visualStateInitialized && _isHoverVisualActive == active)
        {
            return;
        }

        _visualStateInitialized = true;
        _isHoverVisualActive = active;

        StopActiveTweens();
        SetDecorationsActive(active);

        foreach (KeyValuePair<Graphic, Color> entry in _normalColors)
        {
            Graphic graphic = entry.Key;
            if (graphic == null)
            {
                continue;
            }

            Color targetColor = entry.Value;
            if (active && TryGetCurrentHoverColor(graphic, out Color hoverColor))
            {
                targetColor = hoverColor;
            }

            AnimateColor(graphic, targetColor, immediate);
        }
    }

    private bool TryGetCurrentHoverColor(Graphic graphic, out Color hoverColor)
    {
        bool found = false;
        hoverColor = default;

        if (_background == graphic)
        {
            hoverColor = _backgroundHoverColor;
            found = true;
        }

        if (_colorGroups == null)
        {
            return found;
        }

        for (int groupIndex = 0; groupIndex < _colorGroups.Length; groupIndex++)
        {
            GraphicColorGroup group = _colorGroups[groupIndex];
            if (group == null || group.Graphics == null)
            {
                continue;
            }

            for (int graphicIndex = 0; graphicIndex < group.Graphics.Count; graphicIndex++)
            {
                if (group.Graphics[graphicIndex] != graphic)
                {
                    continue;
                }

                hoverColor = group.HoverColor;
                found = true;
            }
        }

        return found;
    }

    private void AnimateColor(Graphic graphic, Color targetColor, bool immediate)
    {
        if (immediate || _transitionDuration <= 0f || !Application.isPlaying)
        {
            graphic.color = targetColor;
            return;
        }

        Tween tween = graphic
            .DOColor(targetColor, _transitionDuration)
            .SetEase(_transitionEase)
            .SetUpdate(true)
            .SetTarget(this);

        _activeTweens.Add(tween);
    }

    private void StopActiveTweens()
    {
        for (int i = 0; i < _activeTweens.Count; i++)
        {
            Tween tween = _activeTweens[i];
            if (tween != null && tween.IsActive())
            {
                tween.Kill();
            }
        }

        _activeTweens.Clear();
    }

    private bool HasActiveTweens()
    {
        for (int i = 0; i < _activeTweens.Count; i++)
        {
            Tween tween = _activeTweens[i];
            if (tween != null && tween.IsActive())
            {
                return true;
            }
        }

        return false;
    }

    private void UpdateNormalColorsFromCurrentGraphics()
    {
        if (_background != null)
        {
            _normalColors[_background] = _background.color;
        }

        if (_colorGroups == null)
        {
            return;
        }

        for (int groupIndex = 0; groupIndex < _colorGroups.Length; groupIndex++)
        {
            GraphicColorGroup group = _colorGroups[groupIndex];
            if (group == null || group.Graphics == null)
            {
                continue;
            }

            for (int graphicIndex = 0; graphicIndex < group.Graphics.Count; graphicIndex++)
            {
                Graphic graphic = group.Graphics[graphicIndex];
                if (graphic != null)
                {
                    _normalColors[graphic] = graphic.color;
                }
            }
        }
    }

    private void RestoreNormalColorsImmediately()
    {
        foreach (KeyValuePair<Graphic, Color> entry in _normalColors)
        {
            if (entry.Key != null)
            {
                entry.Key.color = entry.Value;
            }
        }
    }

    private void SetDecorationsActive(bool active)
    {
        foreach (GameObject decoration in _activeDecorations)
        {
            if (decoration != null && decoration != gameObject)
            {
                decoration.SetActive(false);
            }
        }

        _activeDecorations.Clear();

        if (_hoverDecorations == null)
        {
            return;
        }

        for (int i = 0; i < _hoverDecorations.Length; i++)
        {
            GameObject decoration = _hoverDecorations[i];
            if (decoration == null || decoration == gameObject)
            {
                continue;
            }

            decoration.SetActive(active);
            if (active)
            {
                _activeDecorations.Add(decoration);
            }
        }
    }
}
