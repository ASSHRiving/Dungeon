using UnityEngine;

public class PlayerShoot : MonoBehaviour
{
    float shootDistance = 100f;
    public Transform shootOrigin;
    public Transform gun;
    public LayerMask shootMask;
    public GameObject bulletPrefab;
    Animator animator;
    private float shooting = 3f;
    private float gunRotate = 0.5f;
    private float shootTimer = 0f;
    private float gunRotateTimer = 0f;
    private Quaternion defaultRotation;
    void Start()
    {
        animator = GetComponentInChildren<Animator>();
        defaultRotation = gun.localRotation;
    }
    void Update()
    {
        //左鍵開火
        if(Input.GetMouseButtonDown(0))
        {
            shootTimer = shooting; //開火後持續持槍
            animator.SetBool("Shooting", true);
            gunRotateTimer = gunRotate;
            //從畫面中心取得目標點
            Ray cameraRay = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            RaycastHit camHit;

            Vector3 targetPoint;

            if (Physics.Raycast(cameraRay, out camHit, 100f, shootMask))
            {
                targetPoint = camHit.point;
            }
            else
            {
                targetPoint = cameraRay.GetPoint(100f);
            }

            //從角色發射 Ray（比較合理）
            Vector3 dir = (targetPoint - shootOrigin.position).normalized;
            gun.rotation = Quaternion.LookRotation(dir);
            GameObject bullet = Instantiate(bulletPrefab, shootOrigin.position, Quaternion.LookRotation(dir));
            
            Collider bulletCol = bullet.GetComponent<Collider>();
            Collider playerCol = GetComponent<Collider>();
            Physics.IgnoreCollision(bulletCol, playerCol);
            bullet.GetComponent<Bullet>().Init(dir);

            Ray playerRay = new Ray(shootOrigin.position, dir);
            RaycastHit hit;

            if (Physics.Raycast(playerRay, out hit, shootDistance))
            {
                Debug.Log("Hit: " + hit.collider.name);
                
            }
        }
        else
        {
            shootTimer -= Time.deltaTime;
            gunRotateTimer -= Time.deltaTime;

             if (gunRotateTimer <= 0)
            {
                gun.localRotation = defaultRotation;
            }
            if (shootTimer <= 0)
            {
                animator.SetBool("Shooting", false);
            }
        }
    }
}
