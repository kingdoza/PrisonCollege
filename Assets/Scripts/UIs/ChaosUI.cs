using UnityEngine;

public class ChaosUI : MonoBehaviour
{
    [Header("Warning Popup")]
    [SerializeField] private Transform _waringParent;
    [SerializeField] private GameObject _waringPrefab;
    [SerializeField] private Vector3 _spawnPosition;
    [SerializeField] private float _velocity;
    [SerializeField] private float _duration;
    [Header("Sound Datas")]
    [SerializeField] private SoundData _chaosSD;
    [SerializeField] private SoundData _hackSD;
    [SerializeField] private SoundData _hackBlockSD;
    [SerializeField] private SoundData _moneySD;



    public void SpawnWarningPanel(Info info)
    {
        _waringPrefab.SetActive(true);
        GameObject warningPanelObj = Instantiate(_waringPrefab, _waringParent);
        warningPanelObj.SetActive(false);
        warningPanelObj.GetComponent<RectTransform>().anchoredPosition = _spawnPosition;
        ChaosWarning warningPanel = warningPanelObj.GetComponent<ChaosWarning>();
        warningPanel.Play(info, _velocity, _duration);

        SoundData targetSD = null;
        if (info is ChaosInfo)
        {
            targetSD = _chaosSD;
        }
        else if (info is HackInfo)
        {
            targetSD = _hackSD;
        }
        else if (info is HackBlockInfo)
        {
            targetSD = _hackBlockSD;
        }
        else if (info is MoneyInfo)
        {
            targetSD = _moneySD;
        }
        SoundUtils.PlayUISFX(targetSD);
    }



    public bool TryPlayHackSoundOnly()
    {
        if (_hackSD == null)
        {
            Debug.LogError("ChaosUI Hack SoundData가 연결되지 않아 정전음을 재생할 수 없습니다.", this);
            return false;
        }

        SoundUtils.PlayUISFX(_hackSD);
        return true;
    }
}
