using System;
using UnityEngine;
using System.Collections;
using TMPro;

public class GetLocation : MonoBehaviour
{
    public TextMeshPro output; // Reference to a UI Text element
    readonly WaitForSeconds waitTime = new(1); // Time to wait between pulling location data
    private int pullcount = 0;
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
            double ts = Input.location.lastData.timestamp;
            string tsText;
            if (ts > 1000000000) // likely Unix epoch seconds
            {
                long unix = (long)ts;
                tsText = DateTimeOffset.FromUnixTimeSeconds(unix).ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
            }
            else // treat as seconds since device start or app start
            {
                TimeSpan span = TimeSpan.FromSeconds(ts);
                tsText = string.Format("{0:00}:{1:00}:{2:00}.{3:000}", span.Hours, span.Minutes, span.Seconds, span.Milliseconds);
            }
            pullcount++;
            output.text = "Latitude: " + Input.location.lastData.latitude + "\nLongitude: " + Input.location.lastData.longitude + "\nTimestamp: " + tsText + "\n" + "Pull count: " + pullcount;
        }
    }
}