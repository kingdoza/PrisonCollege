using TMPro;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public class StageOver : MonoBehaviour
{
    [Header("Wave End Panel")]
    [SerializeField] private CanvasGroup _waveEndCanvas;
    [SerializeField] private TextMeshProUGUI _waveTitleTmp;
    [SerializeField] private TextMeshProUGUI _waveDetailTmp;
    [SerializeField] private GameObject _storeBtnObj;
    [SerializeField] private GameObject _arenaBtnObj;
    [Header("Stage End Panel")]
    [SerializeField] private CanvasGroup _stageEndCanvas;
    [SerializeField] private TextMeshProUGUI _stageTitleTmp;
    [SerializeField] private TextMeshProUGUI _detailTmp;

    [Header("Stage Result Color")]
    [Tooltip("Optional. If unassigned, stage result color changes are skipped.")]
    [SerializeField] private Image _resultColorTarget;
    [SerializeField] private Color _successColor = new Color(0.47f, 0.91f, 0.57f, 1f);
    [SerializeField] private Color _failureColor = new Color(1f, 0.41f, 0.41f, 1f);

    [Header("Result Panel Unfold")]
    [Tooltip("Optional. If unassigned, the stage result panel is shown immediately.")]
    [SerializeField] private ResultPanelUnfoldAnimator _stageEndAnimator;
    [Tooltip("Optional. If unassigned, the wave result panel is shown immediately.")]
    [SerializeField] private ResultPanelUnfoldAnimator _waveEndAnimator;



    private void Awake()
    {
        _stageEndCanvas.gameObject.SetActive(true);
        _stageEndCanvas.alpha = 0f;
        _stageEndCanvas.interactable = false;
        _stageEndCanvas.blocksRaycasts = false;

        _waveEndCanvas.gameObject.SetActive(true);
        _waveEndCanvas.alpha = 0f;
        _waveEndCanvas.interactable = false;
        _waveEndCanvas.blocksRaycasts = false;

        _stageEndAnimator?.Initialize();
        _waveEndAnimator?.Initialize();
    }



    public void ShowStageOverPanel(bool isSuccess)
    {
        ApplyResultColor(isSuccess);
        _stageTitleTmp.text = isSuccess ? "<color=green>감금 성공!</color>" : "<color=red>감금 실패!</color>";
        _detailTmp.text = isSuccess ? "대학원생들의 자유 박탈에 성공하였습니다." : "대학원생들에게 자유를 허락하고 말았습니다.";
        ShowResultCanvas(_stageEndCanvas, _stageEndAnimator);
    }



    private void ApplyResultColor(bool isSuccess)
    {
        if (_resultColorTarget == null)
            return;

        Color resultColor = isSuccess ? _successColor : _failureColor;
        resultColor.a = _resultColorTarget.color.a;
        _resultColorTarget.color = resultColor;
    }



    public void ShowWaveOverPanel(int moneyEarned)
    {
        _waveTitleTmp.text = $"웨이브 {WaveSystem.Instance.CurrentWave} 완료";
        _waveDetailTmp.text = $"소득: ${moneyEarned}";
        bool hasToGoArena = WaveSystem.Instance.IsCurrentWaveEndWithArena;
        _arenaBtnObj.SetActive(hasToGoArena);
        _storeBtnObj.SetActive(!hasToGoArena);
        ShowResultCanvas(_waveEndCanvas, _waveEndAnimator);
    }



    private static void ShowResultCanvas(
        CanvasGroup canvasGroup,
        ResultPanelUnfoldAnimator animator)
    {
        if (animator != null && animator.Play(canvasGroup))
            return;

        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }



    public void Store_Btn()
    {
        GameManager.Instance.GoStore();
    }



    public void Restart_Btn()
    {
        GameManager.Instance.Restart();
    }



    public void StageSelect_Btn()
    {
        GameManager.Instance.ShowStageSelect();
    }



    public void Arena_Btn()
    {
        GameManager.Instance.GoArena();
    }
}
