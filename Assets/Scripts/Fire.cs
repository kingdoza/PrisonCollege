using UnityEngine;

public class Fire : MonoBehaviour
{
    [SerializeField] private ParticleSystem _fireParticle;
    [SerializeField] private SoundData _fireSD;
    private Stat _burnDuration;
    private bool _isBurning = false;
    private SoundEmitter _emitter;



    private void Awake()
    {
        _burnDuration = GetComponent<Stat>();
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
}
