using System.Drawing;
using TMPro;
using UnityEngine;

public class WeaponPanel : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _nameTmp;
    [SerializeField] private TextMeshProUGUI _typeTmp;
    [SerializeField] private TextMeshProUGUI _curBulletTmp;
    [SerializeField] private TextMeshProUGUI _maxBulletTmp;
    [SerializeField] private Crosshair _crosshair;



    public void ShowInfo(WeaponBase weapon)
    {
        _nameTmp.text = weapon.Name;
        _typeTmp.text = weapon.TypeName;

        Stat weaponBullet = weapon.GetComponent<Stat>();
        if (weaponBullet == null)
        {
            _curBulletTmp.text = "-";
            //_maxBulletTmp.text = string.Empty;
        }
        else
        {
            //_curBulletTmp.text = $"{weaponBullet.Current.ToString("F0")} / {weaponBullet.Max.ToString("F0")}";
            _curBulletTmp.text = $"{weaponBullet.Current.ToString("F0")} <size=70%>/  {weaponBullet.Max.ToString("F0")}</size>";
        }

        RangedWeapon rangedWeapon = weapon as RangedWeapon;
        if (rangedWeapon != null)
        {
            _crosshair.ShowRanged(rangedWeapon.SpreadIntensity);
        }
        else
        {
            _crosshair.HideRanged();
        }
    }
}
