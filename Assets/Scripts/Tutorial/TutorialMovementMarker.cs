using System;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class TutorialMovementMarker : MonoBehaviour
{
    [SerializeField] private Professor _player;
    [Tooltip("위치 표시용 마커 기준점입니다. 비어 있으면 이 오브젝트의 Transform을 사용합니다.")]
    [SerializeField] private Transform _markerAnchor;
    [Tooltip("1단계 목표 지점 바닥에 함께 표시할 정적 메시/데칼 루트입니다. Collider를 포함하지 않습니다.")]
    [SerializeField] private GameObject _staticVisualRoot;

    [Header("Legacy scene visual fallback")]
    [SerializeField] private GameObject _visualRoot;
    [Tooltip("Visual Root에 연결된 공용 마커 애니메이션입니다. 비어 있으면 런타임에 Visual Root에서 확인하고, 없을 경우 기본 설정으로 추가합니다.")]
    [SerializeField] private TutorialMarkerVisual _markerVisual;
    private TutorialObjectiveMarkerPresenter _objectiveMarkerPresenter;
    private bool _wasReached;

    public event Action Reached;

    private void Awake()
    {
        ResolveMarkerVisual();
        if (_visualRoot != null) _visualRoot.SetActive(false);
        if (_staticVisualRoot != null) _staticVisualRoot.SetActive(false);
    }



    public bool InitializeMarker(TutorialObjectiveMarkerPresenter objectiveMarkerPresenter)
    {
        if (objectiveMarkerPresenter == null || !objectiveMarkerPresenter.IsInitialized)
        {
            Debug.LogError("TutorialMovementMarker에 초기화된 목표 마커 Presenter가 전달되지 않았습니다.", this);
            return false;
        }
        if (_staticVisualRoot == null)
        {
            Debug.LogError("TutorialMovementMarker Static Visual Root가 연결되지 않았습니다.", this);
            return false;
        }
        _objectiveMarkerPresenter = objectiveMarkerPresenter;
        if (_visualRoot != null) _visualRoot.SetActive(false);
        _staticVisualRoot.SetActive(false);
        return true;
    }



    public void ActivateMarker()
    {
        TryActivateMarker();
    }



    public bool TryActivateMarker()
    {
        if (this == null) return false;
        _wasReached = false;
        gameObject.SetActive(true);
        if (_objectiveMarkerPresenter != null)
        {
            Transform anchor = _markerAnchor != null ? _markerAnchor : transform;
            if (!_objectiveMarkerPresenter.ShowLocationMarker(anchor))
                return false;
            _staticVisualRoot.SetActive(true);
            return true;
        }
        if (_visualRoot != null)
        {
            _visualRoot.SetActive(true);
            _markerVisual?.RestartMotion();
            _staticVisualRoot.SetActive(true);
            return true;
        }
        return false;
    }



    public void DeactivateMarker()
    {
        if (this == null) return;
        _objectiveMarkerPresenter?.HideLocationMarker();
        if (_visualRoot != null) _visualRoot.SetActive(false);
        if (_staticVisualRoot != null) _staticVisualRoot.SetActive(false);
        gameObject.SetActive(false);
    }



    private void OnTriggerEnter(Collider other)
    {
        if (_wasReached || _player == null) return;
        Professor professor = other.GetComponentInParent<Professor>();
        if (professor != _player) return;
        _wasReached = true;
        Reached?.Invoke();
    }



    private void ResolveMarkerVisual()
    {
        if (_markerVisual != null || _visualRoot == null) return;
        if (!_visualRoot.TryGetComponent(out _markerVisual))
            _markerVisual = _visualRoot.AddComponent<TutorialMarkerVisual>();
    }
}
