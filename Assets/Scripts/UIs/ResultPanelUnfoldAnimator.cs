using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public sealed class ResultPanelUnfoldAnimator : MonoBehaviour
{
    [Header("Panel")]
    [Tooltip("Height가 변하는 결과 팝업의 RectTransform입니다. 비워 두면 이 컴포넌트의 RectTransform을 사용합니다.")]
    [SerializeField] private RectTransform _panelRect;
    [Tooltip("배경을 제외한 제목, 설명, 버튼의 공통 부모에 있는 CanvasGroup입니다.")]
    [SerializeField] private CanvasGroup _contentCanvasGroup;

    [Header("Unfold")]
    [SerializeField, Min(0f)] private float _minimumHeight = 56f;
    [SerializeField, Min(0f)] private float _unfoldDuration = 0.28f;

    private Tween _unfoldTween;
    private float _expandedHeight;
    private float _contentShownAlpha = 1f;
    private bool _contentShownInteractable = true;
    private bool _contentShownBlocksRaycasts = true;
    private bool _initializationAttempted;
    private bool _initialized;

    public bool Initialize()
    {
        if (_initialized)
            return true;
        if (_initializationAttempted)
            return false;

        _initializationAttempted = true;
        if (_panelRect == null)
            _panelRect = GetComponent<RectTransform>();

        if (_panelRect == null || _contentCanvasGroup == null)
        {
            Debug.LogError(
                "ResultPanelUnfoldAnimator의 Panel Rect 또는 Content Canvas Group 참조가 누락됐습니다.",
                this);
            return false;
        }

        if (_contentCanvasGroup.transform == _panelRect)
        {
            Debug.LogError(
                "Content Canvas Group은 애니메이션 패널 자체가 아니라 배경을 제외한 별도 Content 자식이어야 합니다.",
                this);
            return false;
        }

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(_panelRect);
        Canvas.ForceUpdateCanvases();

        _expandedHeight = _panelRect.rect.height;
        if (_expandedHeight <= 0f
            || float.IsNaN(_expandedHeight)
            || float.IsInfinity(_expandedHeight)
            || _minimumHeight >= _expandedHeight)
        {
            Debug.LogError(
                $"Result Panel Minimum Height({_minimumHeight})는 현재 펼친 높이({_expandedHeight})보다 작아야 합니다.",
                this);
            return false;
        }

        if (_panelRect.GetComponent<ContentSizeFitter>() != null)
        {
            Debug.LogWarning(
                "애니메이션 대상 Result Panel에 ContentSizeFitter가 있습니다. 높이가 외부에서 다시 지정되지 않는지 확인하세요.",
                this);
        }

        _contentShownAlpha = _contentCanvasGroup.alpha;
        _contentShownInteractable = _contentCanvasGroup.interactable;
        _contentShownBlocksRaycasts = _contentCanvasGroup.blocksRaycasts;
        _initialized = true;
        return true;
    }

    public bool Play(CanvasGroup panelCanvasGroup)
    {
        if (panelCanvasGroup == null)
        {
            Debug.LogError("ResultPanelUnfoldAnimator에 표시할 Panel CanvasGroup이 없습니다.", this);
            return false;
        }

        if (!_initialized && !Initialize())
            return false;

        KillTween();
        panelCanvasGroup.alpha = 1f;
        panelCanvasGroup.interactable = false;
        panelCanvasGroup.blocksRaycasts = true;
        SetContentVisible(false);
        SetPanelHeight(_minimumHeight);

        float duration = Mathf.Max(0f, _unfoldDuration);
        if (duration <= 0f)
        {
            CompleteUnfold(panelCanvasGroup);
            return true;
        }

        _unfoldTween = DOTween.To(
                () => _panelRect != null ? _panelRect.rect.height : _expandedHeight,
                SetPanelHeight,
                _expandedHeight,
                duration)
            .SetEase(Ease.OutCubic)
            .SetUpdate(true)
            .SetTarget(this)
            .OnComplete(() => CompleteUnfold(panelCanvasGroup));

        return true;
    }

    public bool Hide(CanvasGroup panelCanvasGroup)
    {
        if (panelCanvasGroup == null)
        {
            Debug.LogError("ResultPanelUnfoldAnimator에서 숨길 Panel CanvasGroup이 없습니다.", this);
            return false;
        }

        if (!_initialized && !Initialize())
            return false;

        KillTween();
        SetPanelHeight(_expandedHeight);
        SetContentVisible(true);
        panelCanvasGroup.alpha = 0f;
        panelCanvasGroup.interactable = false;
        panelCanvasGroup.blocksRaycasts = false;
        return true;
    }

    private void OnDisable()
    {
        KillTween();
    }

    private void OnDestroy()
    {
        KillTween();
    }

    private void CompleteUnfold(CanvasGroup panelCanvasGroup)
    {
        _unfoldTween = null;
        SetPanelHeight(_expandedHeight);
        SetContentVisible(true);

        if (panelCanvasGroup == null)
            return;

        panelCanvasGroup.alpha = 1f;
        panelCanvasGroup.interactable = true;
        panelCanvasGroup.blocksRaycasts = true;
    }

    private void SetPanelHeight(float height)
    {
        if (_panelRect == null)
            return;

        _panelRect.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Vertical,
            Mathf.Max(0f, height));
    }

    private void SetContentVisible(bool visible)
    {
        if (_contentCanvasGroup == null)
            return;

        _contentCanvasGroup.alpha = visible ? _contentShownAlpha : 0f;
        _contentCanvasGroup.interactable = visible && _contentShownInteractable;
        _contentCanvasGroup.blocksRaycasts = visible && _contentShownBlocksRaycasts;
    }

    private void KillTween()
    {
        if (_unfoldTween == null)
            return;

        _unfoldTween.Kill(false);
        _unfoldTween = null;
    }
}
