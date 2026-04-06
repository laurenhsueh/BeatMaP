using UnityEngine;

public class PrefabMove : MonoBehaviour
{
    public float speed = 5f;
    public bool isMoving = true;

    [Header("Path Follow")]
    [SerializeField] private float lookAheadDistance = 0.35f;
    [SerializeField] private float despawnAheadDistance = 16f;
    [SerializeField] private float yOffset = 0f;
    [SerializeField] private float fadeOutDuration = 0.35f;

    private float distanceOnPath;
    private bool hasDistanceOnPath;
    private bool despawnStarted;

    private void Update()
    {
        if (!isMoving) return;
        FollowNavigationPath();
    }

    private void FollowNavigationPath()
    {
        NavigationRoute nav = NavigationRoute.Instance;
        if (nav == null || !nav.TryGetRouteWorldPoints(out Vector3[] routePoints) || routePoints.Length < 2)
            return;

        if (!hasDistanceOnPath)
        {
            distanceOnPath = RoutePathMath.ProjectDistance(routePoints, transform.position);
            hasDistanceOnPath = true;
        }

        distanceOnPath += Mathf.Max(0f, speed) * Time.deltaTime;

        Vector3 pathPos  = RoutePathMath.SampleAtDistance(routePoints, distanceOnPath);
        Vector3 aheadPos = RoutePathMath.SampleAtDistance(routePoints, distanceOnPath + Mathf.Max(lookAheadDistance, 0.05f));
        pathPos.y  += yOffset;
        aheadPos.y += yOffset;

        Vector3 forward = aheadPos - pathPos;
        if (forward.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(forward.normalized, Vector3.up);
        transform.position = pathPos;

        if (Camera.main != null)
        {
            float cameraDistanceOnPath = RoutePathMath.ProjectDistance(routePoints, Camera.main.transform.position);
            if (distanceOnPath > cameraDistanceOnPath + despawnAheadDistance)
                BeginDespawn();
        }
    }

    private void BeginDespawn()
    {
        if (despawnStarted) return;
        despawnStarted = true;
        isMoving = false;

        FadeController fader = GetComponent<FadeController>();
        if (fader != null)
        {
            fader.FadeOut(fadeOutDuration);
            Destroy(gameObject, fadeOutDuration);
            return;
        }

        Destroy(gameObject);
    }
}
