using UnityEngine;

public class PlayerShoot : MonoBehaviour
{
    float shootDistance = 100f;
    public Transform shootOrigin;
    public LayerMask shootMask;
    public GameObject bulletPrefab;
    void Update()
    {
        //左鍵開火
        if(Input.GetMouseButtonDown(0))
        {
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
            GameObject bullet = Instantiate(bulletPrefab, shootOrigin.position, Quaternion.LookRotation(dir));
            bullet.GetComponent<Bullet>().Init(dir);
            Collider bulletCollider = bullet.GetComponent<bulletCollider>();
            Collider playerCollider = GetComponent<bulletCollider>();
            Physics.ignoreCollision(bulletCollider, playerCollider);

            Ray playerRay = new Ray(shootOrigin.position, dir);
            RaycastHit hit;

            if (Physics.Raycast(playerRay, out hit, shootDistance))
            {
                Debug.Log("Hit: " + hit.collider.name);
                
            }

            //Debug
            Debug.DrawRay(cameraRay.origin, cameraRay.direction * shootDistance, Color.red);
            Debug.DrawRay(shootOrigin.position, dir * shootDistance, Color.green);
        }
    }
}
