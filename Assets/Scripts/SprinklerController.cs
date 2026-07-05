using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;
using Unity.Content;

public class SprinklerController : MonoBehaviour
{
    [SerializeField] private float _fadeDuration;
    [SerializeField] private float _targetEmission;
    [SerializeField] private Transform _fireAlarmPoint;
    [SerializeField] private SoundData _fireAlarmSD;
    [SerializeField] private SoundData _sprinklerSD;
    private ParticleSystem[] _rainParticles;
    private float _currentEmissionRate = 0f; // 현재 보간 값을 저장할 변수
    private Tweener _emissionTweener;

    private SoundEmitter _fireAlarmEmitter;
    private List<SoundEmitter> _sprinklerEmitters;

    private void Awake()
    {
        _rainParticles = GetComponentsInChildren<ParticleSystem>();
    }

    public void TurnOn()
    {
        PlayEmissionTween(_targetEmission, _fadeDuration);
        _fireAlarmEmitter = SoundUtils.PlayOwnedScene3DSFX(_fireAlarmSD, _fireAlarmPoint.position, false, 1, true);
        _sprinklerEmitters = new();
        foreach (Transform child in transform)
        {
            if (child.GetComponent<ParticleSystem>() == null) continue;
            SoundEmitter sprinklerEmitter = SoundUtils.PlayOwnedScene3DSFX(_sprinklerSD, child.position, true, 1, true);
            _sprinklerEmitters.Add(sprinklerEmitter);
        }
    }

    public void TurnOff()
    {
        PlayEmissionTween(0f, _fadeDuration);
        _fireAlarmEmitter?.StopAndReturn();
        foreach (SoundEmitter sprinklerEmitter in _sprinklerEmitters)
        {
            if (sprinklerEmitter == null) continue;
            sprinklerEmitter.StopAndReturn();
        }
        _sprinklerEmitters?.Clear();
    }

    public void TurnOffImmediate()
    {
        _emissionTweener?.Kill();
        SetEmissionRate(0f);

        foreach (var ps in _rainParticles)
        {
            if (ps == null) continue;
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    private void PlayEmissionTween(float targetValue, float duration)
    {
        _emissionTweener?.Kill();
        _emissionTweener = DOTween.To(() => _currentEmissionRate,
                                     x => SetEmissionRate(x),
                                     targetValue,
                                     duration)
                                  .SetEase(Ease.OutQuad); // 자연스러운 가속/감속
    }

    private void SetEmissionRate(float rate)
    {
        _currentEmissionRate = rate;

        foreach (var ps in _rainParticles)
        {
            if (ps == null) continue;

            var emission = ps.emission;
            emission.rateOverTime = rate;
            if (rate > 0.1f && !ps.isPlaying) ps.Play();
            else if (rate <= 0.1f && ps.isPlaying) ps.Stop();
        }
    }
}