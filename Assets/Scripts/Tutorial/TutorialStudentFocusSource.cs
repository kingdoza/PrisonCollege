using System;
using UnityEngine;

public sealed class TutorialStudentFocusSource : MonoBehaviour
{
    [Tooltip("기존 StudentDetector가 표시하는 StudentInfo를 직접 연결합니다.")]
    [SerializeField] private StudentInfo _studentInfo;

    private bool _isInitialized;
    private bool _isSubscribed;

    public PostStudent CurrentStudent => _studentInfo != null ? _studentInfo.CurrentStudent : null;
    public event Action<PostStudent, PostStudent> FocusedStudentChanged;

    public bool InitializeSource()
    {
        if (_isInitialized)
        {
            Subscribe();
            return true;
        }
        if (_studentInfo == null)
        {
            Debug.LogError("TutorialStudentFocusSource에 기존 StudentDetector가 사용하는 StudentInfo 참조가 누락됐습니다.", this);
            return false;
        }

        _isInitialized = true;
        Subscribe();
        return true;
    }

    private void Subscribe()
    {
        if (_isSubscribed || _studentInfo == null) return;
        _studentInfo.FocusedStudentChanged += ForwardFocusedStudentChanged;
        _isSubscribed = true;
    }

    private void Unsubscribe()
    {
        if (!_isSubscribed || _studentInfo == null) return;
        _studentInfo.FocusedStudentChanged -= ForwardFocusedStudentChanged;
        _isSubscribed = false;
    }

    private void ForwardFocusedStudentChanged(PostStudent previous, PostStudent current)
    {
        FocusedStudentChanged?.Invoke(previous, current);
    }

    private void OnEnable()
    {
        if (_isInitialized)
            Subscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }
}
