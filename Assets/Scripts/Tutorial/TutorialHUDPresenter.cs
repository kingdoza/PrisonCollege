using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using DOTweenSequence = DG.Tweening.Sequence;

public class TutorialHUDPresenter : MonoBehaviour
{
    [Header("Step UI")]
    [SerializeField] private GameObject _stepPanel;
    [Tooltip("Step Panel 배경을 제외하고 접기 시작 시 숨기고 펼치기 완료 후 표시할 모든 UI의 공통 부모입니다.")]
    [SerializeField] private GameObject _stepPanelContentRoot;
    [SerializeField] private TextMeshProUGUI _title;
    [SerializeField] private TextMeshProUGUI _subtitle;
    [SerializeField] private TextMeshProUGUI _guide;
    [SerializeField] private TextMeshProUGUI _objective;
    [SerializeField] private TextMeshProUGUI _inputHint;
    [SerializeField] private TextMeshProUGUI _progress;
    [SerializeField] private GameObject _chaosDecayHighlight;

    [Header("Step panel transition")]
    [SerializeField, Min(0f)] private float _stepPanelMinimumHeight = 80f;
    [SerializeField, Min(0f)] private float _stepPanelFoldDuration = 0.2f;
    [SerializeField, Min(0f)] private float _stepPanelUnfoldDuration = 0.25f;

    [Header("Objective feedback")]
    [SerializeField] private Color _progressFeedbackColor = new(1f, 0.82f, 0.25f, 1f);
    [SerializeField] private Color _completionFeedbackColor = new(0.36f, 0.9f, 0.45f, 1f);
    [SerializeField, Min(0f)] private float _progressFeedbackDuration = 0.25f;
    [SerializeField, Min(0f)] private float _progressHoldDuration = 0.15f;
    [SerializeField, Min(0f)] private float _progressFeedbackMinimumInterval = 0.2f;
    [SerializeField, Min(0f)] private float _progressPunchScale = 0.05f;
    [SerializeField, Min(0f)] private float _completionFeedbackDuration = 0.6f;
    [SerializeField, Min(0f)] private float _completionPunchScale = 0.1f;

    [Header("Step 8 result")]
    [SerializeField] private GameObject _miniWaveFailurePanel;
    [SerializeField] private Button _restartMiniWaveButton;
    [SerializeField] private Button _skipMiniWaveButton;

    [Header("Course summary")]
    [SerializeField] private GameObject _courseSummaryButtons;
    [SerializeField] private Button _reenrollButton;
    [SerializeField] private Button _mainMenuButton;

    private UnityAction _restartMiniWaveAction;
    private UnityAction _skipMiniWaveAction;
    private UnityAction _reenrollAction;
    private UnityAction _mainMenuAction;
    private string _objectiveText = string.Empty;
    private string _progressText = string.Empty;
    private Color _objectiveOriginalColor;
    private Vector3 _objectiveOriginalScale;
    private Tween _objectiveFeedbackTween;
    private float _lastProgressFeedbackTime = float.NegativeInfinity;
    private bool _objectiveOriginalStateCaptured;
    private bool _hasProgressValue;
    private RectTransform _stepPanelRect;
    private RectTransform _stepPanelContentRect;
    private Tween _stepPanelTween;
    private float _stepPanelExpandedHeight;
    private bool _stepPanelTransitionInitialized;

    public int StateRevision { get; private set; }



    private void Awake()
    {
        CaptureObjectiveOriginalState();
    }



    public bool InitializeButtons(
        Action restartMiniWave,
        Action skipMiniWave,
        Action reenroll,
        Action mainMenu)
    {
        if (_restartMiniWaveButton == null
            || _skipMiniWaveButton == null
            || _reenrollButton == null
            || _mainMenuButton == null)
        {
            Debug.LogError("TutorialHUDPresenter 결과/요약 버튼 참조가 누락됐습니다.", this);
            return false;
        }
        if (!InitializeStepPanelTransition()) return false;

        _restartMiniWaveAction = () => restartMiniWave?.Invoke();
        _skipMiniWaveAction = () => skipMiniWave?.Invoke();
        _reenrollAction = () => reenroll?.Invoke();
        _mainMenuAction = () => mainMenu?.Invoke();
        _restartMiniWaveButton.onClick.AddListener(_restartMiniWaveAction);
        _skipMiniWaveButton.onClick.AddListener(_skipMiniWaveAction);
        _reenrollButton.onClick.AddListener(_reenrollAction);
        _mainMenuButton.onClick.AddListener(_mainMenuAction);
        HideMiniWaveFailure();
        ShowCourseSummaryButtons(false);
        return true;
    }



    public void ShowStep(TutorialStepContent content)
    {
        StopObjectiveFeedback();
        if (_stepPanel != null) _stepPanel.SetActive(true);
        if (_title != null) _title.text = content.title;
        if (_subtitle != null) _subtitle.text = content.subtitle;
        if (_guide != null) _guide.text = content.guide;
        _objectiveText = content.objective ?? string.Empty;
        _progressText = string.Empty;
        _hasProgressValue = false;
        _lastProgressFeedbackTime = float.NegativeInfinity;
        RenderObjective();
        if (_inputHint != null) _inputHint.text = content.inputHint;
        if (_progress != null) _progress.text = string.Empty;
        RefreshStepPanelExpandedHeight();
        StateRevision++;
    }



    public void HideStep()
    {
        StopObjectiveFeedback();
        if (_stepPanel != null) _stepPanel.SetActive(false);
        StateRevision++;
    }



    public void PlayStepPanelFold(Action completed)
    {
        StopObjectiveFeedback();
        SetStepInfoContentVisible(false);
        PlayStepPanelHeightTween(
            _stepPanelMinimumHeight,
            _stepPanelFoldDuration,
            Ease.InCubic,
            false,
            completed);
    }



    public void PlayStepPanelUnfold(Action completed)
    {
        SetStepInfoContentVisible(false);
        PlayStepPanelHeightTween(
            _stepPanelExpandedHeight,
            _stepPanelUnfoldDuration,
            Ease.OutCubic,
            true,
            completed);
    }



    public void SetNumericProgress(int current, int target)
    {
        string nextProgressText = $"({current}/{target})";
        bool changed = _hasProgressValue && !string.Equals(_progressText, nextProgressText, StringComparison.Ordinal);
        _progressText = nextProgressText;
        _hasProgressValue = true;
        RenderObjective();
        RefreshStepPanelExpandedHeight();
        if (changed) PlayProgressFeedback();
        if (_progress != null) _progress.text = string.Empty;
        StateRevision++;
    }



    public void SetTimedProgress(float current, float target)
    {
        string nextProgressText = $"({current:F1}/{target:F1})";
        bool changed = _hasProgressValue && !string.Equals(_progressText, nextProgressText, StringComparison.Ordinal);
        _progressText = nextProgressText;
        _hasProgressValue = true;
        RenderObjective();
        RefreshStepPanelExpandedHeight();
        if (changed) PlayProgressFeedback();
        if (_progress != null) _progress.text = string.Empty;
        StateRevision++;
    }



    private void RenderObjective()
    {
        if (_objective == null) return;
        if (string.IsNullOrWhiteSpace(_objectiveText))
        {
            _objective.text = _progressText;
            return;
        }

        string formattedObjective = $"\u00B7 {_objectiveText}";
        _objective.text = string.IsNullOrWhiteSpace(_progressText)
            ? formattedObjective
            : $"{formattedObjective}  {_progressText}";
    }



    public void PlayObjectiveCompletionFeedback(Action completed)
    {
        StopObjectiveFeedback();
        StateRevision++;

        if (_objective == null || !_objective.gameObject.activeInHierarchy)
        {
            completed?.Invoke();
            return;
        }

        CaptureObjectiveOriginalState();
        Color completionColor = _completionFeedbackColor;
        completionColor.a = _objectiveOriginalColor.a;
        _objective.color = completionColor;
        _objective.rectTransform.localScale = _objectiveOriginalScale;

        float duration = Mathf.Max(0f, _completionFeedbackDuration);
        if (duration <= 0f)
        {
            completed?.Invoke();
            return;
        }

        float punchDuration = Mathf.Min(0.25f, duration);
        DOTweenSequence sequence = DOTween.Sequence()
            .SetTarget(this)
            .SetUpdate(true);
        sequence.Append(_objective.rectTransform.DOPunchScale(
            _objectiveOriginalScale * _completionPunchScale,
            punchDuration,
            1,
            0f));
        if (duration > punchDuration)
            sequence.AppendInterval(duration - punchDuration);
        sequence.OnComplete(() =>
        {
            _objectiveFeedbackTween = null;
            completed?.Invoke();
        });
        _objectiveFeedbackTween = sequence;
    }



    private void PlayProgressFeedback()
    {
        if (_objective == null || !_objective.enabled || !_objective.gameObject.activeInHierarchy) return;
        if (Time.unscaledTime - _lastProgressFeedbackTime < _progressFeedbackMinimumInterval) return;

        _lastProgressFeedbackTime = Time.unscaledTime;
        StopObjectiveFeedback();
        CaptureObjectiveOriginalState();

        Color progressColor = _progressFeedbackColor;
        progressColor.a = _objectiveOriginalColor.a;
        _objective.color = progressColor;
        _objective.rectTransform.localScale = _objectiveOriginalScale;

        float duration = Mathf.Max(0f, _progressFeedbackDuration);
        if (duration <= 0f)
        {
            RestoreObjectiveOriginalState();
            return;
        }

        float holdDuration = Mathf.Clamp(_progressHoldDuration, 0f, duration);
        float fadeDuration = duration - holdDuration;

        DOTweenSequence sequence = DOTween.Sequence()
            .SetTarget(this)
            .SetUpdate(true);
        sequence.AppendInterval(holdDuration);
        if (fadeDuration > 0f)
            sequence.Append(_objective.DOColor(_objectiveOriginalColor, fadeDuration));
        sequence.Insert(0f, _objective.rectTransform.DOPunchScale(
            _objectiveOriginalScale * _progressPunchScale,
            duration,
            1,
            0f));
        sequence.OnComplete(() =>
        {
            _objectiveFeedbackTween = null;
            RestoreObjectiveOriginalState();
        });
        _objectiveFeedbackTween = sequence;
    }



    private void CaptureObjectiveOriginalState()
    {
        if (_objectiveOriginalStateCaptured || _objective == null) return;
        _objectiveOriginalColor = _objective.color;
        _objectiveOriginalScale = _objective.rectTransform.localScale;
        _objectiveOriginalStateCaptured = true;
    }



    private void StopObjectiveFeedback()
    {
        if (_objectiveFeedbackTween != null && _objectiveFeedbackTween.IsActive())
            _objectiveFeedbackTween.Kill();
        _objectiveFeedbackTween = null;
        RestoreObjectiveOriginalState();
    }



    private void RestoreObjectiveOriginalState()
    {
        if (!_objectiveOriginalStateCaptured || _objective == null) return;
        _objective.color = _objectiveOriginalColor;
        _objective.rectTransform.localScale = _objectiveOriginalScale;
    }



    private bool InitializeStepPanelTransition()
    {
        if (_stepPanelTransitionInitialized) return true;
        if (_stepPanel == null)
        {
            Debug.LogError("TutorialHUDPresenter Step Panel 참조가 누락됐습니다.", this);
            return false;
        }

        _stepPanelRect = _stepPanel.GetComponent<RectTransform>();
        if (_stepPanelRect == null)
        {
            Debug.LogError("TutorialHUDPresenter Step Panel에는 RectTransform이 필요합니다.", _stepPanel);
            return false;
        }
        if (_stepPanelContentRoot == null
            || _stepPanelContentRoot == _stepPanel
            || !_stepPanelContentRoot.transform.IsChildOf(_stepPanel.transform))
        {
            Debug.LogError(
                "TutorialHUDPresenter Step Panel Content Root는 Step Panel 하위의 별도 오브젝트여야 합니다.",
                this);
            return false;
        }

        _stepPanelContentRect = _stepPanelContentRoot.GetComponent<RectTransform>();
        if (_stepPanelContentRect == null)
        {
            Debug.LogError("TutorialHUDPresenter Step Panel Content Root에는 RectTransform이 필요합니다.", _stepPanelContentRoot);
            return false;
        }
        if (_stepPanelContentRoot.GetComponent<LayoutGroup>() == null)
        {
            Debug.LogWarning(
                "Step Panel Content Root에 LayoutGroup이 없습니다. 단계 문구에 따른 동적 높이를 사용하려면 VerticalLayoutGroup을 추가하세요.",
                _stepPanelContentRoot);
        }

        _stepPanel.SetActive(true);
        Canvas.ForceUpdateCanvases();
        _stepPanelExpandedHeight = _stepPanelRect.rect.height;
        if (_stepPanelExpandedHeight <= 0f
            || _stepPanelMinimumHeight >= _stepPanelExpandedHeight)
        {
            Debug.LogError(
                $"Step Panel Minimum Height({_stepPanelMinimumHeight})는 현재 펼친 높이({_stepPanelExpandedHeight})보다 작아야 합니다.",
                this);
            return false;
        }

        _stepPanelTransitionInitialized = true;
        SetStepInfoContentVisible(false);
        SetStepPanelHeight(_stepPanelMinimumHeight);
        return true;
    }



    private void RefreshStepPanelExpandedHeight()
    {
        if (!_stepPanelTransitionInitialized
            || _stepPanel == null
            || _stepPanelRect == null
            || _stepPanelContentRoot == null
            || _stepPanelContentRect == null)
            return;

        bool wasPanelActive = _stepPanel.activeSelf;
        bool wasContentActive = _stepPanelContentRoot.activeSelf;

        if (!wasPanelActive) _stepPanel.SetActive(true);
        if (!wasContentActive) _stepPanelContentRoot.SetActive(true);

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(_stepPanelContentRect);
        Canvas.ForceUpdateCanvases();

        float preferredHeight = LayoutUtility.GetPreferredHeight(_stepPanelContentRect);
        if (preferredHeight <= 0f || float.IsNaN(preferredHeight) || float.IsInfinity(preferredHeight))
            preferredHeight = _stepPanelContentRect.rect.height;

        if (preferredHeight > 0f && !float.IsNaN(preferredHeight) && !float.IsInfinity(preferredHeight))
        {
            _stepPanelExpandedHeight = Mathf.Max(_stepPanelMinimumHeight, preferredHeight);

            bool isHeightTweenActive = _stepPanelTween != null && _stepPanelTween.IsActive();
            if (wasContentActive && !isHeightTweenActive)
                SetStepPanelHeight(_stepPanelExpandedHeight);
        }

        if (!wasContentActive) _stepPanelContentRoot.SetActive(false);
        if (!wasPanelActive) _stepPanel.SetActive(false);
    }



    private void PlayStepPanelHeightTween(
        float targetHeight,
        float duration,
        Ease ease,
        bool showContentOnComplete,
        Action completed)
    {
        if (!_stepPanelTransitionInitialized && !InitializeStepPanelTransition())
        {
            completed?.Invoke();
            return;
        }

        if (_stepPanelTween != null && _stepPanelTween.IsActive())
            _stepPanelTween.Kill();
        _stepPanelTween = null;

        if (_stepPanel != null && !_stepPanel.activeSelf)
            _stepPanel.SetActive(true);

        float clampedDuration = Mathf.Max(0f, duration);
        if (clampedDuration <= 0f)
        {
            SetStepPanelHeight(targetHeight);
            if (showContentOnComplete) SetStepInfoContentVisible(true);
            completed?.Invoke();
            return;
        }

        _stepPanelTween = DOTween.To(
                () => _stepPanelRect != null ? _stepPanelRect.rect.height : targetHeight,
                SetStepPanelHeight,
                targetHeight,
                clampedDuration)
            .SetEase(ease)
            .SetTarget(this)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                _stepPanelTween = null;
                SetStepPanelHeight(targetHeight);
                if (showContentOnComplete) SetStepInfoContentVisible(true);
                completed?.Invoke();
            });
    }



    private void SetStepPanelHeight(float height)
    {
        if (_stepPanelRect == null) return;
        _stepPanelRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Max(0f, height));
    }



    private void SetStepInfoContentVisible(bool isVisible)
    {
        if (_stepPanelContentRoot != null)
            _stepPanelContentRoot.SetActive(isVisible);
    }







    public void NotifyChaosRendered()
    {
        StateRevision++;
    }



    public void ShowChaosDecayHighlight(bool show)
    {
        if (_chaosDecayHighlight != null) _chaosDecayHighlight.SetActive(show);
        StateRevision++;
    }



    public void ShowMiniWaveFailure()
    {
        if (_miniWaveFailurePanel != null) _miniWaveFailurePanel.SetActive(true);
        StateRevision++;
    }



    public void HideMiniWaveFailure()
    {
        if (_miniWaveFailurePanel != null) _miniWaveFailurePanel.SetActive(false);
        StateRevision++;
    }



    public void ShowCourseSummaryButtons(bool show)
    {
        if (_courseSummaryButtons != null) _courseSummaryButtons.SetActive(show);
    }



    private void OnDestroy()
    {
        if (_stepPanelTween != null && _stepPanelTween.IsActive())
            _stepPanelTween.Kill();
        _stepPanelTween = null;
        StopObjectiveFeedback();
        if (_restartMiniWaveButton != null && _restartMiniWaveAction != null)
            _restartMiniWaveButton.onClick.RemoveListener(_restartMiniWaveAction);
        if (_skipMiniWaveButton != null && _skipMiniWaveAction != null)
            _skipMiniWaveButton.onClick.RemoveListener(_skipMiniWaveAction);
        if (_reenrollButton != null && _reenrollAction != null)
            _reenrollButton.onClick.RemoveListener(_reenrollAction);
        if (_mainMenuButton != null && _mainMenuAction != null)
            _mainMenuButton.onClick.RemoveListener(_mainMenuAction);
    }
}
