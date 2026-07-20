using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct TutorialMarkerDisplayProfile
{
    [Min(0f)]
    [Tooltip("대상 기준 월드 Y축 높이입니다.")]
    public float height;

    [Min(0.01f)]
    [Tooltip("마커 prefab 원본 스케일에 곱할 배율입니다.")]
    public float scaleMultiplier;
}

/// <summary>
/// 하나의 목표 마커 prefab을 위치 표시와 학생 표시에서 공유합니다.
/// 표시 대상 추적과 인스턴스 풀만 담당하며 단계 판정에는 관여하지 않습니다.
/// </summary>
public sealed class TutorialObjectiveMarkerPresenter : MonoBehaviour
{
    [Header("Shared marker prefab")]
    [Tooltip("TutorialMarkerVisual이 부착된 순수 시각 prefab입니다. Collider나 TutorialMovementMarker를 포함하지 않습니다.")]
    [SerializeField] private TutorialMarkerVisual _markerPrefab;

    [Tooltip("런타임 마커 인스턴스의 부모입니다. 비어 있으면 이 컴포넌트의 Transform을 사용합니다.")]
    [SerializeField] private Transform _runtimeRoot;

    [Header("Display profiles")]
    [SerializeField] private TutorialMarkerDisplayProfile _locationProfile = new()
    {
        height = 1f,
        scaleMultiplier = 1f,
    };
    [SerializeField] private TutorialMarkerDisplayProfile _studentProfile = new()
    {
        height = 2.15f,
        scaleMultiplier = 0.55f,
    };
    [SerializeField] private TutorialMarkerDisplayProfile _worldTargetProfile = new()
    {
        height = 1f,
        scaleMultiplier = 0.75f,
    };

    [Header("Pool")]
    [Min(1)]
    [Tooltip("3단계 위험 학생 4명을 동시에 표시할 수 있도록 기본 4개를 권장합니다.")]
    [SerializeField] private int _prewarmCount = 4;

    private readonly List<MarkerInstance> _pool = new();
    private readonly Dictionary<PostStudent, MarkerInstance> _studentMarkers = new();
    private readonly Dictionary<UnityEngine.Object, MarkerInstance> _worldTargetMarkers = new();
    private readonly List<PostStudent> _invalidStudentTargets = new();
    private readonly List<UnityEngine.Object> _invalidWorldTargets = new();
    private MarkerInstance _locationMarker;
    private bool _isInitialized;

    public bool IsInitialized => _isInitialized;



    public bool InitializePresenter()
    {
        if (_isInitialized) return true;
        if (_markerPrefab == null)
        {
            Debug.LogError("TutorialObjectiveMarkerPresenter Marker Prefab이 연결되지 않았습니다.", this);
            return false;
        }
        if (!_markerPrefab.gameObject.activeSelf)
        {
            Debug.LogError("목표 마커 prefab의 루트 GameObject는 활성 상태여야 합니다.", _markerPrefab);
            return false;
        }
        if (!_markerPrefab.enabled)
        {
            Debug.LogError("목표 마커 prefab의 TutorialMarkerVisual 컴포넌트가 비활성 상태입니다.", _markerPrefab);
            return false;
        }
        if (_markerPrefab.GetComponentInChildren<Collider>(true) != null
            || _markerPrefab.GetComponentInChildren<TutorialMovementMarker>(true) != null)
        {
            Debug.LogError("목표 마커 prefab은 순수 시각 오브젝트여야 하며 Collider나 TutorialMovementMarker를 포함할 수 없습니다.", _markerPrefab);
            return false;
        }
        if (_locationProfile.scaleMultiplier <= 0f
            || _studentProfile.scaleMultiplier <= 0f
            || _worldTargetProfile.scaleMultiplier <= 0f)
        {
            Debug.LogError("위치/학생/월드 대상 목표 마커 Scale Multiplier는 0보다 커야 합니다.", this);
            return false;
        }
        if (_locationProfile.height < 0f
            || _studentProfile.height < 0f
            || _worldTargetProfile.height < 0f)
        {
            Debug.LogError("위치/학생/월드 대상 목표 마커 Height는 0 이상이어야 합니다.", this);
            return false;
        }

        int count = Mathf.Max(1, _prewarmCount);
        for (int i = 0; i < count; i++)
            CreateInstance();
        _isInitialized = true;
        return true;
    }



    public bool ShowLocationMarker(Transform anchor)
    {
        if (!_isInitialized || anchor == null) return false;
        HideLocationMarker();
        _locationMarker = Acquire(anchor, _locationProfile);
        return _locationMarker != null;
    }



    public void HideLocationMarker()
    {
        if (_locationMarker == null) return;
        Release(_locationMarker);
        _locationMarker = null;
    }



    public bool ShowStudentMarker(PostStudent student)
    {
        if (!_isInitialized || student == null) return false;
        if (_studentMarkers.TryGetValue(student, out MarkerInstance existing))
        {
            existing.target = student.transform;
            existing.profile = _studentProfile;
            UpdateFollowTransform(existing);
            return true;
        }

        MarkerInstance marker = Acquire(student.transform, _studentProfile);
        if (marker == null) return false;
        _studentMarkers.Add(student, marker);
        return true;
    }



    public void HideStudentMarker(PostStudent student)
    {
        if (ReferenceEquals(student, null)
            || !_studentMarkers.TryGetValue(student, out MarkerInstance marker))
            return;
        _studentMarkers.Remove(student);
        Release(marker);
    }



    public bool ShowWorldTargetMarker(UnityEngine.Object key, Transform anchor)
    {
        if (!_isInitialized || key == null || anchor == null) return false;
        if (_worldTargetMarkers.TryGetValue(key, out MarkerInstance existing))
        {
            existing.target = anchor;
            existing.profile = _worldTargetProfile;
            UpdateFollowTransform(existing);
            return true;
        }

        MarkerInstance marker = Acquire(anchor, _worldTargetProfile);
        if (marker == null) return false;
        _worldTargetMarkers.Add(key, marker);
        return true;
    }



    public void HideWorldTargetMarker(UnityEngine.Object key)
    {
        if (ReferenceEquals(key, null)
            || !_worldTargetMarkers.TryGetValue(key, out MarkerInstance marker))
            return;
        _worldTargetMarkers.Remove(key);
        Release(marker);
    }



    public void ClearAll()
    {
        HideLocationMarker();
        foreach (MarkerInstance marker in _studentMarkers.Values)
            Release(marker);
        _studentMarkers.Clear();
        foreach (MarkerInstance marker in _worldTargetMarkers.Values)
            Release(marker);
        _worldTargetMarkers.Clear();
        _invalidStudentTargets.Clear();
        _invalidWorldTargets.Clear();
    }



    private void LateUpdate()
    {
        if (!_isInitialized) return;

        if (_locationMarker != null)
        {
            if (_locationMarker.target == null)
                HideLocationMarker();
            else
                UpdateFollowTransform(_locationMarker);
        }

        _invalidStudentTargets.Clear();
        foreach (KeyValuePair<PostStudent, MarkerInstance> pair in _studentMarkers)
        {
            PostStudent student = pair.Key;
            if (student == null || !student.gameObject.activeInHierarchy)
            {
                _invalidStudentTargets.Add(student);
                continue;
            }
            UpdateFollowTransform(pair.Value);
        }
        foreach (PostStudent student in _invalidStudentTargets)
            HideStudentMarker(student);

        _invalidWorldTargets.Clear();
        foreach (KeyValuePair<UnityEngine.Object, MarkerInstance> pair in _worldTargetMarkers)
        {
            if (pair.Key == null || pair.Value.target == null)
            {
                _invalidWorldTargets.Add(pair.Key);
                continue;
            }
            UpdateFollowTransform(pair.Value);
        }
        foreach (UnityEngine.Object key in _invalidWorldTargets)
            HideWorldTargetMarker(key);
    }



    private MarkerInstance Acquire(Transform target, TutorialMarkerDisplayProfile profile)
    {
        MarkerInstance marker = null;
        foreach (MarkerInstance candidate in _pool)
        {
            if (candidate.inUse) continue;
            marker = candidate;
            break;
        }
        marker ??= CreateInstance();
        if (marker == null) return null;

        marker.inUse = true;
        marker.target = target;
        marker.profile = profile;
        marker.visual.transform.localPosition = marker.baseLocalPosition;
        marker.visual.transform.localRotation = marker.baseLocalRotation;
        marker.visual.transform.localScale = marker.baseLocalScale * profile.scaleMultiplier;
        UpdateFollowTransform(marker);
        marker.followRoot.SetActive(true);
        marker.visual.RestartMotion();
        return marker;
    }



    private void Release(MarkerInstance marker)
    {
        if (marker == null || !marker.inUse) return;
        if (marker.followRoot != null)
            marker.followRoot.SetActive(false);
        marker.target = null;
        marker.inUse = false;
    }



    private MarkerInstance CreateInstance()
    {
        Transform parent = _runtimeRoot != null ? _runtimeRoot : transform;
        GameObject followRoot = new("TutorialObjectiveMarker_Runtime");
        followRoot.transform.SetParent(parent, false);
        followRoot.SetActive(false);

        TutorialMarkerVisual visual = Instantiate(_markerPrefab, followRoot.transform, false);
        MarkerInstance marker = new()
        {
            followRoot = followRoot,
            visual = visual,
            baseLocalPosition = visual.transform.localPosition,
            baseLocalRotation = visual.transform.localRotation,
            baseLocalScale = visual.transform.localScale,
        };
        _pool.Add(marker);
        return marker;
    }



    private static void UpdateFollowTransform(MarkerInstance marker)
    {
        if (marker == null || marker.followRoot == null || marker.target == null) return;
        Transform followTransform = marker.followRoot.transform;
        followTransform.position = marker.target.position + Vector3.up * marker.profile.height;
        followTransform.rotation = Quaternion.identity;
    }



    private void OnDisable()
    {
        ClearAll();
    }



    private void OnDestroy()
    {
        foreach (MarkerInstance marker in _pool)
        {
            if (marker?.followRoot != null)
                Destroy(marker.followRoot);
        }
        _pool.Clear();
    }



    private sealed class MarkerInstance
    {
        public GameObject followRoot;
        public TutorialMarkerVisual visual;
        public Transform target;
        public TutorialMarkerDisplayProfile profile;
        public Vector3 baseLocalPosition;
        public Quaternion baseLocalRotation;
        public Vector3 baseLocalScale;
        public bool inUse;
    }
}
