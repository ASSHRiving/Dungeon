using UnityEngine;
public class TestTrigger : MonoBehaviour {
    private void OnTriggerEnter(Collider other) {
        Debug.Log("Cube 撞到：" + other.name);
    }
}