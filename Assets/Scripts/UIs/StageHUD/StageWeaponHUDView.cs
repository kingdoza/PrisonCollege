using UnityEngine;

public class StageWeaponHUDView : MonoBehaviour
{
    [Tooltip("1~4번 순서로 정확히 네 개의 슬롯을 연결합니다.")]
    [SerializeField] private StageWeaponSlotView[] _slots = new StageWeaponSlotView[4];

    private WeaponController _source;
    private bool _initialized;

    public bool Initialize(WeaponController source)
    {
        Shutdown();
        if (source == null || _slots == null || _slots.Length != 4)
        {
            Debug.LogError("StageWeaponHUDView에는 WeaponController와 정확히 4개의 Slot View가 필요합니다.", this);
            return false;
        }

        for (int i = 0; i < _slots.Length; i++)
        {
            if (_slots[i] == null || !_slots[i].ValidateReferences())
            {
                Debug.LogError($"StageWeaponHUDView 슬롯 {i + 1}의 참조가 누락됐습니다.", this);
                return false;
            }
        }

        _source = source;
        _initialized = true;
        Refresh(true);
        return true;
    }

    public void Shutdown()
    {
        if (_slots != null)
        {
            foreach (StageWeaponSlotView slot in _slots)
            {
                if (slot != null)
                    slot.Shutdown();
            }
        }

        _source = null;
        _initialized = false;
    }

    private void Update()
    {
        if (_initialized)
            Refresh(false);
    }

    private void Refresh(bool immediate)
    {
        int selectedIndex = _source.CurrentIndex;
        for (int i = 0; i < _slots.Length; i++)
        {
            WeaponBase weapon = _source.GetWeaponAt(i);
            _slots[i].Refresh(weapon, i == selectedIndex, immediate);
        }
    }
}
