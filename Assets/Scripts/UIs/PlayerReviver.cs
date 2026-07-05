using TMPro;
using UnityEngine;
using UnityEngine.UIElements;

public class PlayerReviver : MonoBehaviour
{

    [SerializeField] private TextMeshProUGUI _faintingTmp;
    [SerializeField] private TextMeshProUGUI _timerTmp;
    private Professor _professor;
    private Stat _timerStat;
    private bool _isPlayerDied = false;



    private void Awake()
    {
        _professor = StageController.Instance.Player;
        _timerStat = GetComponent<Stat>();
        _professor.DieEvent.AddListener(StartReviveTimer);
        _timerStat.DepletedEvent.AddListener(RevivePlayer);
    }



    private void Update()
    {
        if (!_isPlayerDied) return;
        _timerStat.Decrease(Time.deltaTime);
        _timerTmp.text = $"소생까지 {_timerStat.Current.ToString("F0")}초";
    }



    private void StartReviveTimer(string attackerName)
    {
        _timerStat.Initialize();
        _faintingTmp.text = $"{attackerName}의 하극상!\n당신은 기절했습니다.";

        _timerTmp.text = "소생까지";
        _isPlayerDied = true;
    }



    private void RevivePlayer()
    {
        _isPlayerDied = false;
        _professor.Revive();
    }
}
