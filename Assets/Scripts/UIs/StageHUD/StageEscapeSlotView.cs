using UnityEngine;

public class StageEscapeSlotView : MonoBehaviour
{
    [Tooltip("잔여 인원을 나타내는 아이콘 프리팹 인스턴스")]
    [SerializeField] private GameObject _remainingVisual;
    [Tooltip("통제 실패 인원을 나타내는 아이콘 프리팹 인스턴스")]
    [SerializeField] private GameObject _failedVisual;
    [Header("Shake Feedback")]
    [SerializeField] private UIRectShakeFeedback _failureShake = new();

    private bool _isFailed;
    private bool _hasState;

    public bool IsValid => _remainingVisual != null
        && _failedVisual != null
        && _failureShake != null
        && _failureShake.IsValid;

    public void Initialize()
    {
        _failureShake.Initialize();
        _hasState = false;
    }

    public void SetFailed(bool failed, bool animateTransition = false)
    {
        bool shouldShake = animateTransition && _hasState && !_isFailed && failed;

        if (_remainingVisual != null)
            _remainingVisual.SetActive(!failed);
        if (_failedVisual != null)
            _failedVisual.SetActive(failed);

        _isFailed = failed;
        _hasState = true;
        if (shouldShake)
            _failureShake.Play();
    }

    public void Shutdown()
    {
        _failureShake?.Shutdown();
        _hasState = false;
    }
}
