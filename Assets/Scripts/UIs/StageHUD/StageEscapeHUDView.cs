using System.Collections.Generic;
using UnityEngine;

public class StageEscapeHUDView : MonoBehaviour
{
    [SerializeField] private Transform _slotContainer;
    [SerializeField] private StageEscapeSlotView _slotPrefab;

    private readonly List<StageEscapeSlotView> _slots = new();
    private StageController _source;
    private int _lastThreshold = -1;
    private int _lastEscapeCount = -1;
    private bool _initialized;

    public bool Initialize(StageController source)
    {
        Shutdown();
        if (source == null || _slotContainer == null || _slotPrefab == null)
        {
            Debug.LogError("StageEscapeHUDView의 StageController, Slot Container 또는 Slot Prefab 참조가 누락됐습니다.", this);
            return false;
        }
        if (!_slotPrefab.IsValid)
        {
            Debug.LogError("StageEscapeSlotView 프리팹에는 잔여/실패 아이콘과 Shake Visual 참조가 모두 필요합니다.", _slotPrefab);
            return false;
        }

        _source = source;
        _initialized = true;
        Refresh(true, false);
        _source.StudentEscaped += OnStudentEscaped;
        return true;
    }

    public void Shutdown()
    {
        if (_source != null)
            _source.StudentEscaped -= OnStudentEscaped;
        foreach (StageEscapeSlotView slot in _slots)
        {
            if (slot != null)
                slot.Shutdown();
        }

        _source = null;
        _initialized = false;
        _lastThreshold = -1;
        _lastEscapeCount = -1;
    }

    private void Update()
    {
        if (_initialized)
            Refresh(false, false);
    }

    private void OnStudentEscaped(PostStudent student)
    {
        if (_initialized)
            Refresh(false, true);
    }

    private void Refresh(bool force, bool animateNewestFailure)
    {
        int threshold = Mathf.Max(0, _source.EscapeFailureThreshold);
        int escapeCount = Mathf.Clamp(_source.EscapeCount, 0, threshold);

        if (force || threshold != _lastThreshold)
        {
            EnsureSlotCount(threshold);
            _lastThreshold = threshold;
            force = true;
            animateNewestFailure = false;
        }

        if (!force && escapeCount == _lastEscapeCount) return;

        int failedStartIndex = _slots.Count - escapeCount;
        int newestFailedIndex = animateNewestFailure && escapeCount > 0
            ? failedStartIndex
            : -1;
        for (int i = 0; i < _slots.Count; i++)
            _slots[i].SetFailed(i >= failedStartIndex, i == newestFailedIndex);

        _lastEscapeCount = escapeCount;
    }

    private void EnsureSlotCount(int count)
    {
        while (_slots.Count > count)
        {
            int lastIndex = _slots.Count - 1;
            StageEscapeSlotView slot = _slots[lastIndex];
            _slots.RemoveAt(lastIndex);
            if (slot != null)
            {
                slot.Shutdown();
                Destroy(slot.gameObject);
            }
        }

        while (_slots.Count < count)
        {
            StageEscapeSlotView slot = Instantiate(_slotPrefab, _slotContainer);
            slot.gameObject.SetActive(true);
            slot.Initialize();
            slot.SetFailed(false, false);
            _slots.Add(slot);
        }
    }
}
