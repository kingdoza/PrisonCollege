using UnityEngine;

public class MainScreen : MonoBehaviour
{
    [SerializeField] private SimplePanel _stageSelectPanel;
    [SerializeField] private SettingPanel _settingPanel;
    [SerializeField] private SimplePanel _membersPanel;
    [SerializeField] private SimplePanel _exitCheckPanel;
    [Tooltip("튜토리얼 버튼에 부착한 최초 실행 안내 효과입니다. 연결하지 않아도 버튼 기능은 유지됩니다.")]
    [SerializeField] private TutorialButtonAttention _tutorialButtonAttention;



    private void Awake()
    {
        _stageSelectPanel.gameObject.SetActive(true);
        _settingPanel.gameObject.SetActive(true);
        _membersPanel.gameObject.SetActive(true);
        _exitCheckPanel.gameObject.SetActive(true);
    }



    private void Start()
    {
        _stageSelectPanel.Hide();
        _settingPanel.Hide();
        _membersPanel.Hide();
        _exitCheckPanel.Hide();

        bool shouldShowTutorialAttention = !TutorialLaunchState.HasStartedOnce;
        if (_tutorialButtonAttention != null)
        {
            _tutorialButtonAttention.SetAttentionActive(shouldShowTutorialAttention);
        }
        else if (shouldShowTutorialAttention)
        {
            Debug.LogWarning("최초 튜토리얼 실행 안내를 표시할 TutorialButtonAttention 참조가 없습니다.", this);
        }

        if (GameManager.Instance.hasToStageSelect == true)
        {
            GameManager.Instance.hasToStageSelect = false;
            EscapeInputSystem.Instance.EnablePanel(_stageSelectPanel);
        }
    }



    public void Start_Btn()
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
