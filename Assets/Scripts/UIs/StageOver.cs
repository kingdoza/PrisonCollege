using TMPro;
using UnityEngine;
using UnityEngine.Rendering.Universal;

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
    }



    public void ShowStageOverPanel(bool isSuccess)
    {
        _stageTitleTmp.text = isSuccess ? "<color=green>감금 성공!</color>" : "<color=red>감금 실패!</color>";
        _detailTmp.text = isSuccess ? "대학원생들의 자유 박탈에 성공하였습니다." : "대학원생들에게 자유를 허락하고 말았습니다.";
        _stageEndCanvas.alpha = 1f;
        _stageEndCanvas.interactable = true;
        _stageEndCanvas.blocksRaycasts = true;
    }



    public void ShowWaveOverPanel(int moneyEarned)
    {
        _waveTitleTmp.text = $"웨이브 {WaveSystem.Instance.CurrentWave} 완료";
        _waveDetailTmp.text = $"소득: ${moneyEarned}";
        _waveEndCanvas.alpha = 1;
        _waveEndCanvas.interactable = true;
        _waveEndCanvas.blocksRaycasts = true;
        bool hasToGoArena = WaveSystem.Instance.IsCurrentWaveEndWithArena;
        _arenaBtnObj.SetActive(hasToGoArena);
        _storeBtnObj.SetActive(!hasToGoArena);
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
