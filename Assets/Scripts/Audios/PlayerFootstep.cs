using System.Collections.Generic;
using UnityEngine;
using static Unity.Collections.Unicode;

public class PlayerFootstep : MonoBehaviour
{
    [Header("Sound Data")]
    [SerializeField] private SoundData _walkSD;
    [SerializeField] private SoundData _sprintSD;
    [Header("Settings")]
    [SerializeField] private float _walkStepInterval = 0.5f;   // 걷기 간격
    [SerializeField] private float _sprintStepInterval = 0.35f;   // 달리기 간격

    private FirstPersonController _controller;
    private List<SoundEmitter> _activeFootsteps = new List<SoundEmitter>();
    private float _stepTimer;



    private void Awake()
    {
        _controller = GetComponent<FirstPersonController>();
        GetComponent<Professor>().DieEvent.AddListener(_ => OnPlayerDeath());
    }



    private void Update()
    {
        // 1. 플레이어가 지면에 있고, 일정 속도 이상 움직일 때만 타이머 작동
        if (_controller.IsGrounded && _controller.IsWalking)
        {
            HandleFootstepTimer();
        }
        else
        {
            _stepTimer = _walkStepInterval;
        }
        CheckInvalidSoundEmitters();
    }



    private void HandleFootstepTimer()
    {
        float currentInterval = _controller.IsSprinting ? _sprintStepInterval : _walkStepInterval;

        _stepTimer += Time.deltaTime;

        if (_stepTimer >= currentInterval)
        {
            PlayFootstep(_controller.IsSprinting);
            _stepTimer = 0f;
        }
    }



    private void PlayFootstep(bool isSprint)
    {
        SoundUtils.PlayScene2DSFX(isSprint ? _sprintSD : _walkSD, 1, false);
        //_activeFootsteps.Add(emitter);
    }



    private void CheckInvalidSoundEmitters()
    {
        for (int i = _activeFootsteps.Count - 1; i >= 0; i--)
        {
            SoundEmitter emitter = _activeFootsteps[i];
            if (emitter == null || !emitter.gameObject.activeSelf)
            {
                _activeFootsteps.RemoveAt(i);
                continue;
            }
        }
    }



    public void OnPlayerDeath()
    {
        foreach (var emitter in _activeFootsteps)
        {
            if (emitter != null) emitter.StopAndReturn();
        }
        _activeFootsteps.Clear();
    }



    private void OnDisable()
    {
        OnPlayerDeath();
    }


    private void OnDestroy()
    {
        OnPlayerDeath();
    }
}
