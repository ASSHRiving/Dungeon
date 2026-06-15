using UnityEngine;
using System.Collections;

public class Chest : MonoBehaviour, IInteractable
{
    [SerializeField] private Transform dropPoint;
    [SerializeField] private GameObject itemPrefab;
    [Header("彈射力道")]
    [SerializeField] private float upForce;
    [SerializeField] private float forwardForce;
    private bool isOpen = false;
    private Animator animator;
    void Start()
    {
        animator = GetComponentInChildren<Animator>();
    }
    public void Interact(Transform player)
    {
        if (!isOpen)
        {
            isOpen = true;
            animator.SetBool("IsOpen", true);
            StartCoroutine(Wait());
        }
    }
    private void DropItem()
    {
        if (itemPrefab == null || dropPoint == null)return;

        GameObject itemGO = Instantiate(itemPrefab, dropPoint.position, dropPoint.rotation);
        Rigidbody rb = itemGO.GetComponentInChildren<Rigidbody>();
        if(rb != null)
        {
            float randomRightForce = Random.Range(-1.5f, 1.5f);
            Vector3 dir = (dropPoint.up * upForce) + (dropPoint.forward * forwardForce) + (dropPoint.right * randomRightForce);
            Debug.Log($" 正在對 {itemGO.name} 施加力道: {dir}，此時 isKinematic = {rb.isKinematic}");
             rb.AddForce(dir, ForceMode.Impulse);
        }
        
    }
    private IEnumerator Wait()
    {
        yield return new WaitForSeconds(0.3f);
        DropItem();
    }
}
