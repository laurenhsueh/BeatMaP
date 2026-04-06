using UnityEngine;

public class TestMovement : MonoBehaviour
{
    [SerializeField] private float speed = 2f;
    [SerializeField] private float lookAheadDistance = 0.35f;
    [SerializeField] private float yOffset = 0f;

    private float distanceOnPath;
    private bool hasDistanceOnPath;

    private void Update()
    {
        NavigationRoute nav = NavigationRoute.Instance;
        if (nav == null || !nav.TryGetRouteWorldPoints(out Vector3[] routePoints) || routePoints.Length < 2)
        {
            return;
        }

        if (!hasDistanceOnPath)
        {
            distanceOnPath = RoutePathMath.ProjectDistance(routePoints, transform.position);
            hasDistanceOnPath = true;
        }

        distanceOnPath += Mathf.Max(0f, speed) * Time.deltaTime;

        float routeLength = RoutePathMath.GetPathLength(routePoints);
        distanceOnPath = Mathf.Min(distanceOnPath, routeLength);

        Vector3 pathPos = RoutePathMath.SampleAtDistance(routePoints, distanceOnPath);
        Vector3 aheadPos = RoutePathMath.SampleAtDistance(routePoints, distanceOnPath + Mathf.Max(lookAheadDistance, 0.05f));

        pathPos.y += yOffset;
        aheadPos.y += yOffset;
        transform.position = pathPos;

        Vector3 forward = aheadPos - pathPos;
        if (forward.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.LookRotation(forward.normalized, Vector3.up);
        }
    }

    public void ResetToCurrentRoutePosition()
    {
        hasDistanceOnPath = false;
    }
}