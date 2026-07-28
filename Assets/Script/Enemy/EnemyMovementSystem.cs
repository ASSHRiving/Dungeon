using MoveBase;
using UnityEngine;

public class EnemyMovementSystem : CharacterMovementBase
{

    private Transform spawnPoint;
    public void SetSpawnPoint(Transform point) => spawnPoint = point;
    public Transform GetSpawnPoint() => spawnPoint;
}
