using TMPro;
using UnityEngine;
using DG.Tweening;

public class BetResultPanel : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _titleTmp;
    [SerializeField] private TextMeshProUGUI _mainMoneyTmp;
    [SerializeField] private TextMeshProUGUI _bonusMoneyTmp;
    [SerializeField] private SoundData _moneyIncreaseSD;
    private int _currentDisplayMoney = 0;
    private SoundEmitter _emitter;
    
    
    
    public void Show(BetResult betResult, int money, int increase)
    {
        _currentDisplayMoney = money;
        _mainMoneyTmp.text = _currentDisplayMoney.ToString("N0");
        if (betResult == BetResult.Success)
        {
            _titleTmp.text = "베팅 성공!!";
            _titleTmp.color = new Color(0, 220 / 255f, 0);
            _bonusMoneyTmp.text = $"+{increase.ToString("N0")}";
            StartIncreaseAnimation(money + increase);
        }
        else if (betResult == BetResult.Failed)
        {
            _titleTmp.text = "베팅 실패!!";
            _titleTmp.color = new Color(240/255f, 40/255f, 40/255f);
            _bonusMoneyTmp.text = string.Empty;
        }
        else
        {
            _titleTmp.text = "무승부!!";
            _titleTmp.color = Color.white;
            _bonusMoneyTmp.text = string.Empty;
        }
        gameObject.SetActive(true);
    }



    private void StartIncreaseAnimation(int targetMoney, float duration = 2.5f, float delay = 2.5f)
    {
        DOTween.Kill(this);
        DOTween.To(() => _currentDisplayMoney, x => _currentDisplayMoney = x, targetMoney, duration)
            .SetTarget(this)
            .SetDelay(delay)
            .SetLink(gameObject)
            .OnStart(() =>
            {
                _bonusMoneyTmp.text = string.Empty;
                _emitter = SoundUtils.PlayOwnedScene3DSFX(_moneyIncreaseSD, Camera.main.transform.position, false, 1, true, true);
            })
            .OnUpdate(() =>
            {
                // 숫자가 변할 때마다 텍스트 갱신 (세 자리 쉼표 포함)
                _mainMoneyTmp.text = _currentDisplayMoney.ToString("N0");
            })
            .OnComplete(() =>
            {
                _emitter?.StopAndReturn();
                _emitter = null;
            })
            .SetEase(Ease.OutExpo); // 뒤로 갈수록 천천히 멈추는 효과
    }



    private void OnDisable()
    {
        DOTween.Kill(this);
        _emitter?.StopAndReturn();
    }



    private void OnDestroy()
    {
        DOTween.Kill(this);
        _emitter?.StopAndReturn();
    }
}
