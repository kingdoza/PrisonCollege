using UnityEngine;
using DG.Tweening; // DOTween 네임스페이스 추가

public class OutlineFader : MonoBehaviour
{
    [SerializeField] private float _fadeDuration = 0.5f;
    [SerializeField] private float _holdDuration = 0.2f; // 페이드 사이 대기 시간

    private Outline _outline;
    private Color _originColor;
    private Color _transparentColor;
    private DG.Tweening.Sequence _fadeSequence;

    private void Awake()
    {
        _outline = GetComponent<Outline>();

        // 원본 색상과 투명한 색상 설정
        //_originColor = _outline.OutlineColor;
        _originColor = new Color(_outline.OutlineColor.r, _outline.OutlineColor.g, _outline.OutlineColor.b, 0.5f);
        _transparentColor = new Color(_originColor.r, _originColor.g, _originColor.b, 0.1f);

        // 스테이지 시작 시 페이드 중지 이벤트 연결
        if (StageController.Instance != null)
            StageController.Instance.StageStartEvent.AddListener(StopFade);

        // 루프 시작
    }


    private void Start()
    {
        if (StageController.Instance.IsPreparing)
            StartInfiniteFade();
        else
            _outline.enabled = false;
    }

    private void StartInfiniteFade()
    {
        // 기존 시퀀스가 있다면 제거
        _fadeSequence?.Kill();

        // 외곽선 초기 상태를 투명으로 설정
        _outline.OutlineColor = _transparentColor;
        _outline.OutlineWidth = 20;

        _fadeSequence = DOTween.Sequence()
            .Append(DOTween.To(() => _outline.OutlineColor, x => {
                _outline.OutlineColor = x;
            }, _originColor, _fadeDuration)) // Fade In
            .AppendInterval(_holdDuration)   // 유지
            .Append(DOTween.To(() => _outline.OutlineColor, x => {
                _outline.OutlineColor = x;
            }, _transparentColor, _fadeDuration)) // Fade Out
            .AppendInterval(_holdDuration)   // 유지
            .SetLoops(-1); // 무한 반복
    }

    private void StopFade()
    {
        if (_fadeSequence != null)
        {
            _fadeSequence.Kill();
            // 중지 시 외곽선을 완전히 끄거나 원본 색상으로 복구
            _outline.OutlineColor = _transparentColor;
            _outline.enabled = false;
        }
    }

    private void OnDestroy()
    {
        _fadeSequence?.Kill();
    }
}