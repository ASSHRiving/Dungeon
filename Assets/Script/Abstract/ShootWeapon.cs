using UnityEngine;

public abstract class ShootWeapon : Weapon
{
    [Header("槍械特有設定")]
    public Transform shootOrigin;
    public GameObject bulletPrefab;
    public LayerMask shootMask;
    public float shootDistance = 100f;
     
    public override void Attack(Animator anim){
        if (Time.time < nextAttackTime) return;

        ResetCombatTimer();
        ExecuteShoot(anim);
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

        if (Physics.Raycast(shootOrigin.position, dir, out RaycastHit hit, shootDistance))
        {
            Debug.Log("Hit: " + hit.collider.name);
        }
    }

    protected abstract void ExecuteShoot(Animator anim);
}
