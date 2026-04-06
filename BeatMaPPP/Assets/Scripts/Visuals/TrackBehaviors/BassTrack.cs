using UnityEngine;

public class BassTrack : BaseBehavior
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float behindDistance = 2f;
    [SerializeField] private float horizontalSpread = 1.5f;
    [SerializeField] private float verticalSpread = 1.5f;
    [SerializeField] private float spawnHeightOffset = 0f;

    protected override Vector3 GetSpawnPosition()
    {
        Transform cam = Camera.main.transform;
        return cam.position
            - cam.forward * behindDistance
            + cam.right   * Random.Range(-horizontalSpread, horizontalSpread)
            + cam.up      * Random.Range(-verticalSpread, verticalSpread);
    }

    protected override Quaternion GetSpawnRotation()
    {
        return Quaternion.LookRotation(Camera.main.transform.forward);
    }

    public override GameObject Spawn()
    {
        GameObject spawned = base.Spawn();

        if (spawned != null)
        {
            PrefabMove mover = spawned.AddComponent<PrefabMove>();
            mover.speed = moveSpeed;
            mover.SetDirection(Camera.main.transform.forward);

            Debug.Log("Bass visual spawned.");
        }

        return spawned;
    }
}


// using UnityEngine;

// public class BassTrack : BaseBehavior
// {
//     [SerializeField] private float moveSpeed = 0.3f;

//     public override GameObject Spawn()
//     {
//         GameObject spawned = base.Spawn();

//         if (spawned != null)
//         {
//             PrefabMove mover = spawned.AddComponent<PrefabMove>();
//             mover.speed = moveSpeed;
//             Debug.Log("Bass visual spawned.");
//         }

//         return spawned;
//     }
// }