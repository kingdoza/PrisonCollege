using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName = "NewBGMPlaylist", menuName = "Sound/BGMPlaylist")]
public class BGMPlaylistData : ScriptableObject
{
    [SerializeField] private BGMInfo[] bgmList;
    [Range(0f, 1f)][SerializeField] private float baseVolume = 1f;

    private List<int> _shuffleIndices = new List<int>();
    private int _currentIndex = -1;

    /// <summary>
    /// 외부에서 강제로 셔플 상태를 초기화하고 리스트를 다시 섞습니다.
    /// </summary>
    public void ResetShuffle()
    {
        if (bgmList == null || bgmList.Length == 0) return;

        // 인덱스 리스트 생성 (0, 1, 2...)
        _shuffleIndices = Enumerable.Range(0, bgmList.Length).ToList();

        // 피셔-예이츠 셔플
        for (int i = _shuffleIndices.Count - 1; i > 0; i--)
        {
            int rnd = UnityEngine.Random.Range(0, i + 1);
            int temp = _shuffleIndices[i];
            _shuffleIndices[i] = _shuffleIndices[rnd];
            _shuffleIndices[rnd] = temp;
        }

        // 인덱스를 맨 처음 이전으로 초기화
        _currentIndex = -1;
        Debug.Log($"{name} 플레이리스트가 새로 섞였습니다.");
    }

    public AudioClip GetNextShuffleClip(out float volume, out string title)
    {
        volume = 0;
        title = string.Empty;

        if (bgmList == null || bgmList.Length == 0) return null;

        // 리스트가 비었거나 한 바퀴 다 돌았다면 자동 리셋
        if (_shuffleIndices.Count == 0 || _currentIndex >= _shuffleIndices.Count - 1)
        {
            ResetShuffle();
        }

        _currentIndex++;
        int actualIdx = _shuffleIndices[_currentIndex];

        var info = bgmList[actualIdx];
        volume = info.volumeMultiplier * baseVolume;
        title = string.IsNullOrEmpty(info.bgmTitle) ? info.clip.name : info.bgmTitle;

        return info.clip;
    }

    private void OnValidate()
    {
        if (bgmList == null) return;
        for (int i = 0; i < bgmList.Length; i++)
        {
            if (bgmList[i].volumeMultiplier <= 0) bgmList[i].volumeMultiplier = 1f;
        }
    }
}



[Serializable]
public struct BGMInfo
{
    public string bgmTitle; // UI 표시용 곡 제목
    public AudioClip clip;
    [Range(0f, 2f)] public float volumeMultiplier;
}