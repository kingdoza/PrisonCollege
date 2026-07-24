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
    [Tooltip("선택 시 세로로 커질 패널입니다. 위아래 확장은 Pivot Y를 0.5로 설정하세요.")]
    [SerializeField] private RectTransform _selectionHeightTarget;
    [SerializeField, Min(0f)] private float _selectedHeightIncrease = 24f;
    [SerializeField, Min(0f)] private float _selectionAnimationDuration = 0.15f;

    private Color _originalFillColor;
    private Color _originalStateColor;
    private float _originalFillAmount;
    private float _normalHeight;
    private float _heightFrom;
    private float _heightTo;
    private float _heightElapsed;
    private bool _heightAnimating;
    private bool _isSelected;
    private bool _hasWeaponContent;
    private bool _hasSelectionState;

    public bool ValidateReferences()
    {
        return _itemSlot != null
            && _weaponIcon != null
            && _ammunitionFill != null
            && _ammunitionStateImage != null
            && _weaponNameText != null
            && _ammunitionText != null
            && _selectionHeightTarget != null;
    }

    public void Refresh(WeaponBase weapon, bool selected, bool immediate)
    {
        if (!_hasSelectionState)
        {
            _originalFillColor = _ammunitionFill.color;
            _originalStateColor = _ammunitionStateImage.color;
            _originalFillAmount = _ammunitionFill.fillAmount;
            _normalHeight = _selectionHeightTarget.rect.height;
        }

        bool empty = weapon == null || weapon is EmptyWeapon || _itemSlot.Item == null;
        _hasWeaponContent = !empty;
        RefreshContent(weapon, empty);

        if (!_hasSelectionState || selected != _isSelected || immediate)
            SetSelected(selected, immediate || !_hasSelectionState);

        _hasSelectionState = true;
    }

    public void Shutdown()
    {
        if (!_hasSelectionState) return;

        _heightAnimating = false;
        _hasWeaponContent = false;
        SetHeight(_normalHeight);
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
            UpdateHeightAnimation();
    }

    private void RefreshContent(WeaponBase weapon, bool empty)
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

        Stat ammunition = weapon.GetComponent<Stat>();
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

    private void SetSelected(bool selected, bool immediate)
    {
        _isSelected = selected;
        _weaponNameText.gameObject.SetActive(selected && _hasWeaponContent);
        _ammunitionText.gameObject.SetActive(selected && _hasWeaponContent);

        float targetHeight = _normalHeight + (selected ? _selectedHeightIncrease : 0f);
        if (immediate || _selectionAnimationDuration <= 0f)
        {
            _heightAnimating = false;
            SetHeight(targetHeight);
            return;
        }

        _heightFrom = _selectionHeightTarget.rect.height;
        _heightTo = targetHeight;
        _heightElapsed = 0f;
        _heightAnimating = true;
    }

    private void UpdateHeightAnimation()
    {
        if (!_heightAnimating) return;

        _heightElapsed += Time.unscaledDeltaTime;
        float t = Mathf.Clamp01(_heightElapsed / _selectionAnimationDuration);
        float eased = 1f - Mathf.Pow(1f - t, 3f);
        SetHeight(Mathf.LerpUnclamped(_heightFrom, _heightTo, eased));
        if (t >= 1f)
            _heightAnimating = false;
    }

    private void SetHeight(float height)
    {
        _selectionHeightTarget.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
    }
}
