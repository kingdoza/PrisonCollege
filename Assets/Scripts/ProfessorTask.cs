using UnityEngine;

public class ProfessorTask : MonoBehaviour
{
    [SerializeField] private Monitor _monitor;
    [SerializeField] private Transform _cameraSocket;
    [SerializeField] private GameObject _taskInfoPanel;
    [SerializeField] private GameObject _crosshairPanel;
    private Click _interaction;
    private Professor _professor;
    private bool _isTasking = false;
    public bool IsTasking => _isTasking;



    private void Awake()
    {
        _taskInfoPanel.SetActive(false);
        _professor = StageController.Instance.Player;
        _interaction = GetComponent<Click>();
        _interaction.ActionName = "프로젝트 진행";
        _interaction.ClickEvent.AddListener(OnTaskStateChanged);
        _interaction.FillAmount = 0;
        _professor.DieEvent?.AddListener(_ => OnProfessorDied());
    }



    private void Update()
    {
        ElapseTaskTime();
        CheckMovementInputToStopTask();
        ApplyProjectProgressFill();
    }



    private void ApplyProjectProgressFill()
    {
        if (IsTasking)
        {
            _interaction.FillAmount = StageController.Instance.ProjectProgress;
        }
    }



    private void ElapseTaskTime()
    {
    }



    private void CheckMovementInputToStopTask()
    {
        if (!_isTasking) return;
        //float h = Input.GetAxis("Horizontal");
        //float v = Input.GetAxis("Vertical");
        //float hRaw = Input.GetAxisRaw("Horizontal");
        //float vRaw = Input.GetAxisRaw("Vertical");

        if (Input.GetButtonDown("Horizontal") || Input.GetButtonDown("Vertical"))
        {
            StopTask();
        }
    }



    private void OnTaskStateChanged()
    {
        if (_isTasking)
        {
            StopTask();
        }
        else
        {
            DoTask();
        }
    }



    private void OnProfessorDied()
    {
        if (!IsTasking) return;
        _isTasking = false;
        _interaction.FillAmount = 0;
        _taskInfoPanel.SetActive(false);
        _crosshairPanel.SetActive(true);
        _professor.gameObject.transform.parent = null;
        _monitor.ChangeDisplay(DisplayState.Off);
    }



    private void DoTask()
    {
        _isTasking = true;
        _taskInfoPanel.SetActive(true);
        _crosshairPanel.SetActive(false);
        AttachProp(_professor.gameObject, _cameraSocket);
        _professor.SetTaskPose();
        _monitor.ChangeDisplay(DisplayState.Working);
    }



    private void StopTask()
    {
        _interaction.FillAmount = 0;
        _taskInfoPanel.SetActive(false);
        _crosshairPanel.SetActive(true);
        _isTasking = false;
        _professor.gameObject.transform.parent = null;
        _professor.UnsetTaskPose();
        _monitor.ChangeDisplay(DisplayState.Off);
    }



    protected virtual void AttachProp(GameObject prop, Transform targetSocket)
    {
        prop.transform.SetParent(targetSocket);
        prop.transform.localPosition = Vector3.zero;
        prop.transform.localRotation = Quaternion.identity;
    }
}
