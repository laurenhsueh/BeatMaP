using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
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

    [Header("Route")]
    [SerializeField] private string destinationAddress = "";
    [SerializeField] private string travelMode = "walking";
    [SerializeField] private float waypointReachedMeters = 6f;
    [SerializeField] private bool autoRequestOnStart = true;

    private const string DIRECTIONS_API_URL = "https://maps.googleapis.com/maps/api/directions/json";

    /// <summary>
    /// Snapshot of the current route waypoints.
    /// Exposed so LocationManager can use it for GPS movement simulation.
    /// </summary>
    public class RouteData
    {
        public List<GeoLocation> Waypoints = new List<GeoLocation>();
    }

    public RouteData CurrentRoute { get; private set; }
    public int CurrentWaypointIndex => nextWaypointIndex;

    private readonly List<GeoLocation> waypoints = new();
    private readonly List<string> waypointInstructions = new();
    private readonly List<string> waypointDistanceTexts = new();
    private int nextWaypointIndex;
    private bool routeRequestInProgress;
    private bool pendingRouteRequestAfterGps;
    private bool subscribedToLocationUpdates;

    public bool HasRoute => waypoints.Count > 0;

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
        {
            RequestWaypointList();
        }

        StartCoroutine(UpdateNextWaypointLoop());
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

            if (LocationManager.Instance == null || !LocationManager.Instance.HasReceivedGPS)
            {
                SetOutput($"Waiting for companion app GPS...\n{BuildCompanionAppHint()}");
            }

            // Wait for GPS data to arrive from companion app via LocationManager/LocationReceiver
            const float locationWaitTimeoutSeconds = 15f;
            float waitedSeconds = 0f;
            while ((LocationManager.Instance == null || !LocationManager.Instance.HasReceivedGPS)
                   && waitedSeconds < locationWaitTimeoutSeconds)
            {
                yield return new WaitForSeconds(0.5f);
                waitedSeconds += 0.5f;
            }

            if (LocationManager.Instance == null || !LocationManager.Instance.HasReceivedGPS
                || !LocationManager.Instance.CurrentLocation.IsValid)
            {
                pendingRouteRequestAfterGps = true;
                SetOutput($"No valid location from companion app yet.\n{BuildCompanionAppHint()}");
                yield break;
            }

            GeoLocation userLocation = LocationManager.Instance.CurrentLocation;
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
                UpdateNextDirectionOutput(originLat, originLon);
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
        if (!location.IsValid)
        {
            return;
        }

        if (pendingRouteRequestAfterGps && !routeRequestInProgress)
        {
            RequestWaypointList();
            return;
        }

        if (HasRoute)
        {
            UpdateNextDirectionOutput(location.Latitude, location.Longitude);
        }
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

                    GeoLocation next = waypoints[nextWaypointIndex];
                    float distanceMeters = LocationManager.GetDistance(userLat, userLon, next.Latitude, next.Longitude);

                    if (distanceMeters <= waypointReachedMeters)
                    {
                        nextWaypointIndex++;
                    }

                    UpdateNextDirectionOutput(userLat, userLon);
                }
            }

            yield return interval;
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

    private static string BuildCompanionAppHint()
    {
        LocationReceiver receiver = FindFirstObjectByType<LocationReceiver>();
        if (receiver == null)
        {
            return "Add LocationReceiver to the scene, then connect your companion app.";
        }

        string host = GetLocalIPv4Address();
        string serverStatus = receiver.isServerRunning ? "UP" : "DOWN";
        return $"Companion app WebSocket URL: ws://{host}:{receiver.port}\nServer status: {serverStatus}";
    }

    private static string GetLocalIPv4Address()
    {
        try
        {
            foreach (NetworkInterface networkInterface in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (networkInterface.OperationalStatus != OperationalStatus.Up ||
                    networkInterface.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                {
                    continue;
                }

                IPInterfaceProperties properties = networkInterface.GetIPProperties();
                foreach (UnicastIPAddressInformation unicast in properties.UnicastAddresses)
                {
                    if (unicast.Address.AddressFamily == AddressFamily.InterNetwork &&
                        !IPAddress.IsLoopback(unicast.Address))
                    {
                        return unicast.Address.ToString();
                    }
                }
            }
        }
        catch
        {
            // Fall through to fallback text below.
        }

        return "YOUR_DEVICE_IP";
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

        SetOutput($"Next: {instruction}\nRemaining to next waypoint: {distanceMeters:F0} m\nStep distance: {legDistance}\n{currentLocation}");
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
