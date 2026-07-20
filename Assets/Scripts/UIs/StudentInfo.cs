using System;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class StudentInfo : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI nameTmp;
    [SerializeField] private StatBar _healthBar;
    private CanvasGroup _canvasGroup;
    private PostStudent _currentStudent = null;

    public PostStudent CurrentStudent => _currentStudent;
    public event Action<PostStudent, PostStudent> FocusedStudentChanged;



    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        Hide();
    }



    private void Update()
    {
        if (_currentStudent == null) return;
        //float healthRatio = _currentStudentHealth.Ratio;
        //healthBar.OnStatChanged();
    }



    public void Show(PostStudent student)
    {
        PostStudent previous = _currentStudent;
        _currentStudent = student;
        nameTmp.text = student.Name;
        _healthBar.SetTarget(student.GetComponent<DamageReceiver>().Health);
        _canvasGroup.alpha = 1f;
        if (previous != _currentStudent)
            FocusedStudentChanged?.Invoke(previous, _currentStudent);
    }



    public void Hide()
    {
        PostStudent previous = _currentStudent;
        _canvasGroup.alpha = 0f;
        nameTmp.text = string.Empty;
        _healthBar.SetTarget(null);
        _currentStudent = null;
        if (previous != null)
            FocusedStudentChanged?.Invoke(previous, null);
    }
}
