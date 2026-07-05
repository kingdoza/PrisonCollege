using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

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

    public WeaponBase CurrentWeapon => _weapons[_currentIdx];
    public int WeaponCount => _weapons.Length;
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
                _weapons[i].InfoUpdateEvent.AddListener(OnWeaponInfoUpdated);
                AddRangedWeaponListener(_weapons[i], i);
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
                    _weapons[i].InfoUpdateEvent.AddListener(OnWeaponInfoUpdated);
                    AddRangedWeaponListener(_weapons[i], i);
                }
            }
        }

        Equip(startingIndex);
    }



    private void AddRangedWeaponListener(WeaponBase weapon, int index)
    {
        RangedWeapon rangedWeapon = weapon as RangedWeapon;
        if (rangedWeapon == null) return;
        rangedWeapon.BulletDepleteEvent.AddListener(() => OnWeaponBulletDepleted(index));
        rangedWeapon.BulletFillEvent.AddListener(() => OnWeaponBulletFilled(index));

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
        if (_isSwapping || CurrentWeapon.IsPlayingAttackAnim || CurrentWeapon.CanAttack == false) return false;
        
        CurrentWeapon.PlayAttackAnim(); // 공격 명령
        return true;
    }


    public void ChangeWeaponByWheel(bool isNext)
    {
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
