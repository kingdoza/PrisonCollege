using UnityEngine;
using UnityEngine.UI;

public class EquipInfo : MonoBehaviour
{
    [SerializeField] private Image _borderImg;
    [SerializeField] private Image _backImg;
    [SerializeField] private Image _iconImg;
    [SerializeField] private Color _equipedColor;
    [SerializeField] private Color _bulletDepletedColor;
    private Color _originBorderColor;
    private Color _originBackColor;
    private Color _originIconColor;


    private void Awake()
    {
        _originBorderColor = _borderImg.color;
        _originBackColor = _backImg.color;
        _originIconColor = _iconImg.color;
    }



    public void Equiped()
    {
        _backImg.color = _equipedColor;
        _iconImg.color = Color.white;
    }



    public void Unequiped()
    {
        _backImg.color = _originBackColor;
        _iconImg.color = _originIconColor;
    }



    public void BulletDepleted()
    {
        _borderImg.color = _bulletDepletedColor;
    }



    public void BulletFilled()
    {
        _borderImg.color = _originBorderColor;
    }
}
