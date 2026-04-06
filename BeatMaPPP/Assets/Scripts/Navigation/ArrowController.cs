using UnityEngine;
using TMPro;

public class ArrowController : MonoBehaviour
{
    public Transform userForwardReference;
    [SerializeField] private float modelYawOffsetDegrees = 90f;
    [SerializeField] private TMP_Text outputText;
    void LateUpdate()
    {
        NavigationRoute navigationRoute = NavigationRoute.Instance;
        LocationManager locationManager = LocationManager.Instance;

        bool hasWaypoint = navigationRoute != null
            && locationManager != null
            && navigationRoute.HasRoute
            && navigationRoute.CurrentRoute != null
            && locationManager.CurrentLocation.IsValid
            && locationManager.HasCompassHeading
            && navigationRoute.CurrentWaypointIndex < navigationRoute.CurrentRoute.Waypoints.Count;

        float desiredYaw;

        if (hasWaypoint)
        {
            GeoLocation currentLocation = locationManager.CurrentLocation;
            GeoLocation currentWaypoint = navigationRoute.CurrentRoute.Waypoints[navigationRoute.CurrentWaypointIndex];

            float targetBearing = LocationManager.GetBearing(currentLocation, currentWaypoint);
            float relativeYaw = Mathf.DeltaAngle(locationManager.CompassHeading, targetBearing);

            Transform reference = userForwardReference != null ? userForwardReference : Camera.main != null ? Camera.main.transform : null;
            if (reference == null)
                return;

            Vector3 flatForward = reference.forward;
            flatForward.y = 0f;
            if (flatForward.sqrMagnitude <= 0.0001f)
                return;

            Quaternion baseRotation = Quaternion.LookRotation(flatForward.normalized, Vector3.up);
            desiredYaw = (baseRotation * Quaternion.Euler(0f, relativeYaw, 0f)).eulerAngles.y;
        }
        else
        {
            outputText.text = "No valid waypoint or location data.";
            return;
        }

        Quaternion desiredWorldRotation = Quaternion.Euler(0f, desiredYaw + modelYawOffsetDegrees, 0f);

        // Keep arrow flat and prevent parent hand rotation from twisting it.
        if (transform.parent != null)
            transform.localRotation = Quaternion.Inverse(transform.parent.rotation) * desiredWorldRotation;
        else
            transform.rotation = desiredWorldRotation;
    }
}