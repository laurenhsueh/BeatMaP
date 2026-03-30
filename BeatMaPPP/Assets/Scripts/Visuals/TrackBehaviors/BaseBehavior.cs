using UnityEngine;

public abstract class BaseBehavior : MonoBehaviour
{
    [SerializeField] private GameObject prefab;

    [SerializeField] private float minDistance = 1.5f;
    [SerializeField] private float maxDistance = 3.5f;
    [SerializeField] private float horizontalSpread = 1.5f;
    [SerializeField] private float verticalSpread = 1.5f;

    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private float fadeOutDuration = 0.5f;
    [SerializeField] private float lifetime = 10f;

    public virtual void Spawn()
    {
        if (prefab == null)
        {
            Debug.LogWarning($"{GetType().Name}: No prefab assigned.");
            return;
        }

        Vector3 spawnPos;
        bool usedPlane = false;

        if (PlaneSpawnManager.Instance != null &&
            PlaneSpawnManager.Instance.PlanesReady &&
            PlaneSpawnManager.Instance.TryGetSpawnPoint(out Vector3 planePos))
        {
            spawnPos = planePos;
            usedPlane = true;
        }
        else
        {
            spawnPos = GetCameraSpawnPoint();
        }

        Debug.Log($"{GetType().Name} spawned at {spawnPos} (usedPlane: {usedPlane})");

        GameObject spawned = Instantiate(prefab, spawnPos, Quaternion.identity);

        FadeController fader = spawned.AddComponent<FadeController>();
        fader.FadeIn(fadeInDuration);

        float destroyDelay = Mathf.Max(lifetime, fadeOutDuration);
        fader.ScheduleFadeOut(destroyDelay - fadeOutDuration, fadeOutDuration);
        Destroy(spawned, destroyDelay);
    }

    private Vector3 GetCameraSpawnPoint()
    {
        Transform cam = Camera.main.transform;
        return cam.position
            + cam.forward * Random.Range(minDistance, maxDistance)
            + cam.right   * Random.Range(-horizontalSpread, horizontalSpread)
            + cam.up      * Random.Range(-verticalSpread, verticalSpread);
    }
}



///////////////////////////////////



// OLD SCRIPT BELOW IN CASE SOMETHING MESSES UP

// // WORKING WITH FADE, SPAWNS RANDOM DISTANCE IN FRONT OF CAMERA
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
//     [SerializeField] private float lifetime = 10f;

//     public virtual void Spawn()
//     {
//         if (prefab == null)
//         {
//             Debug.LogWarning($"{GetType().Name}: No prefab assigned.");
//             return;
//         }

//         Transform cam = Camera.main.transform;

//         // Random forward distance (always in front)
//         float randomDistance = Random.Range(minDistance, maxDistance);

//         // Random spread along the camera's local X and Y axes
//         float randomX = Random.Range(-horizontalSpread, horizontalSpread);
//         float randomY = Random.Range(-verticalSpread, verticalSpread);

//         // Build spawn position in camera-local space, then convert to world space
//         Vector3 spawnPos = cam.position
//             + cam.forward * randomDistance
//             + cam.right   * randomX
//             + cam.up      * randomY;

//         GameObject spawned = Instantiate(prefab, spawnPos, Quaternion.identity);

//         FadeController fader = spawned.AddComponent<FadeController>();
//         fader.FadeIn(fadeInDuration);

//         // Destroy after full lifetime, but kick off fade-out beforehand
//         float destroyDelay = Mathf.Max(lifetime, fadeOutDuration);
//         float fadeOutStart = destroyDelay - fadeOutDuration;

//         fader.ScheduleFadeOut(fadeOutStart, fadeOutDuration);
//         Destroy(spawned, destroyDelay);

//         Debug.Log($"{GetType().Name} prefab spawned at {spawnPos}");
//     }
// }