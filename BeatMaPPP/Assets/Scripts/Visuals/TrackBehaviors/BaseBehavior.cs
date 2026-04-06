using UnityEngine;

public abstract class BaseBehavior : MonoBehaviour
{
    [SerializeField] private GameObject prefab;

    [SerializeField] protected float spawnDistance = 2f;
    [SerializeField] private bool useNavigationPath = true;
    [SerializeField] private float spawnBehindPlayerDistance = 2f;
    [SerializeField] protected float spawnHeightOffset = 0f;

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

        Vector3 spawnPos;
        Quaternion spawnRot;
        if (!TryGetNavigationSpawnPose(out spawnPos, out spawnRot))
        {
            spawnPos = GetSpawnPosition();
            spawnRot = GetSpawnRotation();
        }

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
        if (Camera.main == null)
        {
            return Quaternion.identity;
        }

        Vector3 forward = Camera.main.transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude <= 0.0001f)
        {
            forward = Vector3.forward;
        }

        return Quaternion.LookRotation(forward.normalized, Vector3.up);
    }

    private Vector3 GetCameraSpawnPoint()
    {
        if (Camera.main == null)
        {
            return transform.position;
        }

        Transform cam = Camera.main.transform;
        return cam.position + cam.forward * spawnDistance;
    }

    private bool TryGetNavigationSpawnPose(out Vector3 spawnPos, out Quaternion spawnRot)
    {
        spawnPos = default;
        spawnRot = Quaternion.identity;

        if (!useNavigationPath || Camera.main == null || NavigationRoute.Instance == null)
        {
            return false;
        }

        if (!NavigationRoute.Instance.TryGetRouteWorldPoints(out Vector3[] routePoints) || routePoints.Length < 2)
        {
            return false;
        }

        Vector3 cameraPosition = Camera.main.transform.position;
        float cameraDistanceOnPath = RoutePathMath.ProjectDistance(routePoints, cameraPosition);
        float spawnDistanceOnPath = Mathf.Max(0f, cameraDistanceOnPath - spawnBehindPlayerDistance);

        spawnPos = RoutePathMath.SampleAtDistance(routePoints, spawnDistanceOnPath);
        spawnPos.y += spawnHeightOffset;

        Vector3 forward = RoutePathMath.TangentAtDistance(routePoints, spawnDistanceOnPath);
        forward.y = 0f;
        if (forward.sqrMagnitude <= 0.0001f)
        {
            forward = Camera.main.transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude <= 0.0001f)
            {
                forward = Vector3.forward;
            }
        }

        spawnRot = Quaternion.LookRotation(forward.normalized, Vector3.up);
        return true;
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