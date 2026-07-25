using DG.Tweening;
using UnityEngine;
using DOTweenSequence = DG.Tweening.Sequence;

[DisallowMultipleComponent]
public class StagePrepareHUDTransition : MonoBehaviour
{
    [Header("Read-only stage source")]
    [Tooltip("씬에 배치된 실제 StageController를 직접 연결합니다.")]
    [SerializeField] private StageController _stageController;

    [Header("Scale roots")]
    [Tooltip("에디터에서 정상 표시 크기(scale.y = 1)로 저장한 준비 패널 루트")]
    [SerializeField] private RectTransform _preparePanelRoot;
    [Tooltip("에디터에서 정상 표시 크기(scale.y = 1)로 저장한 상단바 전체 루트")]
    [SerializeField] private RectTransform _topBarRoot;

    [Header("Existing CanvasGroups")]
    [Tooltip("StageController의 Prepare Panel Group과 같은 CanvasGroup")]
    [SerializeField] private CanvasGroup _prepareCanvasGroup;
    [Tooltip("StageController의 Top Panel Group과 같은 CanvasGroup")]
    [SerializeField] private CanvasGroup _topBarCanvasGroup;

    [Header("Transition")]
    [SerializeField, Min(0f)] private float _transitionDuration = 0.22f;

    private Vector3 _prepareShownScale;
    private Vector3 _topBarShownScale;
    private DOTweenSequence _transition;
    private bool _referencesValid;
    private bool _transitionPlayed;

    private void Awake()
    {
        _referencesValid = ValidateReferences();
        if (!_referencesValid)
        {
            Debug.LogError(
                "StagePrepareHUDTransition 참조가 누락됐습니다. StageController, 두 RectTransform과 두 CanvasGroup을 모두 연결하세요.",
                this);
            enabled = false;
            return;
        }

        _prepareShownScale = _preparePanelRoot.localScale;
        _topBarShownScale = _topBarRoot.localScale;

        if (Mathf.Approximately(_prepareShownScale.y, 0f)
            || Mathf.Approximately(_topBarShownScale.y, 0f))
        {
            Debug.LogError(
                "Prepare Panel Root와 Top Bar Root는 에디터에서 정상 표시 scale.y로 저장해야 합니다.",
                this);
            enabled = false;
            return;
        }

        ApplyCurrentStageStateImmediate();
    }

    private void OnEnable()
    {
        if (_stageController != null)
            _stageController.StageStartEvent.AddListener(PlayStageStartTransition);
    }

    private void Start()
    {
        if (_referencesValid && !_transitionPlayed)
            ApplyCurrentStageStateImmediate();
    }

    private void OnDisable()
    {
        if (_stageController != null)
            _stageController.StageStartEvent.RemoveListener(PlayStageStartTransition);
        KillTransition();
    }

    private void OnDestroy()
    {
        KillTransition();
    }

    private bool ValidateReferences()
    {
        return _stageController != null
            && _preparePanelRoot != null
            && _topBarRoot != null
            && _prepareCanvasGroup != null
            && _topBarCanvasGroup != null
            && _preparePanelRoot != _topBarRoot;
    }

    private void ApplyCurrentStageStateImmediate()
    {
        if (_stageController.IsPreparing)
            ApplyPreparingImmediate();
        else
            ApplyActiveStageImmediate();
    }

    private void ApplyPreparingImmediate()
    {
        KillTransition();
        SetScaleY(_preparePanelRoot, _prepareShownScale.y);
        SetScaleY(_topBarRoot, 0f);
        _prepareCanvasGroup.alpha = 1f;
        _topBarCanvasGroup.alpha = 1f;
        _prepareCanvasGroup.blocksRaycasts = true;
        _topBarCanvasGroup.blocksRaycasts = false;
    }

    private void ApplyActiveStageImmediate()
    {
        KillTransition();
        SetScaleY(_preparePanelRoot, 0f);
        SetScaleY(_topBarRoot, _topBarShownScale.y);
        _prepareCanvasGroup.alpha = 0f;
        _topBarCanvasGroup.alpha = 1f;
        _prepareCanvasGroup.blocksRaycasts = false;
        _topBarCanvasGroup.blocksRaycasts = true;
    }

    private void PlayStageStartTransition()
    {
        if (!_referencesValid || _transitionPlayed) return;
        _transitionPlayed = true;
        KillTransition();

        // StageController가 이벤트 직전에 Prepare alpha를 0으로 만들기 때문에
        // 접히는 모습이 보이도록 같은 프레임에 다시 표시합니다.
        _prepareCanvasGroup.alpha = 1f;
        _topBarCanvasGroup.alpha = 1f;
        _prepareCanvasGroup.blocksRaycasts = false;
        _topBarCanvasGroup.blocksRaycasts = false;
        SetScaleY(_preparePanelRoot, _prepareShownScale.y);
        SetScaleY(_topBarRoot, 0f);

        if (_transitionDuration <= 0f)
        {
            ApplyActiveStageImmediate();
            return;
        }

        _transition = DOTween.Sequence()
            .SetUpdate(true)
            .Join(_preparePanelRoot
                .DOScaleY(0f, _transitionDuration)
                .SetEase(Ease.InCubic))
            .Join(_topBarRoot
                .DOScaleY(_topBarShownScale.y, _transitionDuration)
                .SetEase(Ease.OutCubic))
            .OnComplete(CompleteStageStartTransition);
    }

    private void CompleteStageStartTransition()
    {
        _transition = null;
        SetScaleY(_preparePanelRoot, 0f);
        SetScaleY(_topBarRoot, _topBarShownScale.y);
        _prepareCanvasGroup.alpha = 0f;
        _topBarCanvasGroup.alpha = 1f;
        _prepareCanvasGroup.blocksRaycasts = false;
        _topBarCanvasGroup.blocksRaycasts = true;
    }

    private void KillTransition()
    {
        if (_transition == null) return;
        _transition.Kill(false);
        _transition = null;
    }

    private static void SetScaleY(RectTransform target, float scaleY)
    {
        Vector3 scale = target.localScale;
        scale.y = scaleY;
        target.localScale = scale;
    }
}
