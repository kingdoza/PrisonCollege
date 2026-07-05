using UnityEngine;

public class LerpStatBar : StatBar
{
    [SerializeField] private float _interpSpeed = 5f;
    private float _targetRatio;

    protected override void Start()
    {
        base.Start(); // 부모의 Start(이벤트 바인딩 등) 실행
        ResetLerp();
    }

    // [핵심] 대상을 갈아끼울 때 호출될 로직
    public override void SetTarget(Stat newStat)
    {
        base.SetTarget(newStat); // 부모의 SetTarget(구독 해제/재등록) 실행
        ResetLerp(); // 새 학생을 보자마자 바를 현재 체력에 즉시 맞춤
    }

    private void ResetLerp()
    {
        _targetRatio = _targetStat != null ? _targetStat.Ratio : 0;
        // 조준 대상을 바꿨을 때 바가 이전 위치에서 슬금슬금 오지 않도록 즉시 설정
        if (_fillImage != null)
        {
            UpdateUI(_targetRatio);
        }
    }

    // 부모 클래스의 OnStatChanged 매개변수(float amount) 형식을 맞춰야 합니다.
    protected override void OnStatChanged()
    {
        // 실시간으로 변하는 목표값만 갱신
        _targetRatio = _targetStat != null ? _targetStat.Ratio : 0;
    }

    private void Update()
    {
        if (_fillImage == null) return;

        float current = _fillImage.fillAmount;

        // 근사치 체크 후 부드럽게 보간
        if (!Mathf.Approximately(current, _targetRatio))
        {
            float next = Mathf.Lerp(current, _targetRatio, Time.deltaTime * _interpSpeed);
            UpdateUI(next);
        }
    }
}
