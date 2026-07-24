using UnityEngine;

public class StageEscapeSlotView : MonoBehaviour
{
    [Tooltip("잔여 인원을 나타내는 아이콘 프리팹 인스턴스")]
    [SerializeField] private GameObject _remainingVisual;
    [Tooltip("통제 실패 인원을 나타내는 아이콘 프리팹 인스턴스")]
    [SerializeField] private GameObject _failedVisual;

    public bool IsValid => _remainingVisual != null && _failedVisual != null;

    public void SetFailed(bool failed)
    {
        if (_remainingVisual != null)
            _remainingVisual.SetActive(!failed);
        if (_failedVisual != null)
            _failedVisual.SetActive(failed);
    }
}
