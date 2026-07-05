using System;
using UnityEngine;

[CreateAssetMenu(fileName = "NewSoundData", menuName = "Sound/SoundData")]
public class SoundData : ScriptableObject
{
    [SerializeField] private SoundInfo[] soundInfos; // 랜덤으로 재생될 클립들
    [Range(0f, 5f)] [SerializeField] private float baseVolume = 1f; // 이 사운드 세트의 기본 볼륨

    // 랜덤하게 클립 하나를 가져오는 도우미 함수
    public AudioClip GetRandomClip(out float volume)
    {
        volume = 0;
        if (soundInfos == null || soundInfos.Length == 0) return null;
        int randIdx = UnityEngine.Random.Range(0, soundInfos.Length);
        volume = soundInfos[randIdx].volumeMultiplier * baseVolume;
        return soundInfos[randIdx].clip;
    }



    private void OnValidate()
    {
        if (soundInfos == null) return;

        for (int i = 0; i < soundInfos.Length; i++)
        {
            // 볼륨이 0이면(초기 상태) 기본값 1로 강제 고정
            if (soundInfos[i].volumeMultiplier <= 0)
            {
                var info = soundInfos[i];
                info.volumeMultiplier = 1f;
                soundInfos[i] = info;
            }
        }
    }
}


[Serializable] // 인스펙터에 노출하기 위해 필수!
public struct SoundInfo
{
    public AudioClip clip;
    [Range(0f, 2f)] public float volumeMultiplier; // 기본값 1.0 기준 개별 조절
}