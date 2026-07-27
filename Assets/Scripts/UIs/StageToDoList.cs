using TMPro;
using UnityEngine;

public class StageToDoList : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _mainTargetTmp;



    private void Start()
    {
        string baseString = _mainTargetTmp.text;
        int maxEscapeCount;
        switch (GameManager.Instance.CurrentStageNum)
        {
            case 1:
                maxEscapeCount = 2;
                break;
            case 2:
                maxEscapeCount = 3;
                break;
            case 3:
                maxEscapeCount = 4;
                break;
            default:
                maxEscapeCount = 4;
                Debug.LogWarning(
                    $"정의되지 않은 스테이지 {GameManager.Instance.CurrentStageNum}의 허용 탈출 인원은 4명으로 표시합니다.",
                    this);
                break;
        }

        _mainTargetTmp.text = baseString.Replace("{n}", maxEscapeCount.ToString());
    }
}
