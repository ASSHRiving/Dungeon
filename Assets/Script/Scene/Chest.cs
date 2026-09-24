using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class Chest : MonoBehaviour, IInteractable
{
    [SerializeField] public string interactableName = "Chest";
    string IInteractable.interactableName => interactableName;
    [SerializeField] private Transform dropPoint;
    [SerializeField] private List<GameObject> itemList;
    [SerializeField] private int gold;
    private GameObject itemPrefab;

    [Header("彈射力道")]
    [SerializeField] private float upForce;
    [SerializeField] private float forwardForce;
    private bool isOpen = false;
    private Animator animator;
    
    private void Awake()
    {
        if(itemList != null)
        {
            itemPrefab = itemList[Random.Range(0, itemList.Count)];
        }
        animator = GetComponentInChildren<Animator>();
    }
    public void Interact(Transform player)
    {
        if (!isOpen)
        {
            isOpen = true;
            animator.SetBool("IsOpen", true);
            StartCoroutine(Wait());
            foreach (Transform child in transform)
            {
                child.gameObject.layer = LayerMask.NameToLayer("UnInteractable");
            }
            gameObject.layer = LayerMask.NameToLayer("UnInteractable");
        }
    }
    private void DropItem()
    {
        for (int i = 0; i < gold; i++)
        {
            // 隨機噴散的位移
            Vector3 randomOffset = new Vector3(Random.Range(-0.5f, 0.5f), 0.5f, Random.Range(-0.5f, 0.5f));
            
            // 🎯 直接跟你的 Singleton 拿金幣！
            GameObject coin = ObjectPoolManager.Instance.GetCoin(dropPoint.position + randomOffset, Quaternion.identity);

            // (可選) 給金幣一個微小的向外爆發力
            if (coin != null && coin.TryGetComponent<Rigidbody>(out Rigidbody coinRb))
            {
                Vector3 force = new Vector3(Random.Range(-2f, 2f), 4f, Random.Range(-2f, 2f));
                coinRb.AddForce(force, ForceMode.Impulse);
            }
        }

        if (itemPrefab == null || dropPoint == null)return;

        GameObject itemGO = Instantiate(itemPrefab, dropPoint.position, dropPoint.rotation);
        Rigidbody rb = itemGO.GetComponentInChildren<Rigidbody>();
        if(rb != null)
        {
            float randomRightForce = Random.Range(-1.5f, 1.5f);
            Vector3 dir = (dropPoint.up * upForce) + (dropPoint.forward * forwardForce) + (dropPoint.right * randomRightForce);
            //Debug.Log($" 正在對 {itemGO.name} 施加力道: {dir}，此時 isKinematic = {rb.isKinematic}");
            rb.AddForce(dir, ForceMode.Impulse);
            float randomTorque = Random.Range(-0.05f, 0.05f);
            rb.AddTorque(new Vector3(randomTorque, randomTorque, randomTorque), ForceMode.Impulse);

        }
    }
    private IEnumerator Wait()
    {
        yield return new WaitForSeconds(0.2f);
        DropItem();
    }
}
