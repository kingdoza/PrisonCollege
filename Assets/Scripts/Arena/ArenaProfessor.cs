using UnityEngine;

public class ArenaProfessor : MonoBehaviour
{
    [SerializeField] private WeaponController _weaponController;



    private void Start()
    {
        _weaponController.EquipWeapon(0, gameObject);
        _weaponController.gameObject.SetActive(false);
    }



    private void Update()
    {
        if (Time.timeScale == 0) return;
        HandleWeaponAttack();
    }



    private void HandleWeaponAttack()
    {
        if (_weaponController.IsHiding) return;
        if (Input.GetMouseButtonDown(0) && _weaponController.gameObject.activeSelf)
        {
            if (_weaponController.TryAttack())
            {
                
            }
        }
    }


    public void EnableThrow()
    {
        //Cursor.lockState = CursorLockMode.None;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        _weaponController.gameObject.SetActive(true);
        _weaponController.Show();
    }



    public void DisaleThrow()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        //Cursor.lockState = CursorLockMode.Locked;
        _weaponController.Hide();
    }
}
