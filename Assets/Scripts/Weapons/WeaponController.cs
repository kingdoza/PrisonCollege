using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

public class WeaponController : MonoBehaviour
{
    [SerializeField] public bool _isStage = true;
    [SerializeField] private FirstPersonController _firstPersonController;
    [SerializeField] private WeaponPanel _weaponPanel;
    public FirstPersonController FirstPersonController => _firstPersonController;

    [Header("무기 목록 (번호순)")]
    [SerializeField] private WeaponBase[] _weaponPresets;
    [SerializeField] private WeaponBase[] _weapons; 
    private int _currentIdx = 0;

    [Header("스왑 속도")]
    [SerializeField] private float _swapDuration = 0.3f;

    private bool _isSwapping = false;
    private readonly Dictionary<RangedWeapon, UnityAction> _tutorialBulletDepleteListeners = new();
    private readonly Dictionary<RangedWeapon, UnityAction> _tutorialBulletFillListeners = new();
    private WeaponItem[] _tutorialWeaponItems = Array.Empty<WeaponItem>();

    public WeaponBase CurrentWeapon => _weapons != null && _currentIdx >= 0 && _currentIdx < _weapons.Length
        ? _weapons[_currentIdx]
        : null;
    public int WeaponCount => _weapons == null ? 0 : _weapons.Length;
    public int CurrentIndex => _currentIdx;
    public WeaponBase GetWeaponAt(int index)
    {
        return _weapons != null && index >= 0 && index < _weapons.Length
            ? _weapons[index]
            : null;
    }
    public GameObject Owner { private set; get; }
    private bool isHiding = false;
    public bool IsHiding => isHiding;



    //public void EquipWeapon(int startingIndex, GameObject owner)
    //{
    //    // 시작 시 모든 무기 비활성화 후 1번 무기만 활성화
    //    Owner = owner;
    //    for (int i = 0; i < _weapons.Length; i++)
    //    {
    //        _weapons[i].gameObject.SetActive(false);
    //        _weapons[i].InfoUpdateEvent.AddListener(OnWeaponInfoUpdated);
    //    }
    //    Equip(startingIndex);
    //}


    public void EquipWeapon(int startingIndex, GameObject owner)
    {
        if (_isStage == false)
        {
            _weapons = new WeaponBase[1];
            _weapons[0] = _weaponPresets[0];
            Equip(startingIndex);
            return;
        }
        foreach (var weaponPreset in _weaponPresets)
        {
            weaponPreset.gameObject.SetActive(false);
        }

        Owner = owner;
        if (InventorySystem.Instance == null || InventorySystem.Instance.EquipedItemList == null)
        {
            _weapons = new WeaponBase[_weaponPresets.Length];
            for (int i = 0; i < _weapons.Length; i++)
            {
                _weapons[i] = _weaponPresets[i];
                RegisterWeaponListeners(_weapons[i], i);
            }
        }
        else
        {
            List<WeaponItem> invenEquipList = InventorySystem.Instance.EquipedItemList;
            _weapons = new WeaponBase[invenEquipList.Count];
            for (int i = 0; i < invenEquipList.Count; i++)
            {
                if (invenEquipList[i] == null)
                {
                    _weapons[i] = _weaponPresets[_weaponPresets.Length - 1];
                }
                else
                {
                    _weapons[i] = _weaponPresets[invenEquipList[i].inStageIndex];
                    RegisterWeaponListeners(_weapons[i], i);
                }
            }
        }

        Equip(startingIndex);
    }



    public bool InitializeTutorialLoadout(
        TutorialLoadoutEntry[] loadout,
        GameObject owner,
        int startingIndex)
    {
        if (StageController.Instance == null || !StageController.Instance.IsTutorialRuntime)
        {
            Debug.LogError("InitializeTutorialLoadout은 튜토리얼 runtime에서만 사용할 수 있습니다.", this);
            return false;
        }

        if (loadout == null || loadout.Length == 0)
        {
            Debug.LogError("튜토리얼 loadout이 비어 있습니다. Inspector에서 장비를 연결하세요.", this);
            _weapons = Array.Empty<WeaponBase>();
            _tutorialWeaponItems = Array.Empty<WeaponItem>();
            Owner = owner;
            return false;
        }

        WeaponBase[] resolvedWeapons = new WeaponBase[loadout.Length];
        for (int i = 0; i < loadout.Length; i++)
        {
            if (!TryResolveTutorialWeapon(loadout[i], out resolvedWeapons[i]))
            {
                Debug.LogError($"튜토리얼 loadout 슬롯 {i}을 런타임 무기로 해석하지 못했습니다.", this);
                return false;
            }
            if (!loadout[i].isEmptySlot
                && !loadout[i].fillToMaximum
                && loadout[i].ammunition < 0)
            {
                Debug.LogError($"튜토리얼 loadout 슬롯 {i}의 시작 탄약은 0 이상이어야 합니다.", this);
                return false;
            }
        }

        foreach (WeaponBase weaponPreset in _weaponPresets)
        {
            if (weaponPreset != null)
                weaponPreset.gameObject.SetActive(false);
        }

        Owner = owner;
        _weapons = new WeaponBase[loadout.Length];
        _tutorialWeaponItems = new WeaponItem[loadout.Length];
        for (int i = 0; i < loadout.Length; i++)
        {
            WeaponBase weapon = resolvedWeapons[i];
            _weapons[i] = weapon;
            _tutorialWeaponItems[i] = loadout[i].isEmptySlot ? null : loadout[i].weaponItem;
            RegisterWeaponListeners(weapon, i);
            if (!loadout[i].isEmptySlot)
            {
                int ammunition = loadout[i].fillToMaximum
                    ? int.MaxValue
                    : loadout[i].ammunition;
                SetWeaponAmmunition(weapon, ammunition);
            }
        }

        if (!SyncTutorialEquipSlotItems()) return false;
        SelectTutorialSlotImmediate(Mathf.Clamp(startingIndex, 0, _weapons.Length - 1));
        return true;
    }



    public bool AddTutorialWeaponToFirstEmptySlot(TutorialLoadoutEntry entry, out int slotIndex)
    {
        slotIndex = -1;
        if (StageController.Instance == null || !StageController.Instance.IsTutorialRuntime)
        {
            Debug.LogError("튜토리얼 장비 지급 API는 튜토리얼 runtime에서만 사용할 수 있습니다.", this);
            return false;
        }
        if (_weapons == null || entry.isEmptySlot) return false;
        if (_tutorialWeaponItems == null || _tutorialWeaponItems.Length != _weapons.Length)
        {
            Debug.LogError("튜토리얼 장비 UI 상태가 런타임 슬롯과 일치하지 않습니다.", this);
            return false;
        }
        if (!TryResolveTutorialWeapon(entry, out WeaponBase weapon)) return false;

        for (int i = 0; i < _weapons.Length; i++)
        {
            if (_weapons[i] != null && !(_weapons[i] is EmptyWeapon)) continue;
            if (_weapons[i] != null) _weapons[i].gameObject.SetActive(false);
            _weapons[i] = weapon;
            _tutorialWeaponItems[i] = entry.weaponItem;
            RegisterWeaponListeners(weapon, i);
            SetWeaponAmmunition(
                weapon,
                entry.fillToMaximum ? int.MaxValue : entry.ammunition);
            slotIndex = i;
            if (!SyncTutorialEquipSlotItems()) return false;
            SelectTutorialSlotImmediate(i);
            return true;
        }

        Debug.LogError("연수용 부스터를 지급할 빈 장비 슬롯이 없습니다.", this);
        return false;
    }



    public TutorialWeaponSnapshot CaptureTutorialSnapshot()
    {
        TutorialWeaponSnapshot snapshot = new TutorialWeaponSnapshot
        {
            selectedIndex = _currentIdx,
            slots = new TutorialWeaponState[WeaponCount],
        };
        for (int i = 0; i < WeaponCount; i++)
        {
            Stat ammunition = _weapons[i] != null ? _weapons[i].GetComponent<Stat>() : null;
            snapshot.slots[i] = new TutorialWeaponState
            {
                weapon = _weapons[i],
                weaponItem = i < _tutorialWeaponItems.Length ? _tutorialWeaponItems[i] : null,
                ammunition = ammunition != null ? ammunition.Current : -1f,
            };
        }
        return snapshot;
    }



    public bool RestoreTutorialSnapshot(TutorialWeaponSnapshot snapshot)
    {
        if (snapshot == null || snapshot.slots == null || snapshot.slots.Length == 0)
            return false;

        foreach (WeaponBase weaponPreset in _weaponPresets)
        {
            if (weaponPreset != null)
                weaponPreset.gameObject.SetActive(false);
        }

        _weapons = new WeaponBase[snapshot.slots.Length];
        _tutorialWeaponItems = new WeaponItem[snapshot.slots.Length];
        for (int i = 0; i < snapshot.slots.Length; i++)
        {
            WeaponBase weapon = snapshot.slots[i].weapon;
            if (weapon == null)
            {
                Debug.LogError($"튜토리얼 장비 snapshot 슬롯 {i}의 런타임 무기 참조가 없습니다.", this);
                return false;
            }
            _weapons[i] = weapon;
            _tutorialWeaponItems[i] = snapshot.slots[i].weaponItem;
            RegisterWeaponListeners(weapon, i);
            if (snapshot.slots[i].ammunition >= 0f)
                SetWeaponAmmunition(weapon, Mathf.RoundToInt(snapshot.slots[i].ammunition));
        }

        if (!SyncTutorialEquipSlotItems()) return false;
        SelectTutorialSlotImmediate(Mathf.Clamp(snapshot.selectedIndex, 0, _weapons.Length - 1));
        return true;
    }



    public bool TryResolveTutorialWeapon(TutorialLoadoutEntry entry, out WeaponBase weapon)
    {
        weapon = null;
        if (_weaponPresets == null || _weaponPresets.Length == 0)
        {
            Debug.LogError("WeaponController weapon presets가 비어 있어 튜토리얼 장비를 해석할 수 없습니다.", this);
            return false;
        }

        if (entry.isEmptySlot)
        {
            foreach (WeaponBase preset in _weaponPresets)
            {
                if (preset is EmptyWeapon)
                {
                    weapon = preset;
                    return true;
                }
            }
            Debug.LogError("WeaponController weapon presets에 EmptyWeapon이 없습니다.", this);
            return false;
        }

        if (entry.weaponItem == null)
        {
            Debug.LogError("빈 슬롯이 아닌 TutorialLoadoutEntry에는 WeaponItem asset이 필요합니다.", this);
            return false;
        }

        int presetIndex = entry.weaponItem.inStageIndex;
        if (_weaponPresets == null
            || presetIndex < 0
            || presetIndex >= _weaponPresets.Length
            || _weaponPresets[presetIndex] == null)
        {
            Debug.LogError(
                $"WeaponItem '{entry.weaponItem.name}'의 inStageIndex {presetIndex}에 대응하는 runtime weapon preset이 없습니다.",
                entry.weaponItem);
            return false;
        }

        weapon = _weaponPresets[presetIndex];
        if (weapon is EmptyWeapon)
        {
            Debug.LogError("EmptyWeapon은 WeaponItem 대신 isEmptySlot으로 명시해야 합니다.", this);
            weapon = null;
            return false;
        }
        return true;
    }



    private void AddRangedWeaponListener(WeaponBase weapon, int index)
    {
        RangedWeapon rangedWeapon = weapon as RangedWeapon;
        if (rangedWeapon == null) return;
        if (_tutorialBulletDepleteListeners.TryGetValue(rangedWeapon, out UnityAction oldDeplete))
            rangedWeapon.BulletDepleteEvent.RemoveListener(oldDeplete);
        if (_tutorialBulletFillListeners.TryGetValue(rangedWeapon, out UnityAction oldFill))
            rangedWeapon.BulletFillEvent.RemoveListener(oldFill);

        UnityAction deplete = () => OnWeaponBulletDepleted(index);
        UnityAction fill = () => OnWeaponBulletFilled(index);
        _tutorialBulletDepleteListeners[rangedWeapon] = deplete;
        _tutorialBulletFillListeners[rangedWeapon] = fill;
        rangedWeapon.BulletDepleteEvent.AddListener(deplete);
        rangedWeapon.BulletFillEvent.AddListener(fill);

    }



    private void RegisterWeaponListeners(WeaponBase weapon, int index)
    {
        if (weapon == null) return;
        weapon.InfoUpdateEvent.RemoveListener(OnWeaponInfoUpdated);
        weapon.InfoUpdateEvent.AddListener(OnWeaponInfoUpdated);
        AddRangedWeaponListener(weapon, index);
    }



    private static void SetWeaponAmmunition(WeaponBase weapon, int amount)
    {
        if (weapon == null) return;
        Stat ammunition = weapon.GetComponent<Stat>();
        if (ammunition == null) return;
        ammunition.Initialize(true);
        ammunition.Increase(Mathf.Clamp(amount, 0, Mathf.RoundToInt(ammunition.Max)));
        weapon.InfoUpdateEvent?.Invoke(weapon);
    }



    private bool SyncTutorialEquipSlotItems()
    {
        if (StageController.Instance == null || !StageController.Instance.IsTutorialRuntime)
            return false;
        return StageController.Instance.SetTutorialEquipSlotItems(_tutorialWeaponItems);
    }



    private void SelectTutorialSlotImmediate(int index)
    {
        StopAllCoroutines();
        _isSwapping = false;
        for (int i = 0; i < WeaponCount; i++)
        {
            if (_weapons[i] != null)
                _weapons[i].gameObject.SetActive(i == index);
        }
        _currentIdx = index;
        _weaponPanel?.ShowInfo(CurrentWeapon);
        if (_isStage && StageController.Instance != null)
            StageController.Instance.WeaponEquiped(index);
    }



    public void Hide()
    {
        isHiding = true;
        CurrentWeapon?.PlayHolsterAnim(_swapDuration, null);
    }



    public void Show()
    {
        isHiding = false;
        CurrentWeapon?.PlayDrawAnim(_swapDuration);
    }


    public void OnWeaponInfoUpdated(WeaponBase weapon)
    {
        if (weapon != CurrentWeapon) return;
        _weaponPanel.ShowInfo(CurrentWeapon);
    }



    private void OnWeaponBulletFilled(int index)
    {
        StageController.Instance.WeaponBulletFilled(index);
    }



    private void OnWeaponBulletDepleted(int index)
    {
        StageController.Instance.WeaponBulletDepleted(index);
    }


    

    public bool TryAttack()
    {
        if (CurrentWeapon == null) return false;
        if (_isSwapping || CurrentWeapon.IsPlayingAttackAnim || CurrentWeapon.CanAttack == false) return false;
        
        CurrentWeapon.PlayAttackAnim(); // 공격 명령
        return true;
    }


    public void ChangeWeaponByWheel(bool isNext)
    {
        if (WeaponCount == 0 || CurrentWeapon == null) return;
        // 공격 중이거나 스왑 중일 때는 입력을 무시
        if (_isSwapping || (CurrentWeapon != null && CurrentWeapon.IsPlayingAttackAnim)) return;

        int nextIdx = _currentIdx;

        if (isNext)
        {
            // 다음 무기 (마지막 무기에서 올리면 첫 번째로)
            nextIdx = (_currentIdx + 1) % _weapons.Length;
        }
        else
        {
            // 이전 무기 (첫 번째 무기에서 내리면 마지막으로)
            nextIdx = (_currentIdx - 1 + _weapons.Length) % _weapons.Length;
        }

        // 계산된 인덱스로 무기 교체 실행
        ChangeWeapon(nextIdx);
    }

    

    public void ChangeWeapon(int nextIdx)
    {
        if (nextIdx < 0 || nextIdx >= WeaponCount || CurrentWeapon == null) return;
        if (nextIdx == _currentIdx || _isSwapping || CurrentWeapon.IsPlayingAttackAnim) return;
        StageController.Instance.WeaponEquiped(nextIdx);
        StartCoroutine(SwapRoutine(nextIdx));
    }


    private void HandleInput()
    {
        if (_isSwapping) return;

        // 숫자 1, 2, 3... 키 입력 감지
        for (int i = 0; i < _weapons.Length; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                if (_currentIdx != i) StartCoroutine(SwapRoutine(i));
            }
        }
    }

    private System.Collections.IEnumerator SwapRoutine(int newIdx)
    {
        _isSwapping = true;

        // 1. 현재 무기 넣기
        bool holsterComplete = false;
        CurrentWeapon.PlayHolsterAnim(_swapDuration, () => holsterComplete = true);
        
        yield return new WaitUntil(() => holsterComplete);

        // 2. 인덱스 교체 및 새 무기 꺼내기
        _currentIdx = newIdx;
        _weaponPanel.ShowInfo(CurrentWeapon);
        CurrentWeapon.PlayDrawAnim(_swapDuration);

        yield return new WaitForSeconds(_swapDuration);
        _isSwapping = false;
    }

    private void Equip(int idx)
    {
        _currentIdx = idx;
        _weaponPanel?.ShowInfo(CurrentWeapon);
        CurrentWeapon.gameObject.SetActive(true);
        if (_isStage)
            StageController.Instance.WeaponEquiped(idx);
        // 즉시 장착은 애니메이션 없이 위치만 고정
    }



    public List<WeaponBase> GetDamageWeapons()
    {
        List<WeaponBase> damageWeapons = new List<WeaponBase>();
        foreach (var weapon in _weaponPresets)
        {
            if (weapon.EffectData is DamageData)
            {
                damageWeapons.Add(weapon);
            }
        }
        return damageWeapons;
    }



    public List<WeaponBase> GetBoostWeapons()
    {
        List<WeaponBase> boostWeapons = new List<WeaponBase>();
        foreach (var weapon in _weaponPresets)
        {
            if (weapon.EffectData is BoostData)
            {
                boostWeapons.Add(weapon);
            }
        }
        return boostWeapons;
    }
}
