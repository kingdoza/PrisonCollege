using UnityEngine;

public class MonitorSpot : SingleStudentSpot
{
    [SerializeField] private Monitor _monitor;



    public void TurnOnMonitor(DisplayState displayState)
    {
        if (displayState == DisplayState.Off) return;
        _monitor.ChangeDisplay(displayState);
    }



    public void PauseMonitor()
    {
        _monitor.PauseDisplay();
    }


    public void ResumeMonitor()
    {
        _monitor.ResumeDisplay();
    }



    public override void Release(PostStudent userStudent)
    {
        base.Release(userStudent);
        _monitor.ChangeDisplay(DisplayState.Off);
    }
}
