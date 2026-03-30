// Behavior that all prefabs inherit from
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;

public abstract class BaseBehavior : MonoBehaviour
{
    [SerializeField] private GameObject prefab;

    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private float fadeOutDuration = 0.5f;
    [SerializeField] private float lifetime = 10f;
    private ARPlaneManager _planeManager;

    protected virtual void Awake()
    {
        _planeManager = FindAnyObjectByType<ARPlaneManager>();

        if (_planeManager == null)
            Debug.LogWarning($"{GetType().Name}: No ARPlaneManager found in scene.");
    }

    public virtual void Spawn()
    {
        if (prefab == null)
        {
            Debug.LogWarning($"{GetType().Name}: No prefab assigned.");
            return;
        }

        if (!TryGetSpawnPoint(out Vector3 spawnPos))
        {
            Debug.LogWarning($"{GetType().Name}: No horizontal surface found to spawn on.");
            return;
        }

        GameObject spawned = Instantiate(prefab, spawnPos, Quaternion.identity);

        FadeController fader = spawned.AddComponent<FadeController>();
        fader.FadeIn(fadeInDuration);

        // Destroy after full lifetime, but kick off fade-out beforehand
        float destroyDelay = Mathf.Max(lifetime, fadeOutDuration);
        float fadeOutStart = destroyDelay - fadeOutDuration;

        fader.ScheduleFadeOut(fadeOutStart, fadeOutDuration);
        Destroy(spawned, destroyDelay);

        Debug.Log($"{GetType().Name} prefab spawned at {spawnPos}");
    }

    // Picks a random point on a random detected horizontal plane
    private bool TryGetSpawnPoint(out Vector3 spawnPos)
    {
        spawnPos = Vector3.zero;

        // Collect all valid horizontal planes
        List<ARPlane> horizontalPlanes = new();
        foreach (ARPlane plane in _planeManager.trackables)
        {
            if (plane.alignment == PlaneAlignment.HorizontalUp)
                horizontalPlanes.Add(plane);
        }

        if (horizontalPlanes.Count == 0)
            return false;

        // Pick a random plane
        ARPlane chosen = horizontalPlanes[Random.Range(0, horizontalPlanes.Count)];

        // Pick a random point within the plane's bounds
        Vector2 halfExtents = chosen.extents * 0.5f;
        float randomX = Random.Range(-halfExtents.x, halfExtents.x);
        float randomZ = Random.Range(-halfExtents.y, halfExtents.y);

        // Plane center is in world space; offset in the plane's local X/Z
        spawnPos = chosen.transform.TransformPoint(new Vector3(randomX, 0f, randomZ));

        return true;
    }
}



// // WORKING WITH FADE
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