using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCombat : MonoBehaviour
{
    public Weapon currentWeapon;
    public Weapon nextWeapon;
    private Animator animator;
    private int weaponType = 0;


    void Start()
    {
        animator = GetComponentInChildren<Animator>();
    }

    public void OnAttack(InputAction.CallbackContext context)
    {
        if(context.started && currentWeapon != null)
        {
            currentWeapon.Attack(animator);
        }
    }
    public void OnSwitchWeapon(InputAction.CallbackContext context)
    {
        if(context.started)
        {
           changeWeapon(nextWeapon);
        }
    }

    void changeWeapon(Weapon newWeapon)
    {
        if(currentWeapon != null)
        {
            currentWeapon.gameObject.SetActive(false);
        }
        nextWeapon = currentWeapon;
        currentWeapon = newWeapon;
        if (currentWeapon != null)
        {
            currentWeapon.gameObject.SetActive(true);
        }
        weaponType = weaponType == 1? 0 : 1;
        animator.SetInteger("WeaponType", weaponType); 
    }
}
