using UnityEngine;

[DisallowMultipleComponent]
public class RushChargeVfxController : MonoBehaviour
{
    private const float MinimumDuration = 0.0001f;
    private const float MinimumParticleLifetime = 0.0001f;

    [Header("Particle References")]
    [SerializeField, Tooltip("Looping point particles shown throughout the rush charge.")]
    private ParticleSystem _pointParticles;
    [SerializeField, Tooltip("Single-burst particle whose Size over Lifetime curve represents charge progress.")]
    private ParticleSystem _beam;

    private readonly ParticleSystem.Particle[] _beamParticle = new ParticleSystem.Particle[1];

    private bool _settingsCached;
    private bool _pointParticlesOriginalLoop;
    private float _beamOriginalSimulationSpeed = 1f;
    private float _beamChargeSimulationSpeed;
    private float _pendingRewindDelay;
    private float _rewindHoldRemaining;
    private bool _isPresenting;
    private bool _invalidConfigurationReported;

    public bool IsConfigured => _pointParticles != null && _beam != null;



    private void Awake()
    {
        CacheOriginalSettings();
        StopAndClear();
    }



    private void OnDisable() => StopAndClear();



    private void OnDestroy() => StopAndClear();



    public void BeginCharge(float baseDuration)
    {
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        StopAndClear();

        if (!ValidateConfiguration() || baseDuration <= MinimumDuration)
            return;

        ParticleSystem.MainModule pointMain = _pointParticles.main;
        pointMain.loop = true;

        ParticleSystem.MainModule beamMain = _beam.main;
        if (beamMain.startLifetime.mode != ParticleSystemCurveMode.Constant)
        {
            ReportInvalidConfiguration("Beam Start Lifetime must use Constant mode.");
            RestoreOriginalSettings();
            return;
        }

        float beamLifetime = GetBeamLifetime(beamMain.startLifetime);
        if (beamLifetime <= MinimumParticleLifetime)
        {
            ReportInvalidConfiguration("Beam Start Lifetime must be greater than zero.");
            RestoreOriginalSettings();
            return;
        }

        _beamChargeSimulationSpeed = beamLifetime / baseDuration;
        beamMain.simulationSpeed = _beamChargeSimulationSpeed;

        _pendingRewindDelay = 0f;
        _rewindHoldRemaining = 0f;
        _isPresenting = true;

        _pointParticles.Play(false);
        _beam.Play(false);
    }



    public void ApplyHitDelay(float addedDelay)
    {
        if (!_isPresenting || addedDelay <= 0f)
            return;

        _pendingRewindDelay += addedDelay;
        TryApplyPendingRewind();
    }



    public void TickCharge(float deltaTime)
    {
        if (!_isPresenting)
            return;

        TryApplyPendingRewind();

        if (_rewindHoldRemaining <= 0f)
            return;

        _rewindHoldRemaining = Mathf.Max(0f, _rewindHoldRemaining - Mathf.Max(0f, deltaTime));
        SetBeamSimulationSpeed(_rewindHoldRemaining > 0f ? 0f : _beamChargeSimulationSpeed);
    }



    public void HoldAtCompletion()
    {
        if (!_isPresenting || _beam == null)
            return;

        _pendingRewindDelay = 0f;
        _rewindHoldRemaining = 0f;
        SetBeamSimulationSpeed(0f);

        int count = _beam.GetParticles(_beamParticle);
        if (count <= 0)
            return;

        ParticleSystem.Particle particle = _beamParticle[0];
        particle.remainingLifetime = Mathf.Max(particle.remainingLifetime, MinimumParticleLifetime);
        _beamParticle[0] = particle;
        _beam.SetParticles(_beamParticle, count);
    }



    public void StopAndClear()
    {
        if (_pointParticles != null)
            _pointParticles.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (_beam != null)
            _beam.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);

        _pendingRewindDelay = 0f;
        _rewindHoldRemaining = 0f;
        _beamChargeSimulationSpeed = 0f;
        _isPresenting = false;
        RestoreOriginalSettings();
    }



    private void TryApplyPendingRewind()
    {
        if (_pendingRewindDelay <= 0f || _beam == null || _beamChargeSimulationSpeed <= 0f)
            return;

        int count = _beam.GetParticles(_beamParticle);
        if (count <= 0)
            return;

        ParticleSystem.Particle particle = _beamParticle[0];
        float particleAge = Mathf.Max(0f, particle.startLifetime - particle.remainingLifetime);
        float requestedParticleRewind = _pendingRewindDelay * _beamChargeSimulationSpeed;
        float appliedParticleRewind = Mathf.Min(particleAge, requestedParticleRewind);

        particle.remainingLifetime = Mathf.Min(
            particle.startLifetime,
            particle.remainingLifetime + appliedParticleRewind);
        _beamParticle[0] = particle;
        _beam.SetParticles(_beamParticle, count);

        float appliedRealTime = appliedParticleRewind / _beamChargeSimulationSpeed;
        _rewindHoldRemaining += Mathf.Max(0f, _pendingRewindDelay - appliedRealTime);
        _pendingRewindDelay = 0f;

        SetBeamSimulationSpeed(_rewindHoldRemaining > 0f ? 0f : _beamChargeSimulationSpeed);
    }



    private void SetBeamSimulationSpeed(float speed)
    {
        if (_beam == null)
            return;

        ParticleSystem.MainModule beamMain = _beam.main;
        beamMain.simulationSpeed = Mathf.Max(0f, speed);
    }



    private bool ValidateConfiguration()
    {
        CacheOriginalSettings();
        if (IsConfigured)
            return true;

        ReportInvalidConfiguration("Point Particles and Beam references must both be assigned.");
        return false;
    }



    private void ReportInvalidConfiguration(string message)
    {
        if (_invalidConfigurationReported)
            return;

        Debug.LogError($"[{name}] Rush charge VFX configuration is invalid: {message}", this);
        _invalidConfigurationReported = true;
    }



    private void CacheOriginalSettings()
    {
        if (_settingsCached || !IsConfigured)
            return;

        _pointParticlesOriginalLoop = _pointParticles.main.loop;
        _beamOriginalSimulationSpeed = _beam.main.simulationSpeed;
        _settingsCached = true;
    }



    private void RestoreOriginalSettings()
    {
        if (!_settingsCached)
            return;

        if (_pointParticles != null)
        {
            ParticleSystem.MainModule pointMain = _pointParticles.main;
            pointMain.loop = _pointParticlesOriginalLoop;
        }

        if (_beam != null)
        {
            ParticleSystem.MainModule beamMain = _beam.main;
            beamMain.simulationSpeed = _beamOriginalSimulationSpeed;
        }
    }



    private static float GetBeamLifetime(ParticleSystem.MinMaxCurve startLifetime)
    {
        return startLifetime.mode == ParticleSystemCurveMode.Constant
            ? startLifetime.constant
            : startLifetime.constantMax;
    }
}
