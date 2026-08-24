using UnityEngine;

public class MainScreen : MonoBehaviour
{
    [Header("Showcase Build")]
    [Tooltip("활성화하면 메인씬에서 F1~F5 시연용 저장값 단축키를 사용할 수 있습니다.")]
    [SerializeField] private bool _enableShowcaseSaveHotkeys;
    [Header("Panels")]
    [SerializeField] private SimplePanel _stageSelectPanel;
    [SerializeField] private SettingPanel _settingPanel;
    [SerializeField] private SimplePanel _membersPanel;
    [SerializeField] private SimplePanel _exitCheckPanel;
    [Tooltip("튜토리얼 버튼에 부착한 최초 실행 안내 효과입니다. 연결하지 않아도 버튼 기능은 유지됩니다.")]
    [SerializeField] private TutorialButtonAttention _tutorialButtonAttention;
    [Tooltip("Start 버튼을 처음 누를 때 재생할 메인 메뉴 인트로입니다.")]
    [SerializeField] private MainMenuIntroPresenter _introPresenter;
    [Tooltip("메인 메뉴 버튼을 위에서부터 차례대로 등장시키는 Presenter입니다. 연결하지 않으면 기존처럼 즉시 조작할 수 있습니다.")]
    [SerializeField] private MainMenuButtonEntrancePresenter _buttonEntrancePresenter;

    private bool _isIntroFlowRunning;
    private StageLayout _stageLayout;


    private void Awake()
    {
        _stageSelectPanel.gameObject.SetActive(true);
        _settingPanel.gameObject.SetActive(true);
        _membersPanel.gameObject.SetActive(true);
        _exitCheckPanel.gameObject.SetActive(true);
        _stageLayout = _stageSelectPanel.GetComponentInChildren<StageLayout>(true);
    }



    private void Update()
    {
        if (!_enableShowcaseSaveHotkeys) return;

        if (Input.GetKeyDown(KeyCode.F1))
        {
            TutorialLaunchState.ResetStartedOnce();
            ActivateTutorialAttention();
        }
        else if (Input.GetKeyDown(KeyCode.F2))
        {
            IntroPlaybackState.ResetStartedOnce();
        }
        else if (Input.GetKeyDown(KeyCode.F3))
        {
            GameManager.Instance.ResetStageProgress();
            _stageLayout?.RefreshProgressUI();
        }
        else if (Input.GetKeyDown(KeyCode.F4))
        {
            GameManager.Instance.SetAllStagesCleared(DifficultyLevel.Normal);
            _stageLayout?.RefreshProgressUI();
        }
        else if (Input.GetKeyDown(KeyCode.F5))
        {
            GameManager.Instance.SetAllStagesCleared(DifficultyLevel.Hard);
            _stageLayout?.RefreshProgressUI();
        }
    }



    private void Start()
    {
        _stageSelectPanel.Hide();
        _settingPanel.Hide();
        _membersPanel.Hide();
        _exitCheckPanel.Hide();

        if (_tutorialButtonAttention != null)
            _tutorialButtonAttention.SetAttentionActive(false);

        bool entranceStarted = _buttonEntrancePresenter != null &&
            _buttonEntrancePresenter.Play(ActivateTutorialAttention);
        if (!entranceStarted)
            ActivateTutorialAttention();

        if (GameManager.Instance.hasToStageSelect == true)
        {
            GameManager.Instance.hasToStageSelect = false;
            EscapeInputSystem.Instance.EnablePanel(_stageSelectPanel);
        }
    }



    private void ActivateTutorialAttention()
    {
        bool shouldShowTutorialAttention = !TutorialLaunchState.HasStartedOnce;
        if (_tutorialButtonAttention != null)
        {
            _tutorialButtonAttention.SetAttentionActive(shouldShowTutorialAttention);
        }
        else if (shouldShowTutorialAttention)
        {
            Debug.LogWarning("최초 튜토리얼 실행 안내를 표시할 TutorialButtonAttention 참조가 없습니다.", this);
        }
    }



    public void Start_Btn()
    {
        if (_isIntroFlowRunning) return;
        if (IntroPlaybackState.HasStartedOnce)
        {
            OpenStageSelect();
            return;
        }

        if (_introPresenter == null)
        {
            Debug.LogError("MainScreen의 Intro Presenter 참조가 누락됐습니다. 인트로를 건너뛰고 스테이지 선택창을 엽니다.", this);
            OpenStageSelect();
            return;
        }

        _isIntroFlowRunning = true;
        if (!_introPresenter.Play(OnIntroCompleted))
        {
            OnIntroCompleted();
            return;
        }

        IntroPlaybackState.MarkStartedOnce();
    }



    private void OnIntroCompleted()
    {
        if (!_isIntroFlowRunning) return;

        _isIntroFlowRunning = false;
        OpenStageSelect();
    }



    private void OpenStageSelect()
    {
        EscapeInputSystem.Instance.EnablePanel(_stageSelectPanel);
    }



    public void Tutorial_Btn()
    {
        TutorialLaunchState.MarkStartedOnce();
        _tutorialButtonAttention?.SetAttentionActive(false);
        GameManager.Instance.StartTutorial();
    }



    public void Setting_Btn()
    {
        EscapeInputSystem.Instance.EnablePanel(_settingPanel);
    }



    public void Members_Btn()
    {
        EscapeInputSystem.Instance.EnablePanel(_membersPanel);
    }



    public void Exit_Btn()
    {
        //EscapeInputSystem.Instance.EnablePanel(_exitCheckPanel);
        GameManager.Instance.ExitGame();
    }
}
