using UnityEngine;

public class HackingVfxController : MonoBehaviour
{
    [SerializeField] private GameObject _vfxRoot;
    [SerializeField] private ParticleSystem[] _particles;

    private bool _isPlaying;

    private void Awake()
    {
        CacheParticlesIfNeeded();
        Stop();
    }

    public void Play()
    {
        if (_isPlaying) return;

        CacheParticlesIfNeeded();

        if (_vfxRoot != null)
        {
            _vfxRoot.SetActive(true);
        }

        foreach (ParticleSystem particle in _particles)
        {
            if (particle == null) continue;

            particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particle.Play(true);
        }

        _isPlaying = true;
    }

    public void Stop()
    {
        CacheParticlesIfNeeded();

        foreach (ParticleSystem particle in _particles)
        {
            if (particle == null) continue;

            particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        if (_vfxRoot != null)
        {
            _vfxRoot.SetActive(false);
        }

        _isPlaying = false;
    }

    private void CacheParticlesIfNeeded()
    {
        if (_particles != null && _particles.Length > 0) return;

        _particles = GetComponentsInChildren<ParticleSystem>(true);
    }
}
