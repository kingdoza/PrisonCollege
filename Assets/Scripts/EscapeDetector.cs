using DG.Tweening;
using System.Collections;
using UnityEngine;

public class EscapeDetector : MonoBehaviour
{
    [SerializeField] public ExitSpot exitSpot;
    [SerializeField] private GameObject _offLamp;
    [SerializeField] private GameObject _onLamp;
    [SerializeField] private float _interval = 0.2f; // 깜빡이는 간격
    [SerializeField] private SoundData _warningSD;
    private bool _isAlarming = false;
    private DG.Tweening.Sequence _blinkSequence;
    private SoundEmitter _emitter;



    private void Awake()
    {
        _offLamp.SetActive(true);
        _onLamp.SetActive(false);
    }



    private void Start()
    {
        StopAlarm();
        StartCoroutine(KeepDetectingEscape());
    }



    private void StartAlarm()
    {
        if (_isAlarming == true) return;

        _isAlarming = true;
        _emitter = SoundUtils.PlayOwnedScene3DSFX(_warningSD, transform.position, false, 1, true, true);
        //_audioSource.Play();

        _blinkSequence?.Kill();

        // 초기 상태 설정
        _offLamp.SetActive(true);
        _onLamp.SetActive(false);

        // 새로운 시퀀스 생성
        _blinkSequence = DOTween.Sequence()
            .AppendCallback(() => {
                _offLamp.SetActive(false);
                _onLamp.SetActive(true);
            })
            .AppendInterval(_interval)
            .AppendCallback(() => {
                _offLamp.SetActive(true);
                _onLamp.SetActive(false);
            })
            .AppendInterval(_interval)
            .SetLoops(-1); // 무한 루프
    }



    private void StopAlarm()
    {
        if (_isAlarming == false) return;

        _isAlarming = false;
        _emitter?.StopAndReturn();
        _emitter = null;

        _blinkSequence?.Kill();
        _offLamp.SetActive(true);
        _onLamp.SetActive(false);
    }



    private IEnumerator KeepDetectingEscape()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.5f);
            if (exitSpot.Occupant == null)
            {
                StopAlarm();
                continue;
            }
            PostStudent student = exitSpot.Occupant;
            Animator animator = student.GetComponent<Animator>();
            if (animator.GetLayerWeight(Global.STRIKE_LAYER_INDEX) >= 0.5f)
            {
                StartAlarm();
            }
            else
            {
                StopAlarm();
            }
        }
    }
}
