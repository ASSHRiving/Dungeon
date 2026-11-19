using UnityEngine;
using UnityEngine.AI;

public class Gate : MonoBehaviour
{
    private Animator animator;
    private bool isOpen = true;
    private int openID = Animator.StringToHash("IsOpen");

    [Header("即時封門碰撞體")]
    [Tooltip("可選：手動指派門口阻擋用的 BoxCollider；留空時會在執行期自動建立並貼合門洞")]
    [SerializeField] private BoxCollider instantBlockCollider;
    [Tooltip("自動建立封門碰撞體時的最小厚度（公尺）")]
    [SerializeField, Min(0.05f)] private float blockColliderThickness = 0.2f;

    [Header("音效")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip audioClip;

    private void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        EnsureBlockCollider();
        SetBlockColliderEnabled(false);
    }

    public void CloseGate()
    {
        if (audioSource != null && audioClip != null)
        {
            audioSource.PlayOneShot(audioClip);
        }

        EnsureBlockCollider();
        SetBlockColliderEnabled(true);

        if (isOpen)
        {
            isOpen = false;
            if (animator != null)
            {
                animator.SetBool(openID, isOpen);
            }
        }
    }

    public void OpenGate()
    {
        SetBlockColliderEnabled(false);

        if (!isOpen)
        {
            isOpen = true;
            if (animator != null)
            {
                animator.SetBool(openID, isOpen);
            }
        }
    }

    private void EnsureBlockCollider()
    {
        if (instantBlockCollider != null) return;

        GameObject blockerObj = new GameObject("GateInstantBlocker");
        blockerObj.transform.SetParent(transform, false);
        blockerObj.transform.localPosition = Vector3.zero;
        blockerObj.transform.localRotation = Quaternion.identity;
        blockerObj.transform.localScale = Vector3.one;

        NavMeshObstacle obstacle = GetComponentInChildren<NavMeshObstacle>(true);
        blockerObj.layer = obstacle != null ? obstacle.gameObject.layer : 0;

        BoxCollider box = blockerObj.AddComponent<BoxCollider>();
        box.isTrigger = false;

        if (obstacle != null && obstacle.shape == NavMeshObstacleShape.Box && obstacle.size.x > 0.1f && obstacle.size.y > 0.1f)
        {
            box.center = obstacle.center;
            box.size = new Vector3(
                obstacle.size.x,
                obstacle.size.y,
                Mathf.Max(obstacle.size.z, blockColliderThickness)
            );
        }
        else
        {
            box.center = new Vector3(0f, 1.95f, 0f);
            box.size = new Vector3(3.6f, 3.9f, blockColliderThickness);
        }

        instantBlockCollider = box;
    }

    private void SetBlockColliderEnabled(bool enabled)
    {
        if (instantBlockCollider != null)
        {
            instantBlockCollider.enabled = enabled;
        }
    }
}
