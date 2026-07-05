using System.Collections.Generic;
using UnityEngine;

public class SoundEmitterOwner : MonoBehaviour
{
    private Dictionary<SoundData, (SoundEmitter emitter, Transform target)> _activeEmitters =
        new Dictionary<SoundData, (SoundEmitter, Transform)>();



    private void Update()
    {
        UpdateEmitterPositions();
        CleanUpFinishedEmitters();
    }




    public void Play3DSound(SoundData data, Transform target, bool isRandomPitch, float volumeMultiplier = 1f, bool loop = false)
    {
        if (data == null) return;
        if (_activeEmitters.ContainsKey(data)) return;
        SoundEmitter emitter = SoundUtils.PlayOwnedScene3DSFX(data, target.position, isRandomPitch, volumeMultiplier, loop);

        if (emitter != null)
        {
            _activeEmitters.Add(data, (emitter, target));
        }
    }



    public void Play2DSound(SoundData data, bool isRandomPitch, float volumeMultiplier = 1f, bool loop = false)
    {
        if (data == null) return;
        if (_activeEmitters.ContainsKey(data)) return;
        SoundEmitter emitter = SoundUtils.PlayOwnedScene2DSFX(data, isRandomPitch, volumeMultiplier, loop);
        if (emitter != null)
        {
            _activeEmitters.Add(data, (emitter, null));
        }
    }




    public void StopSound(SoundData data)
    {
        if (data != null && _activeEmitters.TryGetValue(data, out var pair))
        {
            pair.emitter.StopAndReturn();
            _activeEmitters.Remove(data);
        }
    }




    public void StopAllSounds()
    {
        foreach (var pair in _activeEmitters.Values)
        {
            if (pair.emitter != null) pair.emitter.StopAndReturn();
        }
        _activeEmitters.Clear();
    }




    private void UpdateEmitterPositions()
    {
        foreach (var kvp in _activeEmitters)
        {
            var data = kvp.Key;
            var (emitter, target) = kvp.Value;

            if (emitter != null && emitter.gameObject.activeSelf && target != null)
            {
                emitter.transform.position = target.position;
            }
        }
    }



    private void CleanUpFinishedEmitters()
    {
        List<SoundData> finishedList = null;

        foreach (var kvp in _activeEmitters)
        {
            // kvp.Value는 이제 (emitter, target) 튜플임
            if (!kvp.Value.emitter.gameObject.activeSelf)
            {
                if (finishedList == null) finishedList = new List<SoundData>();
                finishedList.Add(kvp.Key);
            }
        }

        if (finishedList != null)
        {
            foreach (var key in finishedList) _activeEmitters.Remove(key);
        }
    }



    private void OnDisable() => StopAllSounds();
    private void OnDestroy() => StopAllSounds();
}
