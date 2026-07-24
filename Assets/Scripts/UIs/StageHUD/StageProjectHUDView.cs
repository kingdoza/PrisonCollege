using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StageProjectHUDView : MonoBehaviour
{
    [SerializeField] private TMP_Text _workingStudentCountText;
    [SerializeField] private Image _projectProgressFill;

    private StageController _source;
    private bool _initialized;

    public bool Initialize(StageController source)
    {
        Shutdown();
        if (source == null || _workingStudentCountText == null || _projectProgressFill == null)
        {
            Debug.LogError("StageProjectHUDView의 StageController, 작업 인원 TMP 또는 Progress Fill 참조가 누락됐습니다.", this);
            return false;
        }

        _source = source;
        _initialized = true;
        Refresh();
        return true;
    }

    public void Shutdown()
    {
        _source = null;
        _initialized = false;
    }

    private void Update()
    {
        if (_initialized)
            Refresh();
    }

    private void Refresh()
    {
        _workingStudentCountText.text = _source.WorkingStudentCount.ToString();
        _projectProgressFill.fillAmount = Mathf.Clamp01(_source.ProjectProgress);
    }
}
