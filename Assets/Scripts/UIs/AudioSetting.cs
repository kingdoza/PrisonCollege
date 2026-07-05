using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class AudioSetting : MonoBehaviour
{
    [Header("Audio Resources")]
    [SerializeField] private AudioMixer mainMixer; // 오디오 믹서 연결

    [Header("UI Sliders")]
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;

    [Header("UI Texts")]
    [SerializeField] private TextMeshProUGUI masterVolumeTmp;
    [SerializeField] private TextMeshProUGUI bgmVolumeTmp;
    [SerializeField] private TextMeshProUGUI sfxVolumeTmp;



    private void Start()
    {
        // 1. 데이터 불러오기
        float masterVol = PlayerPrefs.GetFloat("Master", 0.5f);
        float bgmVol = PlayerPrefs.GetFloat("BGM", 0.5f);
        float sfxVol = PlayerPrefs.GetFloat("SFX", 0.5f);

        // 2. 초기화 (슬라이더 및 믹서 적용)
        UpdateUIAndMixer("Master", masterVol, masterSlider, masterVolumeTmp);
        UpdateUIAndMixer("BGM", bgmVol, bgmSlider, bgmVolumeTmp);
        UpdateUIAndMixer("SFX", sfxVol, sfxSlider, sfxVolumeTmp);

        // 3. 이벤트 연결
        masterSlider.onValueChanged.AddListener(val => OnSliderChanged("Master", val, masterVolumeTmp));
        bgmSlider.onValueChanged.AddListener(val => OnSliderChanged("BGM", val, bgmVolumeTmp));
        sfxSlider.onValueChanged.AddListener(val => OnSliderChanged("SFX", val, sfxVolumeTmp));
    }

    // 초기 설정 시 중복 코드를 줄이기 위한 헬퍼 함수
    private void UpdateUIAndMixer(string paramName, float value, Slider slider, TextMeshProUGUI tmp)
    {
        slider.value = value;
        SetVolume(paramName, value);
        UpdateVolumeText(tmp, value);
    }

    private void OnSliderChanged(string paramName, float value, TextMeshProUGUI tmp)
    {
        SetVolume(paramName, value);
        UpdateVolumeText(tmp, value); // 텍스트 업데이트
        PlayerPrefs.SetFloat(paramName, value);
        // PlayerPrefs.Save(); // 성능을 위해 설정창을 닫을 때 따로 호출하는 것을 추천합니다.
    }

    private void UpdateVolumeText(TextMeshProUGUI tmp, float value)
    {
        if (tmp != null)
        {
            // 0~1 값을 0~100 정수로 변환 후 % 붙이기
            int percent = Mathf.RoundToInt(value * 100f);
            tmp.text = $"{percent}%";
        }
    }

    private void SetVolume(string paramName, float sliderValue)
    {
        //float volume = Mathf.Log10(Mathf.Max(0.0001f, sliderValue)) * 20;
        //mainMixer.SetFloat(paramName, volume);

        float boost = 5f;

        float volume = (Mathf.Log10(Mathf.Max(0.0001f, sliderValue)) * 20) + boost;

        // 최종 값이 믹서 허용치(+20)를 넘지 않게 클램프 (안전장치)
        mainMixer.SetFloat(paramName, Mathf.Clamp(volume, -80f, 20f));
    }



    public void ApplyVolumes()
    {
        float masterVol = PlayerPrefs.GetFloat("Master", 0.5f);
        float bgmVol = PlayerPrefs.GetFloat("BGM", 0.5f);
        float sfxVol = PlayerPrefs.GetFloat("SFX", 0.5f);

        float boost = 5f;

        float masterValue = (Mathf.Log10(Mathf.Max(0.0001f, masterVol)) * 20) + boost;
        float bgmValue = (Mathf.Log10(Mathf.Max(0.0001f, bgmVol)) * 20) + boost;
        float sfxValue = (Mathf.Log10(Mathf.Max(0.0001f, sfxVol)) * 20) + boost;

        // 최종 값이 믹서 허용치(+20)를 넘지 않게 클램프 (안전장치)
        mainMixer.SetFloat("Master", Mathf.Clamp(masterValue, -80f, 20f));
        mainMixer.SetFloat("BGM", Mathf.Clamp(bgmValue, -80f, 20f));
        mainMixer.SetFloat("SFX", Mathf.Clamp(sfxValue, -80f, 20f));
    }



    public void MuteBGM()
    {
        mainMixer.SetFloat("BGM", -80f);
    }
}
