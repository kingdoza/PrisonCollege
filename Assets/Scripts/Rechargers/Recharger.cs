using System.Collections.Generic;
using UnityEngine;

public abstract class Recharger : MonoBehaviour
{
    [SerializeField] protected WeaponController _weaponCtrl;
    [SerializeField] private SoundData _rechargeSD;
    private List<WeaponBase> _targetWeapons;
    private Click _interaction;
    private Stat _supplyProgress;
    private bool _canRecharge = false;
    private bool _isPreparing = true;

    private AttributeModifier _attributeModifier;
    private bool _tutorialConfigured;
    private int _tutorialSessionCost;



    private void Awake()
    {
        _interaction = GetComponent<Click>();
        _supplyProgress = GetComponent<Stat>();
        _interaction.ActionName = GetActionName();
        _targetWeapons = GetTargetWeapons();
        _supplyProgress.Initialize(true);
        _supplyProgress.MaxReachEvent.AddListener(() => _canRecharge = true);
        StageController.Instance.StageStartEvent.AddListener(() => _isPreparing = false);
        _interaction.ClickEvent.AddListener(RechargeWeapons);
        _attributeModifier = AttributeSystem.Instance.WeaponSupplySpeedMod;
    }



    private void Update()
    {
        if (!_canRecharge && !_isPreparing)
        {
            _supplyProgress.Increase(Time.deltaTime * _attributeModifier.GetFinalValue(1));
        }
        _interaction.FillAmount = _supplyProgress.Ratio;
    }


    protected abstract string GetActionName();
    protected abstract List<WeaponBase> GetTargetWeapons();



    private void RechargeWeapons()
    {
        if (!_canRecharge) return;
        if (_tutorialConfigured
            && StageController.Instance.SessionMoney < _tutorialSessionCost)
            return;
        bool recharged = false;
        foreach (var weapon in _targetWeapons)
        {
            //GunWeapon gunWeapon = weapon as GunWeapon;
            //ThrowWeapon throwWeapon = weapon as ThrowWeapon;
            //recharged |= (throwWeapon?.Fill() ?? false) || (gunWeapon?.Fill() ?? false);
            RangedWeapon rangedWeapon = weapon as RangedWeapon;
            if (rangedWeapon == null) continue;
            recharged |= rangedWeapon?.Fill() ?? false;

        }

        if (recharged)
        {
            if (_tutorialConfigured
                && !StageController.Instance.TrySpendTutorialSessionMoney(_tutorialSessionCost))
                return;
            _canRecharge = false;
            _supplyProgress.Initialize(true);
            SoundUtils.PlayScene2DSFX(_rechargeSD);
        }
    }



    public void ConfigureTutorialRuntime(bool isEnabled, int sessionCost)
    {
        if (StageController.Instance == null || !StageController.Instance.IsTutorialRuntime)
        {
            Debug.LogError("Recharger 튜토리얼 설정 API는 튜토리얼 runtime에서만 사용할 수 있습니다.", this);
            return;
        }

        _tutorialConfigured = true;
        _tutorialSessionCost = Mathf.Max(0, sessionCost);
        _interaction.InteractState = isEnabled;
        _isPreparing = !isEnabled;
    }



    public TutorialRechargerState CaptureTutorialState()
    {
        return new TutorialRechargerState
        {
            recharger = this,
            canRecharge = _canRecharge,
            isPreparing = _isPreparing,
            interactable = _interaction.InteractState,
            supplyProgress = _supplyProgress.Current,
        };
    }



    public void RestoreTutorialState(TutorialRechargerState state)
    {
        if (StageController.Instance == null || !StageController.Instance.IsTutorialRuntime) return;
        _canRecharge = state.canRecharge;
        _isPreparing = state.isPreparing;
        _interaction.InteractState = state.interactable;
        _supplyProgress.Initialize(true);
        _supplyProgress.Increase(Mathf.Clamp(state.supplyProgress, 0f, _supplyProgress.Max));
    }
}
