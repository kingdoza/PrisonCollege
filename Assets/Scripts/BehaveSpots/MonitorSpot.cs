using UnityEngine;

public class MonitorSpot : SingleStudentSpot
{
    [SerializeField] private Monitor _monitor;
    [SerializeField] private HackingVfxController _hackingVfx;



    public void TurnOnMonitor(DisplayState displayState)
    {
        if (displayState == DisplayState.Off)
        {
            StopHackingVfx();
            _monitor.ChangeDisplay(DisplayState.Off);
            return;
        }

        _monitor.ChangeDisplay(displayState);

        if (displayState == DisplayState.Hacking)
        {
            PlayHackingVfx();
        }
        else
        {
            StopHackingVfx();
        }
    }



    public void PauseMonitor()
    {
        _monitor.PauseDisplay();
    }


    public void ResumeMonitor()
    {
        _monitor.ResumeDisplay();
    }


    public void PlayHackingVfx()
    {
        _hackingVfx?.Play();
    }


    public void StopHackingVfx()
    {
        _hackingVfx?.Stop();
    }



    public override void Release(PostStudent userStudent)
    {
        base.Release(userStudent);
        StopHackingVfx();
        _monitor.ChangeDisplay(DisplayState.Off);
    }
}
