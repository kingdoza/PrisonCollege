using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CanvasGroup))]
public sealed class StagePauseMenuPanel : MonoBehaviour, IEscapeControllable
{
    [Header("Header")]
    [SerializeField] private TMP_Text _stageTitleTmp;
    [SerializeField] private TMP_Text _waveTmp;
    [SerializeField] private Color _normalWaveColor = Color.white;
    [SerializeField] private Color _hardWaveColor = Color.red;

    [Header("Sub Panels")]
    [SerializeField] private SettingPanel _settingPanel;
    [SerializeField] private SimplePanel _restartCheckPanel;
    [SerializeField] private SimplePanel _exitCheckPanel;

    private CanvasGroup _canvasGroup;
    private bool _isActive;
    private bool _isInitialized;
    private float _originTimeScale = 1f;
    private bool _originCursorVisible;
    private CursorLockMode _originCursorLockMode;

    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        ActivateSubPanel(_settingPanel);
        ActivateSubPanel(_restartCheckPanel);
        ActivateSubPanel(_exitCheckPanel);
        SetCanvasVisible(false);
    }

    private void Start()
    {
        HideSubPanel(_settingPanel);
        HideSubPanel(_restartCheckPanel);
        HideSubPanel(_exitCheckPanel);
    }

    private void OnDisable()
    {
        RestoreRuntimeStateIfNeeded();
    }

    private void OnDestroy()
    {
        RestoreRuntimeStateIfNeeded();
    }

    public bool InitializeNormal(string stageTitle, int waveNumber, bool isHardDifficulty)
    {
        if (!ValidateReferences())
            return false;

        _stageTitleTmp.gameObject.SetActive(true);
        _stageTitleTmp.text = stageTitle ?? string.Empty;

        _waveTmp.gameObject.SetActive(true);
        _waveTmp.text = $"웨이브 {waveNumber}";
        _waveTmp.color = isHardDifficulty ? _hardWaveColor : _normalWaveColor;

        _isInitialized = true;
        SetCanvasVisible(false);
        return true;
    }

    public bool InitializeTutorial(string stageTitle)
    {
        if (!ValidateReferences())
            return false;

        _stageTitleTmp.gameObject.SetActive(true);
        _stageTitleTmp.text = stageTitle ?? string.Empty;

        _waveTmp.text = string.Empty;
        _waveTmp.gameObject.SetActive(false);

        _isInitialized = true;
        SetCanvasVisible(false);
        return true;
    }

    public void Show()
    {
        if (_isActive)
            return;

        if (!_isInitialized)
        {
            Debug.LogError("StagePauseMenuPanel이 초기화되기 전에 열렸습니다.", this);
            return;
        }

        _isActive = true;
        _originTimeScale = Time.timeScale;
        _originCursorVisible = Cursor.visible;
        _originCursorLockMode = Cursor.lockState;

        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SetCanvasVisible(true);
    }

    public void Hide()
    {
        if (!_isActive)
        {
            SetCanvasVisible(false);
            return;
        }

        SetCanvasVisible(false);
        RestoreRuntimeStateIfNeeded();
    }

    public void Resume_Btn()
    {
        EscapeInputSystem.Instance.DisablePanel(this);
    }

    public void Restart_Btn()
    {
        if (_restartCheckPanel != null)
            EscapeInputSystem.Instance.EnablePanel(_restartCheckPanel);
    }

    public void Settings_Btn()
    {
        if (_settingPanel != null)
            EscapeInputSystem.Instance.EnablePanel(_settingPanel);
    }

    public void Exit_Btn()
    {
        if (_exitCheckPanel != null)
            EscapeInputSystem.Instance.EnablePanel(_exitCheckPanel);
    }

    public void Activate()
    {
        Show();
    }

    public void Deactivate()
    {
        Hide();
    }

    private bool ValidateReferences()
    {
        if (_canvasGroup != null
            && _stageTitleTmp != null
            && _waveTmp != null
            && _settingPanel != null
            && _restartCheckPanel != null
            && _exitCheckPanel != null)
        {
            return true;
        }

        Debug.LogError(
            "StagePauseMenuPanel의 CanvasGroup, Stage/Wave TMP 또는 하위 패널 참조가 누락됐습니다.",
            this);
        return false;
    }

    private void SetCanvasVisible(bool visible)
    {
        if (_canvasGroup == null)
            return;

        _canvasGroup.alpha = visible ? 1f : 0f;
        _canvasGroup.interactable = visible;
        _canvasGroup.blocksRaycasts = visible;
    }

    private void RestoreRuntimeStateIfNeeded()
    {
        if (!_isActive)
            return;

        _isActive = false;
        Time.timeScale = _originTimeScale;
        Cursor.visible = _originCursorVisible;
        Cursor.lockState = _originCursorLockMode;
    }

    private static void ActivateSubPanel(SettingPanel panel)
    {
        if (panel == null)
            return;

        panel.gameObject.SetActive(true);
    }

    private static void ActivateSubPanel(SimplePanel panel)
    {
        if (panel == null)
            return;

        panel.gameObject.SetActive(true);
    }

    private static void HideSubPanel(SettingPanel panel)
    {
        if (panel != null)
            panel.Hide();
    }

    private static void HideSubPanel(SimplePanel panel)
    {
        if (panel == null)
            return;

        panel.Hide();
    }
}
