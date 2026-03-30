using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;

public class PlaneSpawnManager : MonoBehaviour
{
    public static PlaneSpawnManager Instance { get; private set; }
    public bool PlanesReady { get; private set; } = false;

    private ARPlaneManager _planeManager;

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
        _planeManager = FindAnyObjectByType<ARPlaneManager>();

        if (_planeManager == null)
        {
            Debug.LogWarning("PlaneSpawnManager: ARPlaneManager not found.");
            return;
        }

        _planeManager.trackablesChanged.AddListener(OnPlanesChanged);
        Debug.Log("PlaneSpawnManager: Listening for planes.");
    }

    private void OnPlanesChanged(ARTrackablesChangedEventArgs<ARPlane> args)
    {
        if (PlanesReady) return;

        foreach (ARPlane plane in _planeManager.trackables)
        {
            if (plane == null) continue;
            if (plane.trackingState != TrackingState.Tracking) continue;

            Debug.Log($"PlaneSpawnManager: Plane found - alignment={plane.alignment}");
            PlanesReady = true;
            break;
        }
    }

    private void OnDestroy()
    {
        if (_planeManager != null)
            _planeManager.trackablesChanged.RemoveListener(OnPlanesChanged);
    }

    public bool TryGetSpawnPoint(out Vector3 spawnPos)
    {
        spawnPos = Vector3.zero;

        if (_planeManager == null) return false;

        List<ARPlane> planes = new();
        foreach (ARPlane plane in _planeManager.trackables)
        {
            if (plane == null) continue;
            if (plane.trackingState != TrackingState.Tracking) continue;
            Debug.Log($"PlaneSpawnManager: Plane alignment={plane.alignment}");
            planes.Add(plane);
        }

        if (planes.Count == 0) return false;

        ARPlane chosen = planes[Random.Range(0, planes.Count)];
        Vector2 halfExtents = chosen.extents * 0.5f;
        float randomX = Random.Range(-halfExtents.x, halfExtents.x);
        float randomZ = Random.Range(-halfExtents.y, halfExtents.y);

        spawnPos = chosen.transform.TransformPoint(new Vector3(randomX, 0f, randomZ));
        return true;
    }
}