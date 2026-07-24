using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public sealed class MainMenuButtonEntrancePresenter : MonoBehaviour
{
    [Serializable]
    private sealed class ButtonEntranceTarget
    {
        [Tooltip("레이아웃 슬롯 자체가 아니라, 위치와 크기를 움직일 버튼 내부 VisualRoot입니다.")]
        [SerializeField] private RectTransform _visualRoot;
        [Tooltip("VisualRoot에 부착해 이 버튼의 등장 투명도만 제어할 CanvasGroup입니다.")]
        [SerializeField] private CanvasGroup _visualCanvasGroup;

        public RectTransform VisualRoot => _visualRoot;
        public CanvasGroup VisualCanvasGroup => _visualCanvasGroup;
    }

    private readonly struct TargetState
    {
        public TargetState(
            RectTransform visualRoot,
            CanvasGroup canvasGroup,
            Vector2 anchoredPosition,
            Vector3 localScale,
            float alpha)
        {
            VisualRoot = visualRoot;
            CanvasGroup = canvasGroup;
            AnchoredPosition = anchoredPosition;
            LocalScale = localScale;
            Alpha = alpha;
        }

        public RectTransform VisualRoot { get; }
        public CanvasGroup CanvasGroup { get; }
        public Vector2 AnchoredPosition { get; }
        public Vector3 LocalScale { get; }
        public float Alpha { get; }
    }

    [Header("Input Lock")]
    [Tooltip("메인 메뉴 버튼 목록 전체에 부착한 CanvasGroup입니다. 알파는 건드리지 않고 입력만 잠급니다.")]
    [SerializeField] private CanvasGroup _buttonListCanvasGroup;

    [Header("Top-To-Bottom Order")]
    [Tooltip("위쪽 버튼부터 아래쪽 버튼 순서로 VisualRoot와 개별 CanvasGroup을 연결합니다.")]
    [SerializeField] private ButtonEntranceTarget[] _targets = Array.Empty<ButtonEntranceTarget>();

    [Header("Entrance")]
    [Tooltip("메인 메뉴가 열린 뒤 첫 버튼 등장을 시작하기 전까지 기다리는 시간입니다.")]
    [Min(0f)] [SerializeField] private float _startDelay = 0.4f;
    [Tooltip("시작 위치를 원래 위치에서 왼쪽으로 이동시킬 양입니다.")]
    [Min(0f)] [SerializeField] private float _leftOffset = 130f;
    [Tooltip("등장 시작 시 원래 크기에 곱할 비율입니다.")]
    [Range(0.01f, 1f)] [SerializeField] private float _initialScale = 0.9f;
    [Tooltip("버튼 하나가 등장하는 시간입니다.")]
    [Min(0f)] [SerializeField] private float _duration = 0.3f;
    [Tooltip("각 버튼의 등장 시작 간격입니다.")]
    [Min(0f)] [SerializeField] private float _staggerInterval = 0.09f;
    [SerializeField] private Ease _positionEase = Ease.OutCubic;
    [SerializeField] private Ease _scaleEase = Ease.OutBack;
    [SerializeField] private Ease _fadeEase = Ease.OutQuad;

    private DG.Tweening.Sequence _entranceSequence;
    private TargetState[] _states = Array.Empty<TargetState>();
    private bool _hasCapturedStates;



    private void OnDisable()
    {
        StopEntrance(true);
    }



#if UNITY_EDITOR
    private void OnValidate()
    {
        _startDelay = Mathf.Max(0f, _startDelay);
        _leftOffset = Mathf.Max(0f, _leftOffset);
        _initialScale = Mathf.Clamp(_initialScale, 0.01f, 1f);
        _duration = Mathf.Max(0f, _duration);
        _staggerInterval = Mathf.Max(0f, _staggerInterval);
    }
#endif



    public bool Play(Action onCompleted)
    {
        StopEntrance(true);

        if (!ValidateReferences(true))
        {
            SetInputEnabled(true);
            return false;
        }

        Canvas.ForceUpdateCanvases();
        CaptureStates();
        ApplyInitialStates();

        EventSystem.current?.SetSelectedGameObject(null);
        SetInputEnabled(false);

        _entranceSequence = DOTween.Sequence()
            .SetUpdate(true)
            .SetTarget(this);

        for (int i = 0; i < _states.Length; i++)
        {
            TargetState state = _states[i];
            float startTime = _startDelay + i * _staggerInterval;

            Tween positionTween = state.VisualRoot
                .DOAnchorPos(state.AnchoredPosition, _duration)
                .SetEase(_positionEase);
            Tween scaleTween = state.VisualRoot
                .DOScale(state.LocalScale, _duration)
                .SetEase(_scaleEase);
            Tween fadeTween = state.CanvasGroup
                .DOFade(state.Alpha, _duration)
                .SetEase(_fadeEase);

            _entranceSequence.Insert(startTime, positionTween);
            _entranceSequence.Insert(startTime, scaleTween);
            _entranceSequence.Insert(startTime, fadeTween);
        }

        _entranceSequence.OnComplete(() =>
        {
            _entranceSequence = null;
            RestoreStates();
            SetInputEnabled(true);
            onCompleted?.Invoke();
        });

        return true;
    }



    public void StopEntrance(bool restoreVisuals)
    {
        if (_entranceSequence != null && _entranceSequence.IsActive())
            _entranceSequence.Kill();

        _entranceSequence = null;

        if (restoreVisuals)
            RestoreStates();

        SetInputEnabled(true);
    }



    private void CaptureStates()
    {
        if (_hasCapturedStates) return;

        _states = new TargetState[_targets.Length];
        for (int i = 0; i < _targets.Length; i++)
        {
            RectTransform visualRoot = _targets[i].VisualRoot;
            CanvasGroup canvasGroup = _targets[i].VisualCanvasGroup;
            _states[i] = new TargetState(
                visualRoot,
                canvasGroup,
                visualRoot.anchoredPosition,
                visualRoot.localScale,
                canvasGroup.alpha);
        }

        _hasCapturedStates = true;
    }



    private void ApplyInitialStates()
    {
        for (int i = 0; i < _states.Length; i++)
        {
            TargetState state = _states[i];
            state.VisualRoot.anchoredPosition =
                state.AnchoredPosition + Vector2.left * _leftOffset;
            state.VisualRoot.localScale = state.LocalScale * _initialScale;
            state.CanvasGroup.alpha = 0f;
        }
    }



    private void RestoreStates()
    {
        if (!_hasCapturedStates) return;

        for (int i = 0; i < _states.Length; i++)
        {
            TargetState state = _states[i];
            if (state.VisualRoot != null)
            {
                state.VisualRoot.anchoredPosition = state.AnchoredPosition;
                state.VisualRoot.localScale = state.LocalScale;
            }

            if (state.CanvasGroup != null)
                state.CanvasGroup.alpha = state.Alpha;
        }
    }



    private void SetInputEnabled(bool enabled)
    {
        if (_buttonListCanvasGroup == null) return;

        _buttonListCanvasGroup.interactable = enabled;
        _buttonListCanvasGroup.blocksRaycasts = enabled;
    }



    private bool ValidateReferences(bool logErrors)
    {
        if (_buttonListCanvasGroup == null)
        {
            if (logErrors)
                Debug.LogError(
                    "MainMenuButtonEntrancePresenter의 Button List Canvas Group 참조가 누락됐습니다.",
                    this);
            return false;
        }

        if (_targets == null || _targets.Length == 0)
        {
            if (logErrors)
                Debug.LogError(
                    "MainMenuButtonEntrancePresenter의 등장 버튼 목록이 비어 있습니다.",
                    this);
            return false;
        }

        for (int i = 0; i < _targets.Length; i++)
        {
            ButtonEntranceTarget target = _targets[i];
            if (target == null ||
                target.VisualRoot == null ||
                target.VisualCanvasGroup == null)
            {
                if (logErrors)
                    Debug.LogError(
                        $"MainMenuButtonEntrancePresenter의 Targets [{i}]에 Visual Root와 Visual Canvas Group을 모두 연결해야 합니다.",
                        this);
                return false;
            }
        }

        return true;
    }
}
