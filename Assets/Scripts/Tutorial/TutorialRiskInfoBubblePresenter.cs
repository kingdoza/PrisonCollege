using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DOTweenSequence = DG.Tweening.Sequence;

public sealed class TutorialRiskInfoBubblePresenter : MonoBehaviour
{
    [Header("Explicit references")]
    [SerializeField] private Canvas _canvas;
    [Tooltip("말풍선 배경, 꼬리와 텍스트를 포함하는 루트 RectTransform입니다.")]
    [SerializeField] private RectTransform _bubbleRoot;
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private TextMeshProUGUI _title;
    [SerializeField] private TextMeshProUGUI _description;
    [Tooltip("학생 UpperChest 본을 화면 좌표로 변환할 월드 카메라입니다.")]
    [SerializeField] private Camera _worldCamera;

    [Header("Screen placement")]
    [Min(0f)]
    [Tooltip("UpperChest 본 위치에 더하는 월드 Y축 높이입니다. 본 회전은 적용하지 않습니다.")]
    [SerializeField] private float _worldYOffset = 0.6f;
    [SerializeField] private Vector2 _screenOffset = new(80f, 45f);
    [SerializeField] private Vector2 _screenPadding = new(24f, 24f);

    [Header("Transition")]
    [SerializeField, Min(0f)] private float _showDuration = 0.12f;
    [SerializeField, Min(0f)] private float _hideDuration = 0.08f;
    [SerializeField, Range(0.01f, 1f)] private float _showStartScale = 0.92f;

    private RectTransform _positioningRoot;
    private PostStudent _student;
    private Transform _anchorBone;
    private Vector3 _originalScale;
    private Tween _transitionTween;
    private bool _isInitialized;
    private bool _isStepActive;

    public bool InitializePresenter()
    {
        if (_isInitialized) return true;
        if (_canvas == null
            || _bubbleRoot == null
            || _canvasGroup == null
            || _title == null
            || _description == null
            || _worldCamera == null)
        {
            Debug.LogError("TutorialRiskInfoBubblePresenter 필수 참조가 누락됐습니다.", this);
            return false;
        }
        if (!_bubbleRoot.IsChildOf(_canvas.transform)
            || !(_bubbleRoot.parent is RectTransform parentRect))
        {
            Debug.LogError("위험 행동 말풍선 루트는 지정 Canvas 아래의 UI 오브젝트여야 합니다.", this);
            return false;
        }
        if (_canvas.renderMode != RenderMode.ScreenSpaceOverlay && _canvas.worldCamera == null)
        {
            Debug.LogError("Screen Space - Camera Canvas에는 World Camera 참조가 필요합니다.", _canvas);
            return false;
        }

        _positioningRoot = parentRect;
        _originalScale = _bubbleRoot.localScale;
        _isInitialized = true;
        SetHiddenImmediate();
        return true;
    }

    public void ActivateForStep()
    {
        if (!_isInitialized) return;
        _isStepActive = true;
        SetHiddenImmediate();
    }

    public void Show(
        PostStudent student,
        Transform anchorBone,
        TutorialRiskBehaviorContent content)
    {
        if (!_isInitialized || !_isStepActive || student == null || anchorBone == null) return;

        bool wasVisible = _bubbleRoot.gameObject.activeSelf && _canvasGroup.alpha > 0f;
        bool targetChanged = _student != student;
        _student = student;
        _anchorBone = anchorBone;
        _title.text = content.title;
        _description.text = content.description;
        _bubbleRoot.gameObject.SetActive(true);
        LayoutRebuilder.ForceRebuildLayoutImmediate(_bubbleRoot);

        if (!UpdateScreenPosition())
        {
            SetHiddenImmediate();
            return;
        }
        if (wasVisible && !targetChanged)
            return;

        KillTransition();
        _canvasGroup.alpha = 0f;
        _bubbleRoot.localScale = _originalScale * _showStartScale;
        float duration = Mathf.Max(0f, _showDuration);
        if (duration <= 0f)
        {
            _canvasGroup.alpha = 1f;
            _bubbleRoot.localScale = _originalScale;
            return;
        }

        DOTweenSequence sequence = DOTween.Sequence()
            .SetTarget(this)
            .SetUpdate(true);
        sequence.Join(_canvasGroup.DOFade(1f, duration));
        sequence.Join(_bubbleRoot.DOScale(_originalScale, duration).SetEase(Ease.OutBack));
        sequence.OnComplete(() => _transitionTween = null);
        _transitionTween = sequence;
    }

    public void HideTemporary()
    {
        _student = null;
        _anchorBone = null;
        if (!_isInitialized || !_bubbleRoot.gameObject.activeSelf) return;

        KillTransition();
        float duration = Mathf.Max(0f, _hideDuration);
        if (duration <= 0f)
        {
            SetHiddenImmediate();
            return;
        }

        _transitionTween = _canvasGroup.DOFade(0f, duration)
            .SetTarget(this)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                _transitionTween = null;
                if (_bubbleRoot != null)
                    _bubbleRoot.gameObject.SetActive(false);
            });
    }

    public void DeactivateForStep()
    {
        _isStepActive = false;
        SetHiddenImmediate();
    }

    private void LateUpdate()
    {
        if (!_isStepActive || _student == null || _anchorBone == null) return;
        if (!_student.gameObject.activeInHierarchy || !UpdateScreenPosition())
            HideTemporary();
    }

    private bool UpdateScreenPosition()
    {
        if (_anchorBone == null || _positioningRoot == null) return false;
        Vector3 anchorPosition = _anchorBone.position + Vector3.up * _worldYOffset;
        Vector3 screenPosition = _worldCamera.WorldToScreenPoint(anchorPosition);
        if (screenPosition.z <= 0f) return false;

        Camera uiCamera = _canvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : _canvas.worldCamera;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _positioningRoot,
                screenPosition,
                uiCamera,
                out Vector2 localPoint))
            return false;

        localPoint += _screenOffset;
        Rect parentRect = _positioningRoot.rect;
        Rect bubbleRect = _bubbleRoot.rect;
        Vector2 pivot = _bubbleRoot.pivot;
        float minX = parentRect.xMin + _screenPadding.x + bubbleRect.width * pivot.x;
        float maxX = parentRect.xMax - _screenPadding.x - bubbleRect.width * (1f - pivot.x);
        float minY = parentRect.yMin + _screenPadding.y + bubbleRect.height * pivot.y;
        float maxY = parentRect.yMax - _screenPadding.y - bubbleRect.height * (1f - pivot.y);
        if (minX <= maxX) localPoint.x = Mathf.Clamp(localPoint.x, minX, maxX);
        if (minY <= maxY) localPoint.y = Mathf.Clamp(localPoint.y, minY, maxY);
        _bubbleRoot.anchoredPosition = localPoint;
        return true;
    }

    private void SetHiddenImmediate()
    {
        KillTransition();
        _student = null;
        _anchorBone = null;
        if (_canvasGroup != null) _canvasGroup.alpha = 0f;
        if (_bubbleRoot != null)
        {
            _bubbleRoot.localScale = _originalScale;
            _bubbleRoot.gameObject.SetActive(false);
        }
    }

    private void KillTransition()
    {
        if (_transitionTween != null && _transitionTween.IsActive())
            _transitionTween.Kill();
        _transitionTween = null;
    }

    private void OnDestroy()
    {
        KillTransition();
    }
}
