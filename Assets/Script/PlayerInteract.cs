using UnityEngine;

public class PlayerInteract : MonoBehaviour
{
    public float interactDistance = 5f;
    public Transform interactOrigin; // 建議設在角色胸口或頭
    public LayerMask interactMask;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            // ① 從畫面中心取得目標點
            Ray cameraRay = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            RaycastHit camHit;

            Vector3 targetPoint;

            if (Physics.Raycast(cameraRay, out camHit, 100f, interactMask))
            {
                targetPoint = camHit.point;
            }
            else
            {
                targetPoint = cameraRay.GetPoint(100f);
            }

            // ② 從角色發射 Ray（比較合理）
            Vector3 dir = (targetPoint - interactOrigin.position).normalized;

            Ray playerRay = new Ray(interactOrigin.position, dir);
            RaycastHit hit;

            if (Physics.Raycast(playerRay, out hit, interactDistance))
            {
                Debug.Log("Hit: " + hit.collider.name);

                IInteractable interactable = hit.collider.GetComponent<IInteractable>();

                if (interactable != null)
                {
                    interactable.Interact();
                }
            }

            // Debug
            Debug.DrawRay(cameraRay.origin, cameraRay.direction * 100f, Color.red);
            Debug.DrawRay(interactOrigin.position, dir * interactDistance, Color.green);
            Debug.Log("Dir: " + dir);
        }
    }
}