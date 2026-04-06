// using UnityEngine;

// public class BassTrack : BaseBehavior
// {
//     [SerializeField] private float moveSpeed = 5f;
//     [SerializeField] private float behindDistance = 2f;
//     [SerializeField] private float spawnHeightOffset = 0f;

//     public override GameObject Spawn()
//     {
//         GameObject spawned = base.Spawn();

//         if (spawned != null)
//         {
//             Transform cam = Camera.main.transform;

//             // Override position to behind the camera
//             spawned.transform.position = cam.position
//                                        - cam.forward * spawnDistance
//                                        + Vector3.up * spawnHeightOffset;

//             spawned.transform.rotation = Quaternion.LookRotation(cam.forward);

//             PrefabMove mover = spawned.AddComponent<PrefabMove>();
//             mover.speed = moveSpeed;
//             mover.SetDirection(cam.forward); // Lock direction at spawn time

//             Debug.Log("Bass visual spawned.");
//         }

//         return spawned;
//     }
// }


using UnityEngine;

public class BassTrack : BaseBehavior
{
    [SerializeField] private float moveSpeed = 0.3f;

    public override GameObject Spawn()
    {
        GameObject spawned = base.Spawn();

        if (spawned != null)
        {
            PrefabMove mover = spawned.AddComponent<PrefabMove>();
            mover.speed = moveSpeed;
            Debug.Log("Bass visual spawned.");
        }

        return spawned;
    }
}