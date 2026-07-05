using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class StudentInfo : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI nameTmp;
    [SerializeField] private StatBar _healthBar;
    private CanvasGroup _canvasGroup;
    private PostStudent _currentStudent = null;



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
        _currentStudent = student;
        nameTmp.text = student.Name;
        _healthBar.SetTarget(student.GetComponent<DamageReceiver>().Health);
        _canvasGroup.alpha = 1f;
    }



    public void Hide()
    {
        _canvasGroup.alpha = 0f;
        nameTmp.text = string.Empty;
        _healthBar.SetTarget(null);
        _currentStudent = null;
    }
}
