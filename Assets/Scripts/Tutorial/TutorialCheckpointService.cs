using UnityEngine;

public class TutorialCheckpointService : MonoBehaviour
{
    [SerializeField] private TutorialStageFacade _facade;
    [SerializeField] private TutorialActorDirector _actorDirector;
    [SerializeField] private TutorialTransientRegistry _transientRegistry;

    private TutorialCheckpointSnapshot _miniWaveSnapshot;
    public bool HasMiniWaveCheckpoint => _miniWaveSnapshot != null;



    public bool CaptureMiniWaveCheckpoint()
    {
        if (_miniWaveSnapshot != null) return true;
        if (_facade == null || !_facade.IsInitialized
            || _actorDirector == null || !_actorDirector.IsInitialized)
        {
            Debug.LogError("8단계 체크포인트 캡처 참조가 초기화되지 않았습니다.", this);
            return false;
        }
        if (!_actorDirector.HasPreparedMiniWave)
        {
            Debug.LogError("미니웨이브 로스터가 준비되기 전에 체크포인트를 캡처할 수 없습니다.", this);
            return false;
        }

        TutorialFireSuppressionState fireSuppression = _facade.CaptureFireSuppressionState();
        if (fireSuppression.isFlooding)
        {
            Debug.LogError("8단계 최초 진입 시 소방 침수 동작이 이미 진행 중입니다.", this);
            return false;
        }

        _miniWaveSnapshot = new TutorialCheckpointSnapshot
        {
            player = _facade.Player.CaptureTutorialState(),
            weapons = _facade.WeaponController.CaptureTutorialSnapshot(),
            chaos = _facade.Chaos,
            projectProgress = _facade.ProjectProgress,
            escapeCount = _facade.EscapeCount,
            escapeThreshold = _facade.EscapeFailureThreshold,
            timer = _facade.TimerRemaining,
            sessionMoney = _facade.SessionMoney,
            gates = _facade.CaptureGateStates(),
            microwaves = _facade.CaptureMicrowaveStates(),
            fires = _facade.CaptureFireStates(),
            fireSuppression = fireSuppression,
            rechargers = _facade.CaptureRechargerStates(),
            lightsOn = _facade.AreLightsOn,
            actors = _actorDirector.CaptureSnapshot(),
        };
        return true;
    }



    public bool RestoreMiniWaveCheckpoint()
    {
        if (_miniWaveSnapshot == null)
        {
            Debug.LogError("미니웨이브 준비 완료 체크포인트가 없습니다.", this);
            return false;
        }

        // Ready 이벤트나 재스폰 없이 같은 학생 인스턴스를 동기적으로 재구성한다.
        _facade.StopAllStageSimulation();
        _transientRegistry?.ClearAll();
        _facade.ForceStopProfessorTasks(ProfessorTaskStopReason.StepExit);
        _facade.RestoreFacilityStates(
            _miniWaveSnapshot.microwaves,
            _miniWaveSnapshot.fires,
            _miniWaveSnapshot.fireSuppression,
            _miniWaveSnapshot.rechargers,
            _miniWaveSnapshot.lightsOn);
        _facade.Player.RestoreTutorialState(_miniWaveSnapshot.player);
        _facade.WeaponController.RestoreTutorialSnapshot(_miniWaveSnapshot.weapons);
        _facade.SetChaos(_miniWaveSnapshot.chaos);
        _facade.SetProjectProgress(_miniWaveSnapshot.projectProgress);
        _facade.SetEscapeCount(_miniWaveSnapshot.escapeCount, _miniWaveSnapshot.escapeThreshold);
        _facade.SetTimer(_miniWaveSnapshot.timer);
        _facade.SetSessionMoney(_miniWaveSnapshot.sessionMoney);
        _facade.RestoreGateStates(_miniWaveSnapshot.gates);
        return _actorDirector.RestoreSnapshot(_miniWaveSnapshot.actors);
    }
}



public sealed class TutorialCheckpointSnapshot
{
    public TutorialPlayerState player;
    public TutorialWeaponSnapshot weapons;
    public float chaos;
    public float projectProgress;
    public int escapeCount;
    public int escapeThreshold;
    public float timer;
    public int sessionMoney;
    public TutorialGateState[] gates;
    public TutorialMicrowaveState[] microwaves;
    public TutorialFireState[] fires;
    public TutorialFireSuppressionState fireSuppression;
    public TutorialRechargerState[] rechargers;
    public bool lightsOn;
    public TutorialActorPoolSnapshot actors;
}
