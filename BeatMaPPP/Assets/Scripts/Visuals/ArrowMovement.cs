using System.Collections;
using UnityEngine;

public class ArrowMovement : MonoBehaviour
{
    [SerializeField] private float modelPitchOffsetX = -90f;
    [SerializeField] private float speed = 5f;
    [SerializeField] private float lookAheadDistance = 0.35f;
    [SerializeField] private float despawnAheadDistance = 16f;
    [SerializeField] private float yOffset = 1.35f;
    [SerializeField] private float lateralOffset = -2.8f;
    [SerializeField] private float frequencyYOffsetSensitivity = 0.01f;
    [SerializeField] private float amplitudeYOffsetSensitivity = 2.0f;
    [SerializeField] private float yOffsetSmoothTime = 0.12f;
    [SerializeField] private float minYOffset = 0.3f;
    [SerializeField] private float maxYOffset = 3.5f;
    [SerializeField] private float fadeOutDuration = 0.35f;
    [SerializeField] private float fadeInDuration = 0.2f;
    [SerializeField] private float respawnDelay = 0f;

    private float distanceOnPath;
    private bool hasDistanceOnPath;
    private bool respawnInProgress;
    private float targetYOffset;
    private float yOffsetVelocity;

    private void Awake()
    {
        targetYOffset = yOffset;
    }

    private void Update()
    {
        if (respawnInProgress)
        {
            return;
        }

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

        float safeSmoothTime = Mathf.Max(0.01f, yOffsetSmoothTime);
        yOffset = Mathf.SmoothDamp(yOffset, targetYOffset, ref yOffsetVelocity, safeSmoothTime);

        distanceOnPath += Mathf.Max(0f, speed) * Time.deltaTime;

        float routeLength = RoutePathMath.GetPathLength(routePoints);
        distanceOnPath = Mathf.Min(distanceOnPath, routeLength);

        Vector3 pathPos = RoutePathMath.SampleAtDistance(routePoints, distanceOnPath);
        Vector3 aheadPos = RoutePathMath.SampleAtDistance(routePoints, distanceOnPath + Mathf.Max(lookAheadDistance, 0.05f));

        Vector3 laneForward = aheadPos - pathPos;
        laneForward.y = 0f;
        if (laneForward.sqrMagnitude > 0.0001f)
        {
            Vector3 laneRight = Vector3.Cross(Vector3.up, laneForward.normalized);
            pathPos += laneRight * lateralOffset;
            aheadPos += laneRight * lateralOffset;
        }

        pathPos.y += yOffset;
        aheadPos.y += yOffset;
        transform.position = pathPos;

        Vector3 forward = aheadPos - pathPos;
        if (forward.sqrMagnitude > 0.0001f)
        {
            Quaternion pathRotation = Quaternion.LookRotation(forward.normalized, Vector3.up);
            transform.rotation = pathRotation * Quaternion.Euler(modelPitchOffsetX, 0f, 0f);
        }

        if (Camera.main == null)
        {
            return;
        }

        float cameraDistanceOnPath = RoutePathMath.ProjectDistance(routePoints, Camera.main.transform.position);
        if (distanceOnPath <= cameraDistanceOnPath + despawnAheadDistance)
        {
            return;
        }

        Vector3 respawnPos = RoutePathMath.SampleAtDistance(routePoints, 0f);
        Vector3 respawnAhead = RoutePathMath.SampleAtDistance(routePoints, Mathf.Max(lookAheadDistance, 0.05f));

        Vector3 respawnForwardFlat = respawnAhead - respawnPos;
        respawnForwardFlat.y = 0f;
        if (respawnForwardFlat.sqrMagnitude > 0.0001f)
        {
            Vector3 respawnRight = Vector3.Cross(Vector3.up, respawnForwardFlat.normalized);
            respawnPos += respawnRight * lateralOffset;
            respawnAhead += respawnRight * lateralOffset;
        }

        respawnPos.y += yOffset;
        respawnAhead.y += yOffset;

        Quaternion respawnRot = transform.rotation;
        Vector3 respawnForward = respawnAhead - respawnPos;
        if (respawnForward.sqrMagnitude > 0.0001f)
        {
            Quaternion pathRotation = Quaternion.LookRotation(respawnForward.normalized, Vector3.up);
            respawnRot = pathRotation * Quaternion.Euler(modelPitchOffsetX, 0f, 0f);
        }

        StartCoroutine(FadeDespawnAndRespawn(respawnPos, respawnRot));
    }

    private IEnumerator FadeDespawnAndRespawn(Vector3 respawnPosition, Quaternion respawnRotation)
    {
        if (respawnInProgress)
        {
            yield break;
        }

        respawnInProgress = true;

        FadeController fader = GetComponent<FadeController>();
        if (fader != null)
        {
            // Build renderer cache so FadeOut always affects this object.
            fader.FadeIn(0f);

            if (fadeOutDuration > 0f)
            {
                fader.FadeOut(fadeOutDuration);
                yield return new WaitForSeconds(fadeOutDuration);
            }
        }

        if (respawnDelay > 0f)
        {
            yield return new WaitForSeconds(respawnDelay);
        }

        GameObject replacement = Instantiate(gameObject, respawnPosition, respawnRotation);
        ArrowMovement replacementMovement = replacement.GetComponent<ArrowMovement>();
        if (replacementMovement != null)
        {
            replacementMovement.distanceOnPath = 0f;
            replacementMovement.hasDistanceOnPath = true;
            replacementMovement.respawnInProgress = false;
            replacementMovement.targetYOffset = targetYOffset;
            replacementMovement.yOffsetVelocity = 0f;
        }

        FadeController replacementFader = replacement.GetComponent<FadeController>();
        if (replacementFader != null && fadeInDuration > 0f)
        {
            replacementFader.FadeIn(fadeInDuration);
        }

        Destroy(gameObject);
    }

    public void ChangeYOffset(float frequencyDelta, float amplitudeDelta)
    {
        targetYOffset += frequencyDelta * frequencyYOffsetSensitivity;
        targetYOffset += amplitudeDelta * amplitudeYOffsetSensitivity;
        targetYOffset = Mathf.Clamp(targetYOffset, minYOffset, maxYOffset);
    }

    public void ChangeYOffset(float frequencyDelta)
    {
        ChangeYOffset(frequencyDelta, 0f);
    }

    public void ResetToCurrentRoutePosition()
    {
        hasDistanceOnPath = false;
        respawnInProgress = false;
    }
}