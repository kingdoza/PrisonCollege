using UnityEngine;

public class TutorialMiniWaveStep : TutorialStepBase
{
    private bool _failed;
    public override TutorialStepId StepId => TutorialStepId.MiniWave;

    protected override bool OnEnter()
    {
        _failed = false;
        Context.hud.HideMiniWaveFailure();
        Context.hud.ShowMiniWaveHud(true);
        Context.facade.SetPlayerDeathAllowed(true);
        Context.actors.SetAllBoostBlocked(false);

        if (!Context.checkpoint.HasMiniWaveCheckpoint)
        {
            Debug.LogError("미니웨이브 준비 단계에서 생성한 체크포인트가 없습니다.", this);
            return false;
        }
        if (!Context.actors.StartPreparedMiniWave()) return false;

        if (!Context.facade.ApplyPolicy(MiniWavePolicy())) return false;
        Context.facade.StageFinished += OnStageFinished;
        return true;
    }

    protected override void OnTick() => Context.hud.RenderMiniWave(Context.facade);

    private void OnStageFinished(StageFinishResult result)
    {
        if (result == StageFinishResult.EscapeFailure)
        {
            FailMiniWave();
            return;
        }
        if (result == StageFinishResult.TimerExpired
            && Context.facade.EscapeCount < Context.facade.EscapeFailureThreshold)
        {
            CompleteOnce();
        }
        else
        {
            FailMiniWave();
        }
    }

    private void FailMiniWave()
    {
        if (_failed) return;
        _failed = true;
        Context.facade.StopAllStageSimulation();
        Context.facade.ForceStopProfessorTasks(ProfessorTaskStopReason.StepExit);
        Context.actors.StopAllActors();
        Context.playerInputGate.Acquire(TutorialPlayerInputLockReason.MiniWaveResult);
        Context.hud.ShowMiniWaveFailure();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 0f;
    }

    public void RestartFromCheckpoint()
    {
        if (!_failed || State != TutorialStepState.Active) return;
        if (!Context.checkpoint.RestoreMiniWaveCheckpoint()) return;
        if (!Context.actors.StartPreparedMiniWave()) return;
        _failed = false;
        Context.hud.HideMiniWaveFailure();
        Context.facade.ApplyPolicy(MiniWavePolicy());
        Context.facade.ResumeStageSimulation();
        Context.facade.SetPlayerDeathAllowed(true);
        Time.timeScale = 1f;
        Context.playerInputGate.Release(TutorialPlayerInputLockReason.MiniWaveResult);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    protected override void OnCompleting()
    {
        Context.facade.StopAllStageSimulation();
        Context.facade.ForceStopProfessorTasks(ProfessorTaskStopReason.StepExit);
        Context.actors.StopAllActors();
        Context.playerInputGate.Acquire(TutorialPlayerInputLockReason.MiniWaveResult);
        Time.timeScale = 0f;
    }

    protected override void OnExit()
    {
        Context.facade.StageFinished -= OnStageFinished;
        Context.hud.HideMiniWaveFailure();
        Context.hud.ShowMiniWaveHud(false);
        Context.facade.StopAllStageSimulation();
        Context.actors.StopAllActors();
        Context.actors.ClearMiniWaveComputerSeats();
    }

    private static TutorialStagePolicy MiniWavePolicy()
    {
        return new TutorialStagePolicy
        {
            runTimer = true,
            runProject = true,
            allowContinuousChaosSources = true,
            allowInnocentDownChaos = true,
            allowEscapeChaos = true,
            allowGunshotChaos = true,
            allowNormalFoodRemovedChaos = true,
            allowChaosDecay = true,
            evaluateEscapeFailure = true,
            allowProfessorTask = true,
            showFullStageHud = true,
        };
    }
}
