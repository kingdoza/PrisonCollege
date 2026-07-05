using System.Collections.Generic;
using UnityEngine;
using System.Linq;

// 이 클래스는 추상(abstract) 클래스로 만듭니다.
public abstract class WeightedSetSO<T, TEntry> : ScriptableObject
    where TEntry : IWeightedEntry<T>
{
    [SerializeField] private List<TEntry> _weightedElements = new List<TEntry>();

    public List<TEntry> WeightedElements => _weightedElements;

    protected abstract TEntry CloneEntry(TEntry entry);

    public void InitializeCopy(List<TEntry> sourceElements)
    {
        _weightedElements = new List<TEntry>();
        foreach (var element in sourceElements)
        {
            _weightedElements.Add(CloneEntry(element));
        }
    }

    public T GetRandomValue()
    {
        if (_weightedElements == null || _weightedElements.Count == 0)
            return default;

        float totalWeight = _weightedElements.Sum(e => e.Chance);
        if (totalWeight <= 0) return default;

        float pivot = Random.Range(0f, totalWeight);
        float cumulative = 0f;

        foreach (var entry in _weightedElements)
        {
            cumulative += entry.Chance;
            if (pivot <= cumulative) return entry.Value;
        }

        return _weightedElements.Last().Value;
    }
}