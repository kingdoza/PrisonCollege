using UnityEngine;

public class WeaponRecharger : MonoBehaviour
{
    [SerializeField] private WeaponBase _targetWeapon;
    [SerializeField] private Stat _rechargeAmount;
    [SerializeField] private Stat _remainedSupplyTime;
    private Click _interaction;



    private void Awake()
    {
        _rechargeAmount.Initialize();
        _remainedSupplyTime.Initialize();

        _interaction = GetComponent<Click>();
        _interaction.ClickEvent.AddListener(RechargeWeapon);
        _interaction.ActionName = $"{_targetWeapon.Name} È¹µæ";

        _remainedSupplyTime.DepletedEvent.AddListener(RechargeSupplied);
    }



    private void Update()
    {
        UpdateUIFillAmount();
        SupplyRechargeStuff();
    }



    private void RechargeSupplied()
    {
        _remainedSupplyTime.Initialize();
        _rechargeAmount.Increase(1);
    }



    private void UpdateUIFillAmount()
    {
        if (_rechargeAmount.IsDepleted)
        {
            _interaction.FillAmount =  (1f - _remainedSupplyTime.Ratio);
        }
        else
        {
            _interaction.FillAmount = 1f;
        }
    }



    private void SupplyRechargeStuff()
    {
        if (_rechargeAmount.IsMax) return;
        _remainedSupplyTime.Decrease(Time.deltaTime);
    }



    private void RechargeWeapon()
    {
        if (_rechargeAmount.IsDepleted) return;
        //ICountableWeapon countableWeapon = _targetWeapon as ICountableWeapon;
        //if (countableWeapon == null) return;
        //_rechargeAmount.Decrease(1);
        //countableWeapon.Acquire(1);

        ThrowWeapon throwWeapon = _targetWeapon as ThrowWeapon;
        GunWeapon gunWeapon = _targetWeapon as GunWeapon;
        if (throwWeapon == null && gunWeapon == null) return;

        bool recharged = (throwWeapon?.Acquire(1) ?? false) || (gunWeapon?.Acquire(1) ?? false);
        if (recharged)
        {
            _rechargeAmount.Decrease(1);
        }
    }
}
