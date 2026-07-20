using UnityEngine;

public class EnforceableDamageReceiver : DamageReceiver
{
    [SerializeField] private Health _defaultHealth;
    [SerializeField] private Health _enforcedHealth;
    [SerializeField] private GameObject _enforceObject;



    protected override void Awake()
    {
        bool hasToEnforce = false;
        WaveSystem.Instance?.TryRollEnforcement(out hasToEnforce);
        _health = hasToEnforce ? _enforcedHealth : _defaultHealth;
        _health.Initialize();
        _enforceObject.SetActive(hasToEnforce);
    }
}
