using UnityEngine;

public class Axe : Melee
{
    protected override void AttackAnimation(Animator anim, int combo)
    {
        // 這裡對應你 Animator 裡的參數
        anim.SetInteger("Combo", combo); 
        anim.SetTrigger("LAtk");
        Debug.Log($"揮動斧頭！第 {combo} 段攻擊");
    }
}
