using UnityEngine;
using DG.Tweening;

public class WindowGate : ExitGate
{
    [Header("Prefabs & Points")]
    [SerializeField] private GameObject _windowPanelPrefab;
    [SerializeField] private Transform _spawnPoint; // 생성 위치 (Up)
    [SerializeField] private Transform _targetPoint; // 도달 위치 (Down)

    [Header("Timing Settings")]
    [SerializeField] private float _openDelay = 0.1f;    // 파괴 전 대기
    [SerializeField] private float _closeDelay = 0.5f;   // Open 후 Close까지 대기
    [SerializeField] private float _closeDuration = 0.2f; // 창문이 내려오는 속도

    private GameObject _currentWindowInstance;
    private Tween _moveTween;
    private BoxCollider _boxCollider;

    public override ExitGateType GateType => ExitGateType.Window;



    protected override void Awake()
    {
        _boxCollider = GetComponent<BoxCollider>();
        base.Awake();
    }

    public override void Open()
    {
        // 이미 열려있거나 파괴 중이면 중단
        if (_currentWindowInstance == null) return;

        // _openDelay 후에 창문 파괴 및 Close 예약
        DOVirtual.DelayedCall(_openDelay, () =>
        {
            if (_currentWindowInstance != null)
            {
                Destroy(_currentWindowInstance);
                _currentWindowInstance = null;
                base.Open(); // ExitGate의 공통 로직 실행 (사운드 등)
            }

            // 창문이 깨진 후 _closeDelay 뒤에 자동으로 다시 닫힘
            CancelInvoke(nameof(Close));
            Invoke(nameof(Close), _closeDelay);
        }, false);
    }

    public override void Close()
    {
        // 이미 창문이 있으면 생성하지 않음
        if (_currentWindowInstance != null) return;

        base.Close();

        // 1. 창문 생성 (Up 위치)
        _currentWindowInstance = Instantiate(_windowPanelPrefab, _spawnPoint);
        Vector3 targetScale = _boxCollider.size;
        targetScale.x *= 0.1f;
        _currentWindowInstance.transform.localScale = targetScale;

        // 2. DOTween으로 목표 위치(Down)까지 내리기
        _moveTween?.Kill();
        _moveTween = _currentWindowInstance.transform.DOMove(_targetPoint.position, _closeDuration)
            .SetEase(Ease.OutQuad);
    }
}