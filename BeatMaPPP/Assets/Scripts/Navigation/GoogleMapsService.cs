using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

// /// Handles Google Maps API requests for directions and routing.

public class GoogleMapsService : MonoBehaviour
{
    public static GoogleMapsService Instance { get; private set; }
    [Header("API Configuration")]
    [SerializeField] private APIConfig apiConfig;
    [Header("Settings")]
    [SerializeField] private string travelMode = "walking";
    [SerializeField] private bool enableAlternatives = false;
    private const string DIRECTIONS_API_URL = "https://maps.googleapis.com/maps/api/directions/json";
    private string apiKey;

    // Events
    // public event Action<NavigationRoute> OnRouteReceived;
    // public event Action<string> OnRouteError;

    // private void Awake()
    // {
    //     if (Instance != null && Instance != this)
    //     {
    //         Destroy(gameObject);
    //         return;
    //     }
    //     Instance = this;

    //     // Load API key from config
    //     if (apiConfig == null)
    //     {
    //         // Try to load from Resources folder
    //         apiConfig = Resources.Load<APIConfig>("APIConfig");

    //         // Try to find in project if not in Resources (Editor only)
    //         #if UNITY_EDITOR
    //         if (apiConfig == null)
    //         {
    //             string[] guids = UnityEditor.AssetDatabase.FindAssets("t:APIConfig");
    //             if (guids.Length > 0)
    //             {
    //                 string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
    //                 apiConfig = UnityEditor.AssetDatabase.LoadAssetAtPath<APIConfig>(path);
    //                 Debug.Log($"[GoogleMapsService] Auto-loaded APIConfig from: {path}");
    //             }
    //         }
    //         #endif
    //     }

    //     if (apiConfig != null && apiConfig.HasGoogleMapsKey)
    //     {
    //         apiKey = apiConfig.GoogleMapsApiKey;
    //         Debug.Log("[GoogleMapsService] API key loaded successfully");
    //     }
    //     else if (apiConfig == null)
    //     {
    //         Debug.LogError("[GoogleMapsService] APIConfig not found! Assign it in the Inspector or place in Resources folder.");
    //     }
    //     else
    //     {
    //         Debug.LogError("[GoogleMapsService] APIConfig found but Google Maps API key is empty! Fill it in the Inspector.");
    //     }
    // }

//     /// <summary>
//     /// Set the API key at runtime (useful for loading from secure storage)
//     /// </summary>
//     public void SetApiKey(string key)
//     {
//         apiKey = key;
//     }

//     /// <summary>
//     /// Request a walking route from origin to destination
//     /// </summary>
//     public void GetRoute(GeoLocation origin, GeoLocation destination)
//     {
//         GetRoute(origin.Latitude, origin.Longitude, destination.Latitude, destination.Longitude);
//     }

//     /// <summary>
//     /// Request a walking route from origin to destination using coordinates
//     /// </summary>
//     public void GetRoute(double originLat, double originLon, double destLat, double destLon)
//     {
//         StartCoroutine(FetchRoute(originLat, originLon, destLat, destLon));
//     }

//     /// <summary>
//     /// Request a walking route using a destination address string
//     /// </summary>
//     public void GetRouteToAddress(GeoLocation origin, string destinationAddress)
//     {
//         StartCoroutine(FetchRouteToAddress(origin.Latitude, origin.Longitude, destinationAddress));
//     }

//     private IEnumerator FetchRoute(double originLat, double originLon, double destLat, double destLon)
//     {
//         string originStr = $"{originLat},{originLon}";
//         string destStr = $"{destLat},{destLon}";

//         Debug.Log($"[GoogleMapsService] === SENDING REQUEST ===");
//         Debug.Log($"[GoogleMapsService] Origin: {originStr}");
//         Debug.Log($"[GoogleMapsService] Destination: {destStr}");
//         Debug.Log($"[GoogleMapsService] Mode: {travelMode}");

//         string url = $"{DIRECTIONS_API_URL}?origin={originStr}&destination={destStr}&mode={travelMode}&alternatives={enableAlternatives.ToString().ToLower()}&key={apiKey}";

//         yield return FetchRouteRequest(url);
//     }

//     private IEnumerator FetchRouteToAddress(double originLat, double originLon, string destination)
//     {
//         if (originLat == 0 && originLon == 0)
//         {
//             OnRouteError?.Invoke("Waiting for GPS location from companion app");
//             yield break;
//         }

//         string originStr = $"{originLat},{originLon}";
//         string destEncoded = UnityWebRequest.EscapeURL(destination);

//         Debug.Log($"[GoogleMapsService] === SENDING REQUEST (Address) ===");
//         Debug.Log($"[GoogleMapsService] Origin: {originStr}");
//         Debug.Log($"[GoogleMapsService] Destination Address: {destination}");
//         Debug.Log($"[GoogleMapsService] Mode: {travelMode}");

//         string url = $"{DIRECTIONS_API_URL}?origin={originStr}&destination={destEncoded}&mode={travelMode}&alternatives={enableAlternatives.ToString().ToLower()}&key={apiKey}";

//         yield return FetchRouteRequest(url);
//     }

//     private IEnumerator FetchRouteRequest(string url)
//     {
//         using (UnityWebRequest request = UnityWebRequest.Get(url))
//         {
//             yield return request.SendWebRequest();

//             if (request.result != UnityWebRequest.Result.Success)
//             {
//                 Debug.LogError($"[GoogleMapsService] === NETWORK ERROR ===");
//                 Debug.LogError($"[GoogleMapsService] Error: {request.error}");
//                 OnRouteError?.Invoke($"Network error: {request.error}");
//                 yield break;
//             }

//             Debug.Log($"[GoogleMapsService] === RESPONSE RECEIVED ===");
//             Debug.Log($"[GoogleMapsService] Response length: {request.downloadHandler.text.Length} chars");

//             // Log first 500 chars of response for debugging
//             string responsePreview = request.downloadHandler.text;
//             if (responsePreview.Length > 500)
//                 responsePreview = responsePreview.Substring(0, 500) + "...";
//             Debug.Log($"[GoogleMapsService] Response preview: {responsePreview}");

//             NavigationRoute route = null;
//             string parseError = null;

//             try
//             {
//                 route = ParseDirectionsResponse(request.downloadHandler.text);
//             }
//             catch (Exception ex)
//             {
//                 parseError = ex.Message;
//             }

//             if (parseError != null)
//             {
//                 Debug.LogError($"[GoogleMapsService] === PARSE ERROR ===");
//                 Debug.LogError($"[GoogleMapsService] Error: {parseError}");
//                 OnRouteError?.Invoke($"Parse error: {parseError}");
//                 yield break;
//             }

//             if (route != null)
//             {
//                 Debug.Log($"[GoogleMapsService] === ROUTE PARSED ===");
//                 Debug.Log($"[GoogleMapsService] Start: {route.StartAddress}");
//                 Debug.Log($"[GoogleMapsService] End: {route.EndAddress}");
//                 Debug.Log($"[GoogleMapsService] Distance: {route.TotalDistanceMeters}m");
//                 Debug.Log($"[GoogleMapsService] Duration: {route.TotalDurationSeconds}s");
//                 Debug.Log($"[GoogleMapsService] Waypoints: {route.Waypoints.Count}");
//                 Debug.Log($"[GoogleMapsService] Steps: {route.Steps.Count}");

//                 if (route.Waypoints.Count > 0)
//                 {
//                     var first = route.Waypoints[0];
//                     var last = route.Waypoints[route.Waypoints.Count - 1];
//                     Debug.Log($"[GoogleMapsService] First waypoint: {first.Latitude}, {first.Longitude}");
//                     Debug.Log($"[GoogleMapsService] Last waypoint: {last.Latitude}, {last.Longitude}");
//                 }

//                 if (fetchElevationData && route.Waypoints.Count > 0)
//                 {
//                     // Fetch elevation data for waypoints
//                     yield return FetchElevationForRoute(route);
//                 }
//                 OnRouteReceived?.Invoke(route);
//             }
//         }
//     }

//     private NavigationRoute ParseDirectionsResponse(string json)
//     {
//         JObject response = JObject.Parse(json);

//         string status = response["status"]?.ToString();
//         if (status != "OK")
//         {
//             string errorMessage = response["error_message"]?.ToString() ?? status;
//             OnRouteError?.Invoke($"API Error: {errorMessage}");
//             return null;
//         }

//         JArray routes = response["routes"] as JArray;
//         if (routes == null || routes.Count == 0)
//         {
//             OnRouteError?.Invoke("No routes found");
//             return null;
//         }

//         // Get the first route
//         JObject route = routes[0] as JObject;
//         JArray legs = route["legs"] as JArray;
//         JObject leg = legs[0] as JObject;

//         NavigationRoute navRoute = new NavigationRoute
//         {
//             TotalDistanceMeters = leg["distance"]["value"].Value<float>(),
//             TotalDurationSeconds = leg["duration"]["value"].Value<float>(),
//             StartAddress = leg["start_address"]?.ToString() ?? "",
//             EndAddress = leg["end_address"]?.ToString() ?? "",
//             Steps = new List<NavigationStep>(),
//             Waypoints = new List<GeoLocation>()
//         };

//         // Parse individual steps for turn-by-turn instructions
//         // AND build detailed waypoints from step polylines (more accurate than overview_polyline)
//         JArray steps = leg["steps"] as JArray;
//         HashSet<string> addedPoints = new HashSet<string>(); // Avoid duplicate points at step boundaries

//         foreach (JObject step in steps)
//         {
//             NavigationStep navStep = new NavigationStep
//             {
//                 Instruction = StripHtmlTags(step["html_instructions"]?.ToString() ?? ""),
//                 DistanceMeters = step["distance"]["value"].Value<float>(),
//                 DurationSeconds = step["duration"]["value"].Value<float>(),
//                 Maneuver = step["maneuver"]?.ToString() ?? "straight",
//                 StartLocation = new GeoLocation
//                 {
//                     Latitude = step["start_location"]["lat"].Value<double>(),
//                     Longitude = step["start_location"]["lng"].Value<double>()
//                 },
//                 EndLocation = new GeoLocation
//                 {
//                     Latitude = step["end_location"]["lat"].Value<double>(),
//                     Longitude = step["end_location"]["lng"].Value<double>()
//                 }
//             };

//             // Decode step polyline for detailed path
//             string stepPolyline = step["polyline"]["points"].ToString();
//             navStep.PathPoints = DecodePolyline(stepPolyline);

//             // Add step path points to main waypoints list (more detailed than overview_polyline)
//             // This follows the actual sidewalk path instead of cutting corners
//             foreach (var point in navStep.PathPoints)
//             {
//                 string pointKey = $"{point.Latitude:F6},{point.Longitude:F6}";
//                 if (!addedPoints.Contains(pointKey))
//                 {
//                     navRoute.Waypoints.Add(point);
//                     addedPoints.Add(pointKey);
//                 }
//             }

//             navRoute.Steps.Add(navStep);
//         }

//         return navRoute;
//     }

//     /// <summary>
//     /// Decode a Google Maps encoded polyline into GPS coordinates
//     /// </summary>
//     private List<GeoLocation> DecodePolyline(string encodedPolyline)
//     {
//         List<GeoLocation> points = new List<GeoLocation>();

//         int index = 0;
//         int lat = 0;
//         int lng = 0;

//         while (index < encodedPolyline.Length)
//         {
//             // Decode latitude
//             int shift = 0;
//             int result = 0;
//             int b;

//             do
//             {
//                 b = encodedPolyline[index++] - 63;
//                 result |= (b & 0x1f) << shift;
//                 shift += 5;
//             } while (b >= 0x20);

//             lat += ((result & 1) != 0 ? ~(result >> 1) : (result >> 1));

//             // Decode longitude
//             shift = 0;
//             result = 0;

//             do
//             {
//                 b = encodedPolyline[index++] - 63;
//                 result |= (b & 0x1f) << shift;
//                 shift += 5;
//             } while (b >= 0x20);

//             lng += ((result & 1) != 0 ? ~(result >> 1) : (result >> 1));

//             points.Add(new GeoLocation
//             {
//                 Latitude = lat / 1e5,
//                 Longitude = lng / 1e5
//             });
//         }

//         return points;
//     }

//     private string StripHtmlTags(string input)
//     {
//         return System.Text.RegularExpressions.Regex.Replace(input, "<.*?>", " ").Trim();
//     }

//     /// <summary>
//     /// Fetches elevation data for route waypoints from Google Elevation API.
//     /// Updates the Altitude field of each waypoint with real-world elevation.
//     /// </summary>
//     private IEnumerator FetchElevationForRoute(NavigationRoute route)
//     {
//         // Google Elevation API allows up to 512 locations per request
//         // For longer routes, we need to batch the requests
//         const int maxLocationsPerRequest = 500;

//         for (int batchStart = 0; batchStart < route.Waypoints.Count; batchStart += maxLocationsPerRequest)
//         {
//             int batchEnd = Mathf.Min(batchStart + maxLocationsPerRequest, route.Waypoints.Count);

//             // Build the locations string for this batch
//             System.Text.StringBuilder locationsBuilder = new System.Text.StringBuilder();
//             for (int i = batchStart; i < batchEnd; i++)
//             {
//                 if (locationsBuilder.Length > 0)
//                     locationsBuilder.Append("|");
//                 locationsBuilder.Append($"{route.Waypoints[i].Latitude},{route.Waypoints[i].Longitude}");
//             }

//             string url = $"{ELEVATION_API_URL}?locations={UnityWebRequest.EscapeURL(locationsBuilder.ToString())}&key={apiKey}";

//             using (UnityWebRequest request = UnityWebRequest.Get(url))
//             {
//                 yield return request.SendWebRequest();

//                 if (request.result != UnityWebRequest.Result.Success)
//                 {
//                     Debug.LogWarning($"[GoogleMapsService] Elevation API error: {request.error}");
//                     continue;
//                 }

//                 try
//                 {
//                     JObject response = JObject.Parse(request.downloadHandler.text);
//                     string status = response["status"]?.ToString();

//                     if (status == "OK")
//                     {
//                         JArray results = response["results"] as JArray;
//                         if (results != null)
//                         {
//                             for (int i = 0; i < results.Count && (batchStart + i) < route.Waypoints.Count; i++)
//                             {
//                                 float elevation = results[i]["elevation"].Value<float>();
//                                 GeoLocation updatedWaypoint = route.Waypoints[batchStart + i];
//                                 updatedWaypoint.Altitude = elevation;
//                                 route.Waypoints[batchStart + i] = updatedWaypoint;
//                             }
//                             Debug.Log($"[GoogleMapsService] Fetched elevation for {results.Count} waypoints (batch {batchStart / maxLocationsPerRequest + 1})");
//                         }
//                     }
//                     else
//                     {
//                         Debug.LogWarning($"[GoogleMapsService] Elevation API status: {status}");
//                     }
//                 }
//                 catch (Exception ex)
//                 {
//                     Debug.LogWarning($"[GoogleMapsService] Elevation parse error: {ex.Message}");
//                 }
//             }
//         }
//     }
// }

// /// <summary>
// /// Represents a complete navigation route
// /// </summary>
// [Serializable]
// public class NavigationRoute
// {
//     public List<GeoLocation> Waypoints;
//     public List<NavigationStep> Steps;
//     public float TotalDistanceMeters;
//     public float TotalDurationSeconds;
//     public string StartAddress;
//     public string EndAddress;

//     public string FormattedDistance
//     {
//         get
//         {
//             if (TotalDistanceMeters >= 1000)
//                 return $"{TotalDistanceMeters / 1000f:F1} km";
//             return $"{TotalDistanceMeters:F0} m";
//         }
//     }

//     public string FormattedDuration
//     {
//         get
//         {
//             int minutes = Mathf.RoundToInt(TotalDurationSeconds / 60f);
//             if (minutes >= 60)
//             {
//                 int hours = minutes / 60;
//                 minutes = minutes % 60;
//                 return $"{hours}h {minutes}min";
//             }
//             return $"{minutes} min";
//         }
//     }
// }

// /// <summary>
// /// Represents a single step in navigation (turn-by-turn)
// /// </summary>
// [Serializable]
// public class NavigationStep
// {
//     public GeoLocation StartLocation;
//     public GeoLocation EndLocation;
//     public List<GeoLocation> PathPoints;
//     public string Instruction;
//     public string Maneuver;
//     public float DistanceMeters;
//     public float DurationSeconds;

//     public string FormattedDistance
//     {
//         get
//         {
//             if (DistanceMeters >= 1000)
//                 return $"{DistanceMeters / 1000f:F1} km";
//             return $"{DistanceMeters:F0} m";
//         }
//     }
}