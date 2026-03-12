using UnityEngine;
using System.Collections;
using TMPro;

// beat it

public class GetLocation : MonoBehaviour
{
    public TextMeshPro output; // Reference to a UI Text element
    readonly WaitForSeconds waitTime = new(5); // Time to wait between pulling location data

    void Start()
    {
        StartCoroutine(StartGeoLoc());
    }

    IEnumerator StartGeoLoc()
    {
        output.text = "Starting Location services...";
        // First, check if user has location service enabled
        if (!Input.location.isEnabledByUser)
        {
            output.text = "location not enabled by user";
            yield break;
        }
        // Start service before querying location
        Input.location.Start(1, 1); // set the accuracy and change for an update to 1 meter

        int maxWait = 20;
        while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
        {
            yield return new WaitForSeconds(1);
            maxWait--;
        }

        if (maxWait < 1)
        {
            output.text = "Timed out";
            yield break;
        }

        if (Input.location.status == LocationServiceStatus.Failed)
        {
            output.text = "Unable to determine device location";
            yield break;
        }
        else
        {
            // Access granted and location value could be retrieved
            //print("Location: " + Input.location.lastData.latitude + " " + Input.location.lastData.longitude + " " + Input.location.lastData.altitude + " " + Input.location.lastData.horizontalAccuracy + " " + Input.location.lastData.timestamp);
            StartCoroutine(UpdateLoc());
        }
    }

    IEnumerator UpdateLoc()
    {
        while (true)
        {
            yield return waitTime; // waitTime set to 5sec.
            output.text = "Latitude: " + Input.location.lastData.latitude + "\nLongitude: " + Input.location.lastData.longitude;
        }
    }
}