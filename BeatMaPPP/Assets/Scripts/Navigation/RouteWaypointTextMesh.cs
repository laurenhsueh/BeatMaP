using System.Collections;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

public class RouteWaypointTextMesh : MonoBehaviour
{
    [Header("API")]
    [SerializeField] private APIConfig apiConfig;

    [Header("Output")]
    [SerializeField] private TMP_Text outputText;
    [SerializeField] private TextMesh fallbackTextMesh;

    [Header("Route Request")]
    [SerializeField] private string destinationAddress = "";
    [SerializeField] private string travelMode = "walking";
    [SerializeField] private bool autoRequestOnStart = true;

    [Header("Origin")]
    [SerializeField] private bool useDeviceLocation = true;
    [SerializeField] private double originLatitude;
    [SerializeField] private double originLongitude;

    private const string DIRECTIONS_API_URL = "https://maps.googleapis.com/maps/api/directions/json";

    private void Start()
    {
        if (autoRequestOnStart)
        {
            RequestWaypointList();
        }
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

        double originLat = originLatitude;
        double originLon = originLongitude;

        if (useDeviceLocation)
        {
            if (!Input.location.isEnabledByUser)
            {
                SetOutput("Location permission is disabled on this device.");
                yield break;
            }

            if (Input.location.status == LocationServiceStatus.Stopped)
            {
                Input.location.Start();
            }

            const float timeoutSeconds = 10f;
            float waited = 0f;
            while (Input.location.status == LocationServiceStatus.Initializing && waited < timeoutSeconds)
            {
                waited += Time.deltaTime;
                yield return null;
            }

            if (Input.location.status != LocationServiceStatus.Running)
            {
                SetOutput($"Unable to get device location. Status: {Input.location.status}");
                yield break;
            }

            originLat = Input.location.lastData.latitude;
            originLon = Input.location.lastData.longitude;
        }

        string origin = $"{originLat},{originLon}";
        string encodedDestination = UnityWebRequest.EscapeURL(destinationAddress);
        string url = $"{DIRECTIONS_API_URL}?origin={origin}&destination={encodedDestination}&mode={travelMode}&key={apiConfig.GoogleMapsApiKey}";

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

            StringBuilder builder = new StringBuilder();
            builder.AppendLine($"Route to: {destinationAddress}");
            builder.AppendLine($"Mode: {travelMode}");
            builder.AppendLine();

            for (int i = 0; i < steps.Count; i++)
            {
                JObject step = (JObject)steps[i];
                string instruction = StripHtml(step["html_instructions"]?.ToString() ?? "Continue");
                string distanceText = step["distance"]?["text"]?.ToString() ?? "";
                double lat = step["end_location"]?["lat"]?.Value<double>() ?? 0;
                double lon = step["end_location"]?["lng"]?.Value<double>() ?? 0;

                builder.AppendLine($"{i + 1}. {instruction}");
                builder.AppendLine($"   Reach: {lat:F6}, {lon:F6} ({distanceText})");
            }

            SetOutput(builder.ToString());
        }
    }

    private static string StripHtml(string value)
    {
        return Regex.Replace(value, "<.*?>", " ").Trim();
    }

    private void SetOutput(string message)
    {
        if (outputText != null)
        {
            outputText.text = message;
        }

        if (fallbackTextMesh != null)
        {
            fallbackTextMesh.text = message;
        }

        Debug.Log($"[RouteWaypointTextMesh] {message}");
    }
}
