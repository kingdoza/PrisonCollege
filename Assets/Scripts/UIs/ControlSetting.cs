using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ControlSetting : MonoBehaviour
{
    [Header("Mouse Settings")]
    [SerializeField] private Slider _mouseSensitivitySlider;
    [SerializeField] private TextMeshProUGUI _sensitivityValueTmp;

    [Header("FOV Settings")]
    [SerializeField] private Slider _fovSlider;
    [SerializeField] private TextMeshProUGUI _fovValueTmp;

    [Header("Sprint Mode (Toggle Group)")]
    [SerializeField] private Toggle _toggleModeBtn; // "토글 방식" 버튼
    [SerializeField] private Toggle _holdModeBtn;   // "홀드 방식" 버튼
    [SerializeField] private Image _toggleModeBg;   // 홀드 방식 배경 이미지
    [SerializeField] private Image _holdModeBg;   // 홀드 방식 배경 이미지
    [SerializeField] private Color _activeModeColor; // 선택되었을 때 색상
    private Color _originModeColor;



    private void Awake()
    {
        _originModeColor = _toggleModeBg.color;
    }

    private void Start()
    {
        // 1. 기존 설정값 로드 (0: 홀드, 1: 토글)
        float savedSens = PlayerPrefs.GetFloat("MouseSensitivity", 3.0f);
        float savedFov = PlayerPrefs.GetFloat("FOV", 80);
        int savedSprintMode = PlayerPrefs.GetInt("SprintMode", 0);

        // 2. UI 초기 설정
        _mouseSensitivitySlider.value = savedSens;
        UpdateSensitivityText(savedSens);

        _fovSlider.value = savedFov;
        UpdateFOVText(savedFov);

        // 저장된 값에 따라 버튼 체크 상태 결정
        if (savedSprintMode == 0) _toggleModeBtn.isOn = true;
        else _holdModeBtn.isOn = true;

        // 3. 이벤트 연결
        _mouseSensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);

        _fovSlider.onValueChanged.AddListener(OnFOVChanged);

        // 토글 버튼들이 켜질 때(isOn == true)만 저장 로직 실행
        _toggleModeBtn.onValueChanged.AddListener(isOn => { if (isOn) OnSprintModeChanged(0); });
        _holdModeBtn.onValueChanged.AddListener(isOn => { if (isOn) OnSprintModeChanged(1); });

        UpdateToggleVisual(_toggleModeBtn.isOn, _toggleModeBg);
        UpdateToggleVisual(_holdModeBtn.isOn, _holdModeBg);

        // 이벤트 연결: 값이 바뀔 때마다 시각적 효과 업데이트
        _toggleModeBtn.onValueChanged.AddListener(isOn => {
            UpdateToggleVisual(isOn, _toggleModeBg);
            if (isOn) OnSprintModeChanged(0);
        });

        _holdModeBtn.onValueChanged.AddListener(isOn => {
            UpdateToggleVisual(isOn, _holdModeBg);
            if (isOn) OnSprintModeChanged(1);
        });
    }

    private void OnFOVChanged(float value)
    {
        // 1. 값 저장
        PlayerPrefs.SetFloat("FOV", value);

        // 2. 텍스트 업데이트
        UpdateFOVText(value);

        // 3. 게임 매니저에 알림 (카메라 FOV를 실시간으로 바꾸기 위함)
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ControlSettingChanged();
        }
    }

    // --- FOV 텍스트 업데이트 ---
    private void UpdateFOVText(float value)
    {
        if (_fovValueTmp != null)
        {
            // FOV는 보통 정수로 보는 게 깔끔해서 "F0" (소수점 없음) 추천합니다
            _fovValueTmp.text = value.ToString("F0");
        }
    }

    private void UpdateToggleVisual(bool isOn, Image bgImage)
    {
        if (bgImage != null)
        {
            bgImage.color = isOn ? _activeModeColor : _originModeColor;
        }
    }

    private void OnSensitivityChanged(float value)
    {
        PlayerPrefs.SetFloat("MouseSensitivity", value);
        UpdateSensitivityText(value);
        GameManager.Instance.ControlSettingChanged();
    }

    private void OnSprintModeChanged(int modeIndex)
    {
        // 0은 홀드, 1은 토글로 정의하여 저장
        PlayerPrefs.SetInt("SprintMode", modeIndex);
        Debug.Log(modeIndex == 0 ? "달리기 방식: 토글" : "달리기 방식: 홀드");
        GameManager.Instance.ControlSettingChanged();
    }

    private void UpdateSensitivityText(float value)
    {
        if (_sensitivityValueTmp != null)
            _sensitivityValueTmp.text = value.ToString("F1");
    }
}