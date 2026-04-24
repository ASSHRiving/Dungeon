using UnityEngine;

public class Gun : Weapon
{
    [Header("手槍特有設定")]
    public Transform shootOrigin;
    public GameObject bulletPrefab;
    public float shootDistance = 100f;

    public override void Attack(Animator anim)
    {
        if (Time.time < nextAttackTime) return;
        
        //射擊動畫與狀態
        ResetCombatTimer();
        anim.SetTrigger("Attack");
        anim.SetBool("InCombat", true);
        nextAttackTime = Time.time + attackRate;

        Ray cameraRay = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit camHit;

        //生成子彈
        Vector3 targetPoint = Physics.Raycast(cameraRay, out camHit, 100f, shootMask) 
                              ? camHit.point 
                              : cameraRay.GetPoint(100f);
        Vector3 dir = (targetPoint - shootOrigin.position).normalized;
        GameObject bullet = Instantiate(bulletPrefab, shootOrigin.position, Quaternion.LookRotation(dir));
        bullet.GetComponent<Bullet>().Init(dir);
        //射線debug
        if (Physics.Raycast(shootOrigin.position, dir, out RaycastHit hit, shootDistance))
        {
            Debug.Log("Hit: " + hit.collider.name);
        }
    }
}
