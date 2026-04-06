using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

public class NavigationRoute : MonoBehaviour
{
    public static NavigationRoute Instance { get; private set; }

    [Header("References")]
    [SerializeField] private APIConfig apiConfig;

    [Header("Output")]
    [SerializeField] private TMP_Text outputText;
    [SerializeField] private TMP_Text outputText2;

    [Header("Route Line")]
    [SerializeField] private LineRenderer routeLineRenderer;
    [SerializeField] private Transform routeLineAnchor;
    [SerializeField] private bool autoCreateRouteLineRenderer = true;
    [SerializeField] private float routeLineWidth = 0.2f;
    [SerializeField] private float routeLineHeightOffset = 0.05f;

    [Header("Route")]
    [SerializeField] private string destinationAddress = "";
    [SerializeField] private string travelMode = "walking";
    [SerializeField] private float waypointReachedMeters = 6f;
    [SerializeField] private bool autoRequestOnStart = true;
    [SerializeField] private float compassHeadingOffsetDegrees = 0f;

    [Header("Geo Map")]
    [Tooltip("How often (seconds) to resync GPS-to-world origin to correct for drift. 0 = never recalibrate after init.")]
    [SerializeField] private float mapRecalibrationIntervalSeconds = 5f;
    [Tooltip("Enable heading correction during map recalibration.")]
    [SerializeField] private bool enableHeadingCorrection = true;
    [Tooltip("Ignore tiny heading changes below this threshold (degrees).")]
    [SerializeField] private float headingCorrectionMinDeltaDegrees = 2f;
    [Tooltip("How strongly to apply heading correction (0..1).")]
    [SerializeField] [Range(0f, 1f)] private float headingCorrectionStrength = 0.35f;
    [Tooltip("Maximum heading correction applied at each recalibration (degrees).")]
    [SerializeField] private float maxHeadingCorrectionPerUpdateDegrees = 8f;

    private const string DIRECTIONS_API_URL = "https://maps.googleapis.com/maps/api/directions/json";

    public class RouteData
    {
        public List<GeoLocation> Waypoints = new List<GeoLocation>();
    }

    public RouteData CurrentRoute { get; private set; }
    public int CurrentWaypointIndex => nextWaypointIndex;
    public bool HasRoute => waypoints.Count > 0;

    public bool TryGetRouteWorldPoints(out Vector3[] points)
    {
        points = Array.Empty<Vector3>();

        if (!EnsureRouteLineRenderer())
        {
            return false;
        }

        int count = routeLineRenderer.positionCount;
        if (count < 2)
        {
            return false;
        }

        points = new Vector3[count];
        routeLineRenderer.GetPositions(points);
        return true;
    }

    public bool TryGetActiveWaypoint(out GeoLocation waypoint)
    {
        waypoint = default;

        if (waypoints.Count == 0 || nextWaypointIndex >= waypoints.Count)
        {
            return false;
        }

        if (LocationManager.Instance != null)
        {
            GeoLocation userLocation = LocationManager.Instance.CurrentLocation;
            if (userLocation.IsValid)
            {
                AdvanceWaypointIndexForPosition(userLocation.Latitude, userLocation.Longitude);
            }
        }

        if (nextWaypointIndex >= waypoints.Count)
        {
            return false;
        }

        waypoint = waypoints[nextWaypointIndex];
        return true;
    }

    // Route data
    private readonly List<GeoLocation> waypoints = new();
    private readonly List<string> waypointInstructions = new();
    private readonly List<string> waypointDistanceTexts = new();
    private int nextWaypointIndex;
    private bool routeRequestInProgress;
    private bool pendingRouteRequestAfterGps;
    private bool subscribedToLocationUpdates;

    // GeoMap: a live GPS-to-Unity-world coordinate system.
    // mapNorthDir / mapEastDir are initialized from compass and can be refined by heading correction.
    // mapOrigin* (the GPS↔world position pair) recalibrates every few seconds to fix drift.
    private bool mapIsReady;
    private double mapOriginLat;
    private double mapOriginLon;
    private Vector3 mapOriginWorldPos;
    private Vector3 mapNorthDir = Vector3.forward;
    private Vector3 mapEastDir  = Vector3.right;
    private float lastMapRecalibrationTime;
    private float lastAppliedHeading;
    private bool hasAppliedHeading;

    // Latest GPS snapshot stored on every location event
    private double latestGpsLat;
    private double latestGpsLon;
    private bool hasLatestGps;

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
        SubscribeToLocationUpdates();
        if (autoRequestOnStart)
            RequestWaypointList();
        StartCoroutine(UpdateNextWaypointLoop());
    }

    private void Update()
    {
        if (!mapIsReady || !HasRoute || !hasLatestGps) return;

        // Periodically resync the GPS↔world origin to correct accumulated drift
        if (mapRecalibrationIntervalSeconds > 0f &&
            Time.time - lastMapRecalibrationTime >= mapRecalibrationIntervalSeconds)
        {
            RecalibrateMapOrigin();
        }

        // Redraw route every frame so it stays aligned as camera moves
        UpdateRouteLine(latestGpsLat, latestGpsLon);
    }

    private void OnDestroy()
    {
        UnsubscribeFromLocationUpdates();
    }

    public void RequestWaypointList()
    {
        SubscribeToLocationUpdates();

        if (routeRequestInProgress)
        {
            return;
        }

        StartCoroutine(RequestWaypointListCoroutine());
    }

    private IEnumerator RequestWaypointListCoroutine()
    {
        routeRequestInProgress = true;
        pendingRouteRequestAfterGps = false;
        mapIsReady = false;

        try
        {
            if (apiConfig == null)
            {
                apiConfig = Resources.Load<APIConfig>("APIConfig");
            }

            if (apiConfig == null || !apiConfig.HasGoogleMapsKey)
            {
                SetOutput("Missing API key. Assign APIConfig or place APIConfig.asset in Resources.");
                yield break;
            }

            if (string.IsNullOrWhiteSpace(destinationAddress))
            {
                SetOutput("Destination address is empty.");
                yield break;
            }

            if (!TryGetCurrentLocation(out _))
            {
                SetOutput("Waiting for GPS from phone...");
            }

            // Wait for location data from Firebase (ReceiveFromDB) or LocationManager fallback.
            const float locationWaitTimeoutSeconds = 15f;
            float waitedSeconds = 0f;
            while (!TryGetCurrentLocation(out _)
                   && waitedSeconds < locationWaitTimeoutSeconds)
            {
                yield return new WaitForSeconds(0.5f);
                waitedSeconds += 0.5f;
            }

            if (!TryGetCurrentLocation(out GeoLocation userLocation))
            {
                pendingRouteRequestAfterGps = true;
                SetOutput("No valid GPS location received yet. Check phone connection.");
                yield break;
            }

            // Wait briefly for heading so north alignment is accurate when route is built.
            float headingWait = 0f;
            while (LocationManager.Instance != null && !LocationManager.Instance.HasCompassHeading && headingWait < 5f)
            {
                SetOutput("Waiting for compass heading...");
                yield return new WaitForSeconds(0.5f);
                headingWait += 0.5f;
            }

            double originLat = userLocation.Latitude;
            double originLon = userLocation.Longitude;

            string origin = $"{originLat.ToString(CultureInfo.InvariantCulture)},{originLon.ToString(CultureInfo.InvariantCulture)}";
            string encodedDestination = UnityWebRequest.EscapeURL(destinationAddress);
            string url = $"{DIRECTIONS_API_URL}?origin={origin}&destination={encodedDestination}&mode={travelMode}&units=imperial&key={apiConfig.GoogleMapsApiKey}";

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    SetOutput($"Network error: {request.error}");
                    yield break;
                }

                JObject response;
                try
                {
                    response = JObject.Parse(request.downloadHandler.text);
                }
                catch
                {
                    SetOutput("Could not parse Google Directions response.");
                    yield break;
                }

                string status = response["status"]?.ToString();
                if (status != "OK")
                {
                    string errorMessage = response["error_message"]?.ToString();
                    SetOutput($"Directions API error: {status} {errorMessage}");
                    yield break;
                }

                JArray routes = response["routes"] as JArray;
                JArray legs = routes?[0]?["legs"] as JArray;
                JArray steps = legs?[0]?["steps"] as JArray;

                if (steps == null || steps.Count == 0)
                {
                    SetOutput("No waypoints returned for this route.");
                    yield break;
                }

                waypoints.Clear();
                waypointInstructions.Clear();
                waypointDistanceTexts.Clear();

                for (int i = 0; i < steps.Count; i++)
                {
                    JObject step = (JObject)steps[i];
                    string instruction = StripHtml(step["html_instructions"]?.ToString() ?? "Continue");
                    string maneuver = step["maneuver"]?.ToString() ?? string.Empty;
                    string distanceText = step["distance"]?["text"]?.ToString() ?? "";
                    double lat = step["end_location"]?["lat"]?.Value<double>() ?? 0;
                    double lon = step["end_location"]?["lng"]?.Value<double>() ?? 0;

                    waypoints.Add(new GeoLocation { Latitude = lat, Longitude = lon });
                    waypointInstructions.Add(BuildFriendlyInstruction(instruction, maneuver, distanceText));
                    waypointDistanceTexts.Add(distanceText);
                }

                nextWaypointIndex = 0;
                CurrentRoute = new RouteData { Waypoints = new List<GeoLocation>(waypoints) };
                InitializeGeoMap(originLat, originLon);
                UpdateNextDirectionOutput(originLat, originLon);
                UpdateRouteLine(originLat, originLon);
            }
        }
        finally
        {
            routeRequestInProgress = false;
        }
    }

    private void SubscribeToLocationUpdates()
    {
        if (subscribedToLocationUpdates || LocationManager.Instance == null)
        {
            return;
        }

        LocationManager.Instance.OnLocationUpdated += HandleLocationUpdated;
        subscribedToLocationUpdates = true;
    }

    private void UnsubscribeFromLocationUpdates()
    {
        if (!subscribedToLocationUpdates || LocationManager.Instance == null)
        {
            return;
        }

        LocationManager.Instance.OnLocationUpdated -= HandleLocationUpdated;
        subscribedToLocationUpdates = false;
    }

    private void HandleLocationUpdated(GeoLocation location)
    {
        if (!location.IsValid) return;

        // Always save latest GPS so Update() can redraw the line every frame
        latestGpsLat = location.Latitude;
        latestGpsLon = location.Longitude;
        hasLatestGps = true;

        if (pendingRouteRequestAfterGps && !routeRequestInProgress)
        {
            RequestWaypointList();
            return;
        }

        if (HasRoute)
            UpdateNextDirectionOutput(location.Latitude, location.Longitude);
        // Line is redrawn in Update() every frame — no need to call UpdateRouteLine here
    }

    private IEnumerator UpdateNextWaypointLoop()
    {
        var interval = new WaitForSeconds(1f);

        while (true)
        {
            if (waypoints.Count > 0 && nextWaypointIndex < waypoints.Count
                && LocationManager.Instance != null)
            {
                GeoLocation userLocation = LocationManager.Instance.CurrentLocation;
                if (userLocation.IsValid)
                {
                    double userLat = userLocation.Latitude;
                    double userLon = userLocation.Longitude;

                    AdvanceWaypointIndexForPosition(userLat, userLon);

                    UpdateNextDirectionOutput(userLat, userLon);
                }
            }

            yield return interval;
        }
    }

    private void AdvanceWaypointIndexForPosition(double userLat, double userLon)
    {
        while (nextWaypointIndex < waypoints.Count)
        {
            GeoLocation next = waypoints[nextWaypointIndex];
            float distanceMeters = LocationManager.GetDistance(userLat, userLon, next.Latitude, next.Longitude);
            if (distanceMeters > waypointReachedMeters)
            {
                break;
            }

            nextWaypointIndex++;
        }
    }

    private static string StripHtml(string value)
    {
        return Regex.Replace(value, "<.*?>", " ").Trim();
    }

    private static string BuildFriendlyInstruction(string rawInstruction, string maneuver, string distanceText)
    {
        string action = GetActionPhrase(rawInstruction, maneuver);
        string roadName = ExtractRoadName(rawInstruction);

        if (!string.IsNullOrWhiteSpace(action) && !string.IsNullOrWhiteSpace(roadName) && !string.IsNullOrWhiteSpace(distanceText))
        {
            return $"{action}, walk {distanceText} on {roadName}.";
        }

        if (!string.IsNullOrWhiteSpace(action) && !string.IsNullOrWhiteSpace(distanceText))
        {
            return $"{action}, continue for {distanceText}.";
        }

        if (!string.IsNullOrWhiteSpace(distanceText))
        {
            return $"{rawInstruction}. Continue for {distanceText}.";
        }

        return rawInstruction;
    }

    private static string GetActionPhrase(string rawInstruction, string maneuver)
    {
        string normalizedManeuver = (maneuver ?? string.Empty).ToLowerInvariant();
        string loweredInstruction = (rawInstruction ?? string.Empty).ToLowerInvariant();

        if (normalizedManeuver.Contains("uturn") || loweredInstruction.Contains("u-turn"))
        {
            return "Make a U-turn";
        }

        if (normalizedManeuver.Contains("right") || loweredInstruction.Contains("right"))
        {
            return "Take a right";
        }

        if (normalizedManeuver.Contains("left") || loweredInstruction.Contains("left"))
        {
            return "Take a left";
        }

        if (normalizedManeuver.Contains("straight") || loweredInstruction.Contains("continue") || loweredInstruction.Contains("head"))
        {
            return "Continue straight";
        }

        return "Continue";
    }

    private static string ExtractRoadName(string rawInstruction)
    {
        if (string.IsNullOrWhiteSpace(rawInstruction))
        {
            return string.Empty;
        }

        Match onRoadMatch = Regex.Match(rawInstruction, @"\b(?:onto|on)\s+([^,]+)", RegexOptions.IgnoreCase);
        if (onRoadMatch.Success)
        {
            return onRoadMatch.Groups[1].Value.Trim();
        }

        Match towardMatch = Regex.Match(rawInstruction, @"\btoward\s+([^,]+)", RegexOptions.IgnoreCase);
        if (towardMatch.Success)
        {
            return towardMatch.Groups[1].Value.Trim();
        }

        return string.Empty;
    }

    private static bool TryGetCurrentLocation(out GeoLocation location)
    {
        ReceiveFromDB dbReceiver = ReceiveFromDB.Instance;
        if (dbReceiver != null && dbReceiver.HasLocation)
        {
            location = dbReceiver.CurrentLocation;
            return true;
        }

        if (LocationManager.Instance != null
            && LocationManager.Instance.HasReceivedGPS
            && LocationManager.Instance.CurrentLocation.IsValid)
        {
            location = LocationManager.Instance.CurrentLocation;
            return true;
        }

        location = default;
        return false;
    }

    private void UpdateNextDirectionOutput(double userLat, double userLon)
    {
        if (waypoints.Count == 0)
        {
            return;
        }

        if (nextWaypointIndex >= waypoints.Count)
        {
            SetOutput("Arrived at destination.");
            return;
        }

        GeoLocation next = waypoints[nextWaypointIndex];
        float distanceMeters = LocationManager.GetDistance(userLat, userLon, next.Latitude, next.Longitude);
        string instruction = waypointInstructions[nextWaypointIndex];
        string legDistance = waypointDistanceTexts[nextWaypointIndex];
        string currentLocation = $"Current location: {userLat.ToString("F6", CultureInfo.InvariantCulture)}, {userLon.ToString("F6", CultureInfo.InvariantCulture)}";
        string nextWaypointLocation = $"Next waypoint: {next.Latitude.ToString("F6", CultureInfo.InvariantCulture)}, {next.Longitude.ToString("F6", CultureInfo.InvariantCulture)}";

        SetOutput($"Next: {instruction}\nRemaining to next waypoint: {distanceMeters:F0} m\nStep distance: {legDistance}\n{nextWaypointLocation}\n{currentLocation}");
    }

    private void UpdateRouteLine(double userLat, double userLon)
    {
        if (!EnsureRouteLineRenderer()) return;

        if (waypoints.Count == 0 || !mapIsReady)
        {
            routeLineRenderer.positionCount = 0;
            return;
        }

        routeLineRenderer.startWidth = routeLineWidth;
        routeLineRenderer.endWidth   = routeLineWidth;

        int remainingWaypoints = Mathf.Max(0, waypoints.Count - nextWaypointIndex);
        if (remainingWaypoints == 0)
        {
            routeLineRenderer.positionCount = 0;
            return;
        }

        // Skip user→first-waypoint lead-in segment if user is already at the first waypoint
        GeoLocation firstWp = waypoints[nextWaypointIndex];
        float distToFirst = LocationManager.GetDistance(userLat, userLon, firstWp.Latitude, firstWp.Longitude);
        bool userAtFirst = distToFirst <= waypointReachedMeters;

        int pointCount = userAtFirst ? remainingWaypoints : remainingWaypoints + 1;
        routeLineRenderer.positionCount = pointCount;

        // Camera position is always the exact Unity-space representation of the user's real position
        Vector3 camPos = Camera.main != null ? Camera.main.transform.position : mapOriginWorldPos;
        float lineY = camPos.y + routeLineHeightOffset;

        int offset = 0;
        if (!userAtFirst)
        {
            routeLineRenderer.SetPosition(0, new Vector3(camPos.x, lineY, camPos.z));
            offset = 1;
        }

        for (int i = 0; i < remainingWaypoints; i++)
        {
            GeoLocation wp = waypoints[nextWaypointIndex + i];
            Vector3 worldPos = GeoToWorld(wp.Latitude, wp.Longitude);
            worldPos.y = lineY;
            routeLineRenderer.SetPosition(offset + i, worldPos);
        }
    }

    // ─── GeoMap ───────────────────────────────────────────────────────────────
    // The GeoMap stores:
    //   • mapNorthDir / mapEastDir  – world-space unit vectors for geographic north/east.
    //                                  Derived once from the phone compass at route init.
    //                                  NEVER updated after that (compass noise can jitter).
    //   • mapOriginLat / mapOriginLon / mapOriginWorldPos  – a GPS↔Unity-world position pair.
    //                                  Updated every mapRecalibrationIntervalSeconds so that
    //                                  any drift between VR tracking and GPS is corrected.
    //
    // Converting GPS → Unity world:
    //   north/east meter offsets from origin GPS → projected onto north/east basis → added to origin world pos.
    // ─────────────────────────────────────────────────────────────────────────

    private void InitializeGeoMap(double originLat, double originLon)
    {
        // origin world position = exactly where the camera is right now
        Vector3 camPos = Camera.main != null ? Camera.main.transform.position : transform.position;
        mapOriginLat      = originLat;
        mapOriginLon      = originLon;
        mapOriginWorldPos = camPos;
        lastMapRecalibrationTime = Time.time;

        // Derive north/east world-space directions from the phone's compass heading.
        // The user faces the SAME direction with both headset and phone at this moment, so:
        //   • camera forward (in Unity) = the real-world direction of phone heading H
        //   • north = rotate camera forward by −H  (counterclockwise H degrees)
        if (LocationManager.Instance != null && LocationManager.Instance.HasCompassHeading)
        {
            float heading = LocationManager.Instance.CompassHeading + compassHeadingOffsetDegrees;

            Vector3 camFwd = Camera.main != null ? Camera.main.transform.forward : Vector3.forward;
            camFwd.y = 0f;
            if (camFwd.sqrMagnitude < 0.0001f) camFwd = Vector3.forward;
            camFwd.Normalize();

            mapNorthDir = Quaternion.Euler(0f, -heading, 0f) * camFwd;
            mapNorthDir.y = 0f;
            mapNorthDir.Normalize();

            // East = 90° clockwise from north (cross product with up)
            mapEastDir = Vector3.Cross(Vector3.up, mapNorthDir);
            mapEastDir.Normalize();

            lastAppliedHeading = heading;
            hasAppliedHeading = true;

            Debug.Log($"[NavigationRoute] GeoMap INIT: heading={heading:F1}° camFwd={camFwd} northDir={mapNorthDir} eastDir={mapEastDir} origin=({originLat:F6},{originLon:F6}) worldPos={camPos}");
        }
        else
        {
            mapNorthDir = Vector3.forward;
            mapEastDir  = Vector3.right;
            hasAppliedHeading = false;
            Debug.LogWarning("[NavigationRoute] No compass heading at init — north=forward, east=right. Orientation will be wrong.");
        }

        mapIsReady = true;
    }

    // Resyncs the GPS↔world origin to current GPS + current camera position,
    // and optionally applies heading correction to refine north/east basis.
    private void RecalibrateMapOrigin()
    {
        if (!hasLatestGps) return;

        if (enableHeadingCorrection)
        {
            ApplyHeadingCorrection();
        }

        Vector3 camPos = Camera.main != null ? Camera.main.transform.position : mapOriginWorldPos;
        mapOriginLat = latestGpsLat;
        mapOriginLon = latestGpsLon;
        mapOriginWorldPos = camPos;
        lastMapRecalibrationTime = Time.time;

        Debug.Log($"[NavigationRoute] GeoMap recalibrated: GPS ({mapOriginLat:F6},{mapOriginLon:F6}) → world {mapOriginWorldPos}");
    }

    private void ApplyHeadingCorrection()
    {
        if (LocationManager.Instance == null || !LocationManager.Instance.HasCompassHeading)
        {
            return;
        }

        float currentHeading = LocationManager.Instance.CompassHeading + compassHeadingOffsetDegrees;

        if (!hasAppliedHeading)
        {
            lastAppliedHeading = currentHeading;
            hasAppliedHeading = true;
            return;
        }

        float headingDelta = Mathf.DeltaAngle(lastAppliedHeading, currentHeading);
        float absDelta = Mathf.Abs(headingDelta);
        if (absDelta < headingCorrectionMinDeltaDegrees)
        {
            return;
        }

        float clampedDelta = Mathf.Clamp(headingDelta, -maxHeadingCorrectionPerUpdateDegrees, maxHeadingCorrectionPerUpdateDegrees);
        float appliedDelta = clampedDelta * headingCorrectionStrength;

        // Keep convention consistent with init: as heading increases clockwise, north rotates counterclockwise.
        Quaternion correctionRotation = Quaternion.Euler(0f, -appliedDelta, 0f);
        mapNorthDir = correctionRotation * mapNorthDir;
        mapNorthDir.y = 0f;
        mapNorthDir.Normalize();

        mapEastDir = Vector3.Cross(Vector3.up, mapNorthDir);
        mapEastDir.y = 0f;
        mapEastDir.Normalize();

        lastAppliedHeading = Mathf.Repeat(lastAppliedHeading + appliedDelta, 360f);

        Debug.Log($"[NavigationRoute] Heading correction applied: rawDelta={headingDelta:F1}°, applied={appliedDelta:F1}°, headingNow={currentHeading:F1}°");
    }

    // Converts a GPS coordinate to Unity world space using the live GeoMap.
    private Vector3 GeoToWorld(double lat, double lon)
    {
        const double R = 6378137.0; // Earth radius in metres

        double dLat     = (lat - mapOriginLat) * (Math.PI / 180.0);
        double dLon     = (lon - mapOriginLon) * (Math.PI / 180.0);
        double refLatRad = mapOriginLat * (Math.PI / 180.0);
        double latRad    = lat          * (Math.PI / 180.0);

        double northMeters = dLat * R;
        double eastMeters  = dLon * Math.Cos((refLatRad + latRad) * 0.5) * R;

        return mapOriginWorldPos
               + mapNorthDir * (float)northMeters
               + mapEastDir  * (float)eastMeters;
    }

    private bool EnsureRouteLineRenderer()
    {
        if (routeLineRenderer == null && autoCreateRouteLineRenderer)
        {
            routeLineRenderer = GetComponent<LineRenderer>();
            if (routeLineRenderer == null)
            {
                routeLineRenderer = gameObject.AddComponent<LineRenderer>();
            }

            routeLineRenderer.useWorldSpace = true;
            routeLineRenderer.textureMode = LineTextureMode.Stretch;
            routeLineRenderer.numCapVertices = 4;
        }

        if (routeLineRenderer == null)
        {
            return false;
        }

        if (routeLineRenderer.sharedMaterial == null)
        {
            Shader lineShader = Shader.Find("Sprites/Default");
            if (lineShader != null)
            {
                routeLineRenderer.sharedMaterial = new Material(lineShader);
            }
        }

        routeLineRenderer.startColor = Color.cyan;
        routeLineRenderer.endColor = Color.cyan;

        return true;
    }

    private void SetOutput(string message)
    {
        if (outputText && outputText2 != null)
        {
            outputText.text = message;
            outputText2.text = message;
        }

        Debug.Log($"[NavigationRoute] {message}");
    }
}
