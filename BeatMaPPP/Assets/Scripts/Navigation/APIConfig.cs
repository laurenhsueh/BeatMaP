using UnityEngine;

/// ScriptableObject to store API keys.
/// Create via: Right-click in Project > Create > Config > API Config

[CreateAssetMenu(fileName = "APIConfig", menuName = "Config/API Config")]
public class APIConfig : ScriptableObject
{
    [Header("Google Maps")]
    [SerializeField] private string googleMapsApiKey = "";
    public string GoogleMapsApiKey => googleMapsApiKey;
    public bool HasGoogleMapsKey => !string.IsNullOrEmpty(googleMapsApiKey);
}
