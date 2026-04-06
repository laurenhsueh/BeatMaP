using UnityEngine;
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using Firebase.Extensions;
using TMPro;

public class ReceiveFromDB : MonoBehaviour
{
    public static ReceiveFromDB Instance { get; private set; }

    FirebaseAuth auth;
    FirebaseFirestore db;
    ListenerRegistration listener;

    public TextMeshPro output;
    public GeoLocation CurrentLocation { get; private set; }
    public bool HasLocation { get; private set; }
    public float Heading { get; private set; } = -1f;
    public bool HasHeading => Heading >= 0f;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    void Start()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Result != DependencyStatus.Available)
            {
                Debug.LogError("Firebase not ready");
                return;
            }

            auth = FirebaseAuth.DefaultInstance;
            db = FirebaseFirestore.DefaultInstance;

            auth.SignInAnonymouslyAsync().ContinueWithOnMainThread(authTask =>
            {
                if (authTask.IsCompleted)
                {
                    StartListening();
                    Debug.Log("User: " + auth.CurrentUser.UserId);
                }
                else
                {
                    Debug.LogError("Auth failed");
                }
            });
        });
    }

    void StartListening()
    {
        listener = db.Collection("iya").Document("geoloc")
            .Listen(snapshot =>
        {
            if (snapshot.Exists)
            {
                double lat = snapshot.GetValue<double>("lat");
                double lon = snapshot.GetValue<double>("lon");
                float heading = snapshot.TryGetValue("heading", out float h) ? h : -1f;

                CurrentLocation = new GeoLocation
                {
                    Latitude = lat,
                    Longitude = lon,
                    Altitude = 0f,
                    Accuracy = 1f,
                    Timestamp = System.DateTime.UtcNow
                };
                HasLocation = CurrentLocation.IsValid;
                Heading = heading;

                if (LocationManager.Instance != null && HasLocation)
                {
                    LocationManager.Instance.SetDebugLocation(lat, lon, heading);
                }

                if (output != null)
                {
                    output.text = "Lat: " + lat + "\nLon: " + lon
                        + (heading >= 0f ? "\nHeading: " + heading.ToString("F1") + "°" : "");
                }
            }
            else
            {
                Debug.Log("No data yet");
            }
        });
    }

    void OnDestroy()
    {
        if (listener != null)
            listener.Stop();

        if (Instance == this)
        {
            Instance = null;
        }
    }
}