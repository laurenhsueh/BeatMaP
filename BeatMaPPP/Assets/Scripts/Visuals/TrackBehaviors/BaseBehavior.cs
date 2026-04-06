using UnityEngine;

public abstract class BaseBehavior : MonoBehaviour
{
    [SerializeField] private GameObject prefab;

    [SerializeField] protected float spawnDistance = 2f;

    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private float fadeOutDuration = 0.5f;
    [SerializeField] private float lifetime = 5f;

    public virtual GameObject Spawn()
    {
        if (prefab == null)
        {
            Debug.LogWarning($"{GetType().Name}: No prefab assigned.");
            return null;
        }

        Vector3 spawnPos = GetSpawnPosition();
        Quaternion spawnRot = GetSpawnRotation();

        Debug.Log($"{GetType().Name} spawned at {spawnPos}");

        GameObject spawned = Instantiate(prefab, spawnPos, spawnRot);

        FadeController fader = spawned.AddComponent<FadeController>();
        fader.FadeIn(fadeInDuration);

        float destroyDelay = Mathf.Max(lifetime, fadeOutDuration);
        fader.ScheduleFadeOut(destroyDelay - fadeOutDuration, fadeOutDuration);
        Destroy(spawned, destroyDelay);

        return spawned;
    }

    protected virtual Vector3 GetSpawnPosition()
    {
        if (PlaneSpawnManager.Instance != null &&
            PlaneSpawnManager.Instance.PlanesReady &&
            PlaneSpawnManager.Instance.TryGetSpawnPoint(out Vector3 planePos))
        {
            return planePos;
        }

        return GetCameraSpawnPoint();
    }

    protected virtual Quaternion GetSpawnRotation()
    {
        return Quaternion.LookRotation(Camera.main.transform.forward);
    }

    private Vector3 GetCameraSpawnPoint()
    {
        Transform cam = Camera.main.transform;
        return cam.position + cam.forward * spawnDistance;
    }
}


// using UnityEngine;

// public abstract class BaseBehavior : MonoBehaviour
// {
//     [SerializeField] private GameObject prefab;

//     [SerializeField] private float minDistance = 1.5f;
//     [SerializeField] private float maxDistance = 3.5f;
//     [SerializeField] private float horizontalSpread = 1.5f;
//     [SerializeField] private float verticalSpread = 1.5f;

//     [SerializeField] private float fadeInDuration = 0.5f;
//     [SerializeField] private float fadeOutDuration = 0.5f;
//     [SerializeField] private float lifetime = 5f;

//     public virtual GameObject Spawn()
//     {
//         if (prefab == null)
//         {
//             Debug.LogWarning($"{GetType().Name}: No prefab assigned.");
//             return null;
//         }

//         Vector3 spawnPos;
//         bool usedPlane = false;

//         if (PlaneSpawnManager.Instance != null &&
//             PlaneSpawnManager.Instance.PlanesReady &&
//             PlaneSpawnManager.Instance.TryGetSpawnPoint(out Vector3 planePos))
//         {
//             spawnPos = planePos;
//             usedPlane = true;
//         }
//         else
//         {
//             spawnPos = GetCameraSpawnPoint();
//         }

//         Debug.Log($"{GetType().Name} spawned at {spawnPos} (usedPlane: {usedPlane})");

//         Quaternion randomYRot = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
//         GameObject spawned = Instantiate(prefab, spawnPos, randomYRot);

//         FadeController fader = spawned.AddComponent<FadeController>();
//         fader.FadeIn(fadeInDuration);

//         float destroyDelay = Mathf.Max(lifetime, fadeOutDuration);
//         fader.ScheduleFadeOut(destroyDelay - fadeOutDuration, fadeOutDuration);
//         Destroy(spawned, destroyDelay);

//         return spawned;
//     }

//     private Vector3 GetCameraSpawnPoint()
//     {
//         Transform cam = Camera.main.transform;
//         return cam.position
//             + cam.forward * Random.Range(minDistance, maxDistance)
//             + cam.right   * Random.Range(-horizontalSpread, horizontalSpread)
//             + cam.up      * Random.Range(-verticalSpread, verticalSpread);
//     }
// }