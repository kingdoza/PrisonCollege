using UnityEngine;
using UnityEngine.Events;

public abstract class RangedWeapon : WeaponBase
{
    [Header("Ranged")]
    [SerializeField] protected float _maxDistance;
    [SerializeField] private float _spreadIntensity;
    [SerializeField] protected GameObject _projectilePrefab;
    [SerializeField] protected Transform _spawnPoint;
    protected Stat _magazine;
    protected WeaponController _controller;

    public override string TypeName => "¿ø°Å¸®";
    public override bool CanAttack => base.CanAttack && !_magazine.IsDepleted;
    public float SpreadIntensity => _controller._isStage ? _spreadIntensity * AttributeSystem.Instance.ShotSpreadMod.GetFinalValue() : _spreadIntensity;

    public UnityEvent BulletDepleteEvent = new();
    public UnityEvent BulletFillEvent = new();



    protected override void Awake()
    {
        base.Awake();
        _controller = GetComponentInParent<WeaponController>();
        _magazine = GetComponent<Stat>();
        _magazine.Initialize();
    }



    private void Start()
    {
    }


    protected override void ExecuteAttack()
    {
        Vector3 viewportPoint = GetRandomViewportPoint();
        Shot(viewportPoint);
        _magazine.Decrease(1);
        CheckBullet();
        InfoUpdateEvent?.Invoke(this);
        if (_magazine.IsDepleted)
        {
            BulletDepleteEvent?.Invoke();
        }
    }



    private Vector3 GetRandomViewportPoint()
    {
        Vector2 spreadOffset = Random.insideUnitCircle * SpreadIntensity;
        Vector3 viewportPoint = new Vector3(0.5f + spreadOffset.x, 0.5f + spreadOffset.y, 0);
        return viewportPoint;
    }



    protected virtual bool Acquire(int count)
    {
        if (_magazine.IsMax) return false;
        _magazine.Increase(count);
        InfoUpdateEvent?.Invoke(this);
        return true;
    }


    public bool Fill()
    {
        int fillAmount = (int)(_magazine.Max - _magazine.Current);
        bool isAcquired = Acquire(fillAmount);
        BulletFillEvent?.Invoke();
        return isAcquired;
    }



    protected abstract void Shot(Vector3 shotDesination);

    protected virtual void CheckBullet() { }
}
