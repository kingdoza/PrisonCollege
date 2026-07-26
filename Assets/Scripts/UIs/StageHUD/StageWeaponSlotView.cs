using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StageWeaponSlotView : MonoBehaviour
{
    [Header("Existing equipment data")]
    [Tooltip("StageController가 채우는 기존 Equip Slot")]
    [SerializeField] private ItemSlot _itemSlot;

    [Header("Visuals")]
    [SerializeField] private Image _weaponIcon;
    [Tooltip("탄약 비율을 표시하는 Filled Image")]
    [SerializeField] private Image _ammunitionFill;
    [SerializeField] private Image _ammunitionStateImage;
    [SerializeField] private TMP_Text _weaponNameText;
    [SerializeField] private TMP_Text _ammunitionText;

    [Header("Colors")]
    [SerializeField] private Color _availableFillColor = new Color(0.2f, 0.8f, 0.35f, 1f);
    [SerializeField] private Color _depletedFillColor = new Color(0.9f, 0.15f, 0.15f, 1f);
    [SerializeField] private Color _availableStateColor = new Color(0.1f, 0.55f, 0.25f, 1f);
    [SerializeField] private Color _depletedStateColor = new Color(0.55f, 0.05f, 0.05f, 1f);
    [SerializeField] private Color _depletedCurrentTextColor = Color.red;

    [Header("Selection")]
    [Tooltip("세로 Stretch 패널을 연결합니다. 선택 시 원래 Top/Bottom Offset을 기준으로 위아래가 같은 양만큼 확장됩니다.")]
    [SerializeField] private RectTransform _selectionHeightTarget;
    [SerializeField, Min(0f)] private float _selectedHeightIncrease = 24f;
    [SerializeField, Min(0f)] private float _selectionAnimationDuration = 0.15f;

    [Header("Shake Feedback")]
    [SerializeField] private UIRectShakeFeedback _depletionShake = new();

    private Color _originalFillColor;
    private Color _originalStateColor;
    private float _originalFillAmount;
    private float _normalBottomOffset;
    private float _normalTopOffset;
    private float _bottomOffsetFrom;
    private float _bottomOffsetTo;
    private float _topOffsetFrom;
    private float _topOffsetTo;
    private float _offsetAnimationElapsed;
    private bool _offsetAnimating;
    private bool _isSelected;
    private bool _hasWeaponContent;
    private bool _hasSelectionState;
    private WeaponBase _boundWeapon;
    private Stat _boundAmmunition;

    public bool ValidateReferences()
    {
        return _itemSlot != null
            && _weaponIcon != null
            && _ammunitionFill != null
            && _ammunitionStateImage != null
            && _weaponNameText != null
            && _ammunitionText != null
            && _selectionHeightTarget != null
            && _depletionShake != null
            && _depletionShake.IsValid;
    }

    public void Refresh(WeaponBase weapon, bool selected, bool immediate)
    {
        if (!_hasSelectionState)
        {
            _originalFillColor = _ammunitionFill.color;
            _originalStateColor = _ammunitionStateImage.color;
            _originalFillAmount = _ammunitionFill.fillAmount;
            _normalBottomOffset = _selectionHeightTarget.offsetMin.y;
            _normalTopOffset = _selectionHeightTarget.offsetMax.y;
            _depletionShake.Initialize();
        }

        bool empty = weapon == null || weapon is EmptyWeapon || _itemSlot.Item == null;
        _hasWeaponContent = !empty;
        Stat ammunition = empty ? null : weapon.GetComponent<Stat>();
        BindAmmunition(weapon, ammunition);
        RefreshContent(weapon, ammunition, empty);

        if (!_hasSelectionState || selected != _isSelected || immediate)
            SetSelected(selected, immediate || !_hasSelectionState);

        _hasSelectionState = true;
    }

    public void Shutdown()
    {
        BindAmmunition(null, null);
        _depletionShake?.Shutdown();
        if (!_hasSelectionState) return;

        _offsetAnimating = false;
        _hasWeaponContent = false;
        SetVerticalOffsets(_normalBottomOffset, _normalTopOffset);
        _weaponNameText.gameObject.SetActive(false);
        _ammunitionText.gameObject.SetActive(false);
        _ammunitionFill.color = _originalFillColor;
        _ammunitionStateImage.color = _originalStateColor;
        _ammunitionFill.fillAmount = _originalFillAmount;
        _hasSelectionState = false;
    }

    private void Update()
    {
        if (_hasSelectionState)
            UpdateOffsetAnimation();
    }

    private void RefreshContent(WeaponBase weapon, Stat ammunition, bool empty)
    {
        if (empty)
        {
            _weaponIcon.enabled = false;
            _weaponIcon.sprite = null;
            _weaponNameText.text = string.Empty;
            _ammunitionText.text = string.Empty;
            _weaponNameText.gameObject.SetActive(false);
            _ammunitionText.gameObject.SetActive(false);
            _ammunitionFill.color = _originalFillColor;
            _ammunitionStateImage.color = _originalStateColor;
            _ammunitionFill.fillAmount = _originalFillAmount;
            return;
        }

        _weaponIcon.sprite = _itemSlot.Item.icon;
        _weaponIcon.enabled = _weaponIcon.sprite != null;
        _weaponNameText.text = weapon.Name;

        if (ammunition == null)
        {
            _ammunitionFill.fillAmount = 1f;
            _ammunitionFill.color = _availableFillColor;
            _ammunitionStateImage.color = _availableStateColor;
            _ammunitionText.text = "-";
        }
        else
        {
            int current = Mathf.Max(0, Mathf.RoundToInt(ammunition.Current));
            int maximum = Mathf.Max(0, Mathf.RoundToInt(ammunition.Max));
            bool depleted = current <= 0;

            _ammunitionFill.fillAmount = depleted
                ? 1f
                : (ammunition.Max > 0f ? Mathf.Clamp01(ammunition.Current / ammunition.Max) : 0f);
            _ammunitionFill.color = depleted ? _depletedFillColor : _availableFillColor;
            _ammunitionStateImage.color = depleted ? _depletedStateColor : _availableStateColor;
            _ammunitionText.text = depleted
                ? $"<color=#{ColorUtility.ToHtmlStringRGBA(_depletedCurrentTextColor)}>{current}</color><size=60%>/{maximum}</size>"
                : $"{current}<size=60%>/{maximum}</size>";
        }

        _weaponNameText.gameObject.SetActive(_isSelected);
        _ammunitionText.gameObject.SetActive(_isSelected);
    }

    private void BindAmmunition(WeaponBase weapon, Stat ammunition)
    {
        if (_boundWeapon == weapon && _boundAmmunition == ammunition)
            return;

        if (_boundAmmunition != null)
            _boundAmmunition.DepletedEvent.RemoveListener(OnAmmunitionDepleted);

        _boundWeapon = weapon;
        _boundAmmunition = ammunition;
        if (_boundAmmunition != null)
            _boundAmmunition.DepletedEvent.AddListener(OnAmmunitionDepleted);
    }

    private void OnAmmunitionDepleted()
    {
        if (!_hasSelectionState
            || !_isSelected
            || !_hasWeaponContent
            || _boundWeapon == null
            || _boundAmmunition == null)
        {
            return;
        }

        _depletionShake.Play();
    }

    private void SetSelected(bool selected, bool immediate)
    {
        _isSelected = selected;
        _weaponNameText.gameObject.SetActive(selected && _hasWeaponContent);
        _ammunitionText.gameObject.SetActive(selected && _hasWeaponContent);

        float halfIncrease = selected ? _selectedHeightIncrease * 0.5f : 0f;
        float targetBottomOffset = _normalBottomOffset - halfIncrease;
        float targetTopOffset = _normalTopOffset + halfIncrease;
        if (immediate || _selectionAnimationDuration <= 0f)
        {
            _offsetAnimating = false;
            SetVerticalOffsets(targetBottomOffset, targetTopOffset);
            return;
        }

        _bottomOffsetFrom = _selectionHeightTarget.offsetMin.y;
        _bottomOffsetTo = targetBottomOffset;
        _topOffsetFrom = _selectionHeightTarget.offsetMax.y;
        _topOffsetTo = targetTopOffset;
        _offsetAnimationElapsed = 0f;
        _offsetAnimating = true;
    }

    private void UpdateOffsetAnimation()
    {
        if (!_offsetAnimating) return;

        _offsetAnimationElapsed += Time.unscaledDeltaTime;
        float t = Mathf.Clamp01(_offsetAnimationElapsed / _selectionAnimationDuration);
        float eased = 1f - Mathf.Pow(1f - t, 3f);
        SetVerticalOffsets(
            Mathf.LerpUnclamped(_bottomOffsetFrom, _bottomOffsetTo, eased),
            Mathf.LerpUnclamped(_topOffsetFrom, _topOffsetTo, eased));
        if (t >= 1f)
            _offsetAnimating = false;
    }

    private void SetVerticalOffsets(float bottom, float top)
    {
        Vector2 offsetMin = _selectionHeightTarget.offsetMin;
        Vector2 offsetMax = _selectionHeightTarget.offsetMax;
        offsetMin.y = bottom;
        offsetMax.y = top;
        _selectionHeightTarget.offsetMin = offsetMin;
        _selectionHeightTarget.offsetMax = offsetMax;
    }
}
