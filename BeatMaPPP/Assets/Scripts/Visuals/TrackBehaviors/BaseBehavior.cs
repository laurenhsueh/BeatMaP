// Behavior that all prefabs inherit from

// WORKING WITH FADE
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

        Transform cam = Camera.main.transform;

        // Random forward distance (always in front)
        float randomDistance = Random.Range(minDistance, maxDistance);

        // Random spread along the camera's local X and Y axes
        float randomX = Random.Range(-horizontalSpread, horizontalSpread);
        float randomY = Random.Range(-verticalSpread, verticalSpread);

        // Build spawn position in camera-local space, then convert to world space
        Vector3 spawnPos = cam.position
            + cam.forward * randomDistance
            + cam.right   * randomX
            + cam.up      * randomY;

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
}



// WORKING VER
// // Spawns at random distances in front of camera
// using UnityEngine;

// public abstract class BaseBehavior : MonoBehaviour
// {
//     [SerializeField] private GameObject prefab;

//     [SerializeField] private float minDistance = 1.5f;
//     [SerializeField] private float maxDistance = 3.5f;

//     [SerializeField] private float horizontalSpread = 1.5f;
//     [SerializeField] private float verticalSpread = 1.5f;

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
//         Destroy(spawned, 10f);

//         Debug.Log($"{GetType().Name} prefab spawned at {spawnPos}");
//     }
// }