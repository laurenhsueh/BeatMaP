using System;
using System.Collections;
using UnityEngine;
#if PLATFORM_ANDROID
using UnityEngine.Android;
#endif

/// <summary>
/// Manages GPS location services for real-time position tracking.
/// Handles permission requests and provides location updates.
/// </summary>
public class LocationManager : MonoBehaviour
{
    public static LocationManager Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private float updateInterval = 1f;
    [SerializeField] private float desiredAccuracyInMeters = 5f;
    [SerializeField] private float updateDistanceInMeters = 2f;

    [Header("Debug")]
    [Tooltip("Enable to use simulated GPS. Auto-enabled in Unity Editor.")]
    [SerializeField] private bool debugMode = false;
    [Tooltip("Initial coordinates - will be updated by companion app GPS")]
    [SerializeField] private double debugLatitude = 0;
    [SerializeField] private double debugLongitude = 0;
    [SerializeField] private bool simulateMovement = false;
    [Tooltip("Walking speed in m/s (1.4 = average walking)")]
    [SerializeField] private float simulationWalkingSpeed = 1.4f;
    [Tooltip("True when we've received valid GPS from companion app")]
    [SerializeField] private bool hasReceivedGPS = false;

    public GeoLocation CurrentLocation { get; private set; }
    public bool IsLocationServiceRunning { get; private set; }
    public bool HasLocationPermission { get; private set; }
    public LocationServiceStatus ServiceStatus => Input.location.status;

    /// <summary>
    /// Compass heading from companion app (0-360 degrees, 0 = North)
    /// </summary>
    public float CompassHeading { get; private set; } = -1f;
    public bool HasCompassHeading => CompassHeading >= 0;

    /// <summary>
    /// Current speed from companion app (m/s)
    /// </summary>
    public float CurrentSpeed { get; private set; } = 0f;
    public bool IsMoving => CurrentSpeed > 0.7f;

    public event Action<GeoLocation> OnLocationUpdated;
    public event Action<string> OnLocationError;
    public event Action<float> OnCompassHeadingReceived;
#pragma warning disable CS0067
    public event Action OnPermissionGranted;
    public event Action OnPermissionDenied;
#pragma warning restore CS0067

    private Coroutine locationUpdateCoroutine;
    private bool isRequestingPermission;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        StartLocationService();
    }

    private void OnDestroy()
    {
        StopLocationService();
    }

    public void StartLocationService()
    {
        if (IsLocationServiceRunning) return;

#if UNITY_EDITOR
        if (!debugMode)
        {
            debugMode = true;
        }
#endif

        if (debugMode)
        {
            IsLocationServiceRunning = true;
            HasLocationPermission = true;

            if (debugLatitude != 0 || debugLongitude != 0)
            {
                CurrentLocation = new GeoLocation
                {
                    Latitude = debugLatitude,
                    Longitude = debugLongitude,
                    Altitude = 0,
                    Accuracy = 1f,
                    Timestamp = DateTime.UtcNow
                };
                OnLocationUpdated?.Invoke(CurrentLocation);
            }

            locationUpdateCoroutine = StartCoroutine(DebugLocationUpdateLoop());
            return;
        }

        StartCoroutine(InitializeLocationService());
    }

    private IEnumerator DebugLocationUpdateLoop()
    {
        while (IsLocationServiceRunning && debugMode)
        {
            if (!hasReceivedGPS)
            {
                yield return new WaitForSeconds(updateInterval);
                continue;
            }

            if (simulateMovement && NavigationRoute.Instance != null && NavigationRoute.Instance.CurrentRoute != null)
            {
                var nav = NavigationRoute.Instance;
                if (nav.CurrentWaypointIndex < nav.CurrentRoute.Waypoints.Count)
                {
                    var targetWaypoint = nav.CurrentRoute.Waypoints[nav.CurrentWaypointIndex];

                    double dLat = targetWaypoint.Latitude - debugLatitude;
                    double dLon = targetWaypoint.Longitude - debugLongitude;

                    float distanceToWaypoint = GetDistance(debugLatitude, debugLongitude,
                        targetWaypoint.Latitude, targetWaypoint.Longitude);

                    if (distanceToWaypoint > 2f)
                    {
                        float metersToMove = simulationWalkingSpeed * updateInterval;
                        double degreesLat = metersToMove / 111000.0;
                        double degreesLon = metersToMove / (111000.0 * Math.Cos(debugLatitude * Mathf.Deg2Rad));

                        double distance = Math.Sqrt(dLat * dLat + dLon * dLon);
                        if (distance > 0)
                        {
                            debugLatitude += (dLat / distance) * degreesLat;
                            debugLongitude += (dLon / distance) * degreesLon;
                        }
                    }
                }
            }

            CurrentLocation = new GeoLocation
            {
                Latitude = debugLatitude,
                Longitude = debugLongitude,
                Altitude = 0,
                Accuracy = 1f,
                Timestamp = DateTime.UtcNow
            };

            OnLocationUpdated?.Invoke(CurrentLocation);

            yield return new WaitForSeconds(updateInterval);
        }
    }

    public void StopLocationService()
    {
        if (locationUpdateCoroutine != null)
        {
            StopCoroutine(locationUpdateCoroutine);
            locationUpdateCoroutine = null;
        }

        if (!debugMode && Input.location.status == LocationServiceStatus.Running)
        {
            Input.location.Stop();
        }

        IsLocationServiceRunning = false;
    }

    private IEnumerator InitializeLocationService()
    {
        if (!Input.location.isEnabledByUser)
        {
            OnLocationError?.Invoke("Location services are disabled. Please enable GPS in device settings.");
            yield break;
        }

#if PLATFORM_ANDROID
        if (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
        {
            isRequestingPermission = true;
            Permission.RequestUserPermission(Permission.FineLocation);

            float timeout = 30f;
            while (isRequestingPermission && timeout > 0)
            {
                if (Permission.HasUserAuthorizedPermission(Permission.FineLocation))
                {
                    isRequestingPermission = false;
                    HasLocationPermission = true;
                    OnPermissionGranted?.Invoke();
                    break;
                }
                timeout -= Time.deltaTime;
                yield return null;
            }

            if (!HasLocationPermission)
            {
                OnPermissionDenied?.Invoke();
                OnLocationError?.Invoke("Location permission denied. Please grant location access.");
                yield break;
            }
        }
        else
        {
            HasLocationPermission = true;
        }
#else
        HasLocationPermission = true;
#endif

        Input.location.Start(desiredAccuracyInMeters, updateDistanceInMeters);

        int maxWait = 20;
        while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
        {
            yield return new WaitForSeconds(1);
            maxWait--;
        }

        if (maxWait <= 0)
        {
            OnLocationError?.Invoke("Location service initialization timed out.");
            yield break;
        }

        if (Input.location.status == LocationServiceStatus.Failed)
        {
            OnLocationError?.Invoke("Unable to determine device location.");
            yield break;
        }

        IsLocationServiceRunning = true;
        locationUpdateCoroutine = StartCoroutine(LocationUpdateLoop());
    }

    private IEnumerator LocationUpdateLoop()
    {
        while (IsLocationServiceRunning)
        {
            if (Input.location.status == LocationServiceStatus.Running)
            {
                var locationData = Input.location.lastData;

                CurrentLocation = new GeoLocation
                {
                    Latitude = locationData.latitude,
                    Longitude = locationData.longitude,
                    Altitude = locationData.altitude,
                    Accuracy = locationData.horizontalAccuracy,
                    Timestamp = DateTimeOffset.FromUnixTimeSeconds((long)locationData.timestamp).UtcDateTime
                };

                OnLocationUpdated?.Invoke(CurrentLocation);
            }

            yield return new WaitForSeconds(updateInterval);
        }
    }

    public static float GetDistance(GeoLocation from, GeoLocation to)
    {
        return GetDistance(from.Latitude, from.Longitude, to.Latitude, to.Longitude);
    }

    public static float GetDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double earthRadius = 6371000;

        double dLat = (lat2 - lat1) * Mathf.Deg2Rad;
        double dLon = (lon2 - lon1) * Mathf.Deg2Rad;

        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                   Math.Cos(lat1 * Mathf.Deg2Rad) * Math.Cos(lat2 * Mathf.Deg2Rad) *
                   Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return (float)(earthRadius * c);
    }

    public void SetDebugLocation(double latitude, double longitude)
    {
        SetDebugLocation(latitude, longitude, -1f);
    }

    public void SetDebugLocation(double latitude, double longitude, float heading, float speed = 0f)
    {
        if (!hasReceivedGPS && (latitude != 0 || longitude != 0))
        {
            hasReceivedGPS = true;
        }

        debugLatitude = latitude;
        debugLongitude = longitude;
        CurrentSpeed = speed;

        // Store compass heading if provided (0-360 = valid, -1 = no heading)
        if (heading >= 0)
        {
            bool isFirstHeading = CompassHeading < 0;
            CompassHeading = heading;
            Debug.Log($"[LocationManager] Compass heading: {heading:F1}°, Speed: {speed:F2} m/s, Moving: {IsMoving}");

            // Fire event (especially important for first heading)
            if (isFirstHeading)
            {
                OnCompassHeadingReceived?.Invoke(heading);
            }
        }

        CurrentLocation = new GeoLocation
        {
            Latitude = debugLatitude,
            Longitude = debugLongitude,
            Altitude = 0,
            Accuracy = 1f,
            Timestamp = DateTime.UtcNow
        };

        OnLocationUpdated?.Invoke(CurrentLocation);
    }

    /// <summary>
    /// Called by GPSSimulator to update location from controller input
    /// </summary>
    public void UpdateFromSimulator(GeoLocation location)
    {
        if (!hasReceivedGPS)
        {
            hasReceivedGPS = true;
        }

        debugLatitude = location.Latitude;
        debugLongitude = location.Longitude;
        CurrentLocation = location;
        OnLocationUpdated?.Invoke(CurrentLocation);
    }

    public bool HasReceivedGPS => hasReceivedGPS;

    public static float GetBearing(GeoLocation from, GeoLocation to)
    {
        double dLon = (to.Longitude - from.Longitude) * Mathf.Deg2Rad;
        double lat1 = from.Latitude * Mathf.Deg2Rad;
        double lat2 = to.Latitude * Mathf.Deg2Rad;

        double y = Math.Sin(dLon) * Math.Cos(lat2);
        double x = Math.Cos(lat1) * Math.Sin(lat2) - Math.Sin(lat1) * Math.Cos(lat2) * Math.Cos(dLon);

        double bearing = Math.Atan2(y, x) * Mathf.Rad2Deg;
        return (float)((bearing + 360) % 360);
    }
}

[Serializable]
public struct GeoLocation
{
    public double Latitude;
    public double Longitude;
    public float Altitude;
    public float Accuracy;
    public DateTime Timestamp;

    public bool IsValid => Latitude != 0 || Longitude != 0;

    public override string ToString()
    {
        return $"({Latitude:F6}, {Longitude:F6})";
    }
}
