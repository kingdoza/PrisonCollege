using TMPro;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

public class StageToDoList : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _mainTargetTmp;



    private void Start()
    {
        string baseString = _mainTargetTmp.text;
        int maxEscapeCount = GameManager.Instance.CurrentStageNum == 1 ? 2 : 4;
        _mainTargetTmp.text = baseString.Replace("{n}", maxEscapeCount.ToString());
    }
}
