using UnityEngine;

public class Fire : MonoBehaviour
{
    [SerializeField] private ParticleSystem _fireParticle;
    [SerializeField] private SoundData _fireSD;
    private Stat _burnDuration;
    private bool _isBurning = false;
    private SoundEmitter _emitter;
    private PostStudent _scriptedStudentOwner;
    public bool IsBurning => _isBurning;



    private void Awake()
    {
        _burnDuration = GetComponent<Stat>();
        _scriptedStudentOwner = GetComponentInParent<PostStudent>();
        _burnDuration.Initialize(true);
        _burnDuration.MaxReachEvent.AddListener(ActivateExtinguisher);
        FireSuppressionSystem.Instance.FireExtinguishEvent.AddListener(Extinguish);
    }



    private void Update()
    {
        if (_isBurning && !_burnDuration.IsMax)
        {
            _burnDuration.Increase(Time.deltaTime);
        }
    }



    private void ActivateExtinguisher()
    {
        // 3단계 scripted Smoke는 담배 불 연출만 유지하고 소방 작동과 침수를 발생시키지 않는다.
        if (_scriptedStudentOwner != null
            && _scriptedStudentOwner.SuppressScriptedWorldConsequences)
            return;
        Debug.Log("ActivateExtinguisher");
        FireSuppressionSystem.Instance.StartSuppression();
    }



    public void Ignite()
    {
        if (FireSuppressionSystem.Instance.FloodFillRatio > 0f)
        {
            return;
        }
        _isBurning = true;
        _emitter = SoundUtils.PlayOwnedScene3DSFX(_fireSD, transform.position, true, 1, true);
        _burnDuration.Initialize(true);
        _fireParticle.gameObject.SetActive(true);
    }



    public void Extinguish()
    {
        _isBurning = false;
        _emitter?.StopAndReturn();
        _emitter = null;
        _burnDuration.Initialize(true);
        _fireParticle.gameObject.SetActive(false);
    }



    public void SetBurningStateForTutorialSetup(bool isBurning)
    {
        if (StageController.Instance == null || !StageController.Instance.IsTutorialRuntime)
        {
            Debug.LogError($"[{name}] Fire setup API는 튜토리얼 runtime에서만 사용할 수 있습니다.", this);
            return;
        }

        if (!isBurning)
        {
            Extinguish();
            return;
        }
        if (_isBurning) return;
        _isBurning = true;
        _burnDuration.Initialize(true);
        _fireParticle.gameObject.SetActive(true);
        _emitter = SoundUtils.PlayOwnedScene3DSFX(_fireSD, transform.position, true, 1, true);
    }
}
