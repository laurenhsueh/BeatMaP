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
    [Header("References")]
    [SerializeField] private APIConfig apiConfig;
    [SerializeField] private GetLocation getLocation;

    [Header("Output (Optional)")]
    [SerializeField] private TMP_Text outputText;

    [Header("Route")]
    [SerializeField] private string destinationAddress = "";
    [SerializeField] private string travelMode = "walking";
    [SerializeField] private float waypointReachedMeters = 6f;
    [SerializeField] private bool autoRequestOnStart = true;

    private const string DIRECTIONS_API_URL = "https://maps.googleapis.com/maps/api/directions/json";

    private readonly List<Vector2> waypoints = new();
    private readonly List<string> waypointInstructions = new();
    private readonly List<string> waypointDistanceTexts = new();
    private int nextWaypointIndex;

    public bool HasRoute => waypoints.Count > 0;
    public int NextWaypointIndex => nextWaypointIndex;
    public Vector2? CurrentUserLatLon { get; private set; }
    public Vector2? NextWaypointLatLon =>
        nextWaypointIndex >= 0 && nextWaypointIndex < waypoints.Count ? waypoints[nextWaypointIndex] : null;

    private void Start()
    {
        if (autoRequestOnStart)
        {
            RequestWaypointList();
        }

        StartCoroutine(UpdateNextWaypointLoop());
    }

    public void RequestWaypointList()
    {
        StartCoroutine(RequestWaypointListCoroutine());
    }

    private IEnumerator RequestWaypointListCoroutine()
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

        const float locationWaitTimeoutSeconds = 15f;
        float waitedSeconds = 0f;
        while (!TryReadUserLocationFromGetLocation(out _, out _) && waitedSeconds < locationWaitTimeoutSeconds)
        {
            yield return new WaitForSeconds(0.5f);
            waitedSeconds += 0.5f;
        }

        if (!TryReadUserLocationFromGetLocation(out double originLat, out double originLon))
        {
            SetOutput("No valid location from GetLocation yet. Wait for GPS and try again.");
            yield break;
        }

        CurrentUserLatLon = new Vector2((float)originLat, (float)originLon);

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

                waypoints.Add(new Vector2((float)lat, (float)lon));
                waypointInstructions.Add(BuildFriendlyInstruction(instruction, maneuver, distanceText));
                waypointDistanceTexts.Add(distanceText);
            }

            nextWaypointIndex = 0;
            UpdateNextDirectionOutput(originLat, originLon);
        }
    }

    private IEnumerator UpdateNextWaypointLoop()
    {
        var interval = new WaitForSeconds(1f);

        while (true)
        {
            if (waypoints.Count > 0 && nextWaypointIndex < waypoints.Count)
            {
                if (TryReadUserLocationFromGetLocation(out double userLat, out double userLon))
                {
                    CurrentUserLatLon = new Vector2((float)userLat, (float)userLon);

                    Vector2 next = waypoints[nextWaypointIndex];
                    float distanceMeters = DistanceMeters(userLat, userLon, next.x, next.y);

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

    private bool TryReadUserLocationFromGetLocation(out double latitude, out double longitude)
    {
        latitude = 0;
        longitude = 0;

        if (getLocation == null || getLocation.output == null)
        {
            return false;
        }

        string txt = getLocation.output.text;
        Match labeledFormatMatch = Regex.Match(
            txt,
            @"Latitude:\s*(-?\d+(?:\.\d+)?)\s*[\r\n]+Longitude:\s*(-?\d+(?:\.\d+)?)",
            RegexOptions.IgnoreCase);

        if (labeledFormatMatch.Success)
        {
            return double.TryParse(labeledFormatMatch.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out latitude)
                && double.TryParse(labeledFormatMatch.Groups[2].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out longitude);
        }

        Match plainFormatMatch = Regex.Match(
            txt,
            @"(-?\d+(?:\.\d+)?)\s+(-?\d+(?:\.\d+)?)",
            RegexOptions.IgnoreCase);

        if (!plainFormatMatch.Success)
        {
            return false;
        }

        return double.TryParse(plainFormatMatch.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out latitude)
            && double.TryParse(plainFormatMatch.Groups[2].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out longitude);
    }

    private static float DistanceMeters(double lat1, double lon1, double lat2, double lon2)
    {
        const double earthRadius = 6371000.0;
        double dLat = Mathf.Deg2Rad * (float)(lat2 - lat1);
        double dLon = Mathf.Deg2Rad * (float)(lon2 - lon1);
        double lat1Rad = Mathf.Deg2Rad * (float)lat1;
        double lat2Rad = Mathf.Deg2Rad * (float)lat2;

        double a = Mathf.Sin((float)(dLat / 2)) * Mathf.Sin((float)(dLat / 2))
                 + Mathf.Cos((float)lat1Rad) * Mathf.Cos((float)lat2Rad)
                 * Mathf.Sin((float)(dLon / 2)) * Mathf.Sin((float)(dLon / 2));

        return (float)(2 * earthRadius * Mathf.Atan2(Mathf.Sqrt((float)a), Mathf.Sqrt((float)(1 - a))));
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

        Vector2 next = waypoints[nextWaypointIndex];
        float distanceMeters = DistanceMeters(userLat, userLon, next.x, next.y);
        string instruction = waypointInstructions[nextWaypointIndex];
        string legDistance = waypointDistanceTexts[nextWaypointIndex];

        SetOutput($"Next: {instruction}\nRemaining to next waypoint: {distanceMeters:F0} m\nStep distance: {legDistance}");
    }

    private void SetOutput(string message)
    {
        if (outputText != null)
        {
            outputText.text = message;
        }

        Debug.Log($"[NavigationRoute] {message}");
    }
}
