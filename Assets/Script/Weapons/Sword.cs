using UnityEngine;
public class Sword : Melee
{
    protected override void ExecuteMeleeAttack(Animator anim, int combo)
    {
        // 這裡對應你 Animator 裡的參數
        anim.SetInteger("Combo", combo); 
        anim.SetTrigger("LAtk");
        Debug.Log($"揮動武士刀！第 {combo} 段攻擊");
    }
}
