using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using MusicVisualizer;

public class DisplayVisuals : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private string jsonFileName = "song_data.json";

    [SerializeField] private DominanceList dominanceList;
    [SerializeField] private ArrowMovement vocalsArrowMovement;
    [SerializeField] private ArrowMovement instrumentalArrowMovement;

    [SerializeField] private BassTrack bassVisual;
    [SerializeField] private DrumsTrack drumsVisual;
    [SerializeField] private GuitarTrack guitarVisual;

    private bool hasPreviousVocalsFrequency;
    private float previousVocalsFrequency;
    private bool hasPreviousVocalsAmplitude;
    private float previousVocalsAmplitude;

    private bool hasPreviousInstrumentalFrequency;
    private float previousInstrumentalFrequency;
    private bool hasPreviousInstrumentalAmplitude;
    private float previousInstrumentalAmplitude;

    // private void Start()
    // {
    //     SongData songData = SongJsonParser.Load(jsonFileName);
    //     if (songData == null) return;

    //     List<(float time, SongFrame frame)> frames = SongJsonParser.ToSortedFrameList(songData);
    //     StartCoroutine(PlayFrames(frames));
    // }

    private void Start()
    {
        StartCoroutine(SongJsonParser.Load(jsonFileName, OnSongLoaded));
    }

    private void OnSongLoaded(SongData songData)
    {
        if (songData == null) return;

        List<(float time, SongFrame frame)> frames = SongJsonParser.ToSortedFrameList(songData);
        StartCoroutine(PlayFrames(frames));
    }

    private IEnumerator PlayFrames(List<(float time, SongFrame frame)> frames)
    {
        audioSource.Play();

        foreach (var (time, frame) in frames)
        {
            yield return new WaitUntil(() => audioSource.time >= time);

            List<TrackDominance> ranked = dominanceList.BuildFromFrame(frame);
            UpdateArrowYOffsets(frame);
            ProcessDominanceList(ranked);
        }
    }

    private bool EnsureArrowMovements()
    {
        if (vocalsArrowMovement != null && instrumentalArrowMovement != null)
        {
            return true;
        }

        ArrowMovement[] arrows = FindObjectsByType<ArrowMovement>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        if (arrows.Length == 0)
        {
            return false;
        }

        if (vocalsArrowMovement == null)
        {
            vocalsArrowMovement = arrows[0];
        }

        if (instrumentalArrowMovement == null)
        {
            for (int i = 0; i < arrows.Length; i++)
            {
                if (arrows[i] != vocalsArrowMovement)
                {
                    instrumentalArrowMovement = arrows[i];
                    break;
                }
            }
        }

        return vocalsArrowMovement != null && instrumentalArrowMovement != null;
    }

    private void UpdateArrowYOffsets(SongFrame frame)
    {
        if (!EnsureArrowMovements() || frame == null)
        {
            return;
        }

        UpdateArrowYOffsetFromTrack(
            frame.GetTrack("vocals"),
            vocalsArrowMovement,
            ref hasPreviousVocalsFrequency,
            ref previousVocalsFrequency,
            ref hasPreviousVocalsAmplitude,
            ref previousVocalsAmplitude);

        UpdateArrowYOffsetFromTrack(
            frame.GetTrack("instrumental"),
            instrumentalArrowMovement,
            ref hasPreviousInstrumentalFrequency,
            ref previousInstrumentalFrequency,
            ref hasPreviousInstrumentalAmplitude,
            ref previousInstrumentalAmplitude);
    }

    private static void UpdateArrowYOffsetFromTrack(
        TrackFrameData track,
        ArrowMovement arrow,
        ref bool hasPreviousFrequency,
        ref float previousFrequency,
        ref bool hasPreviousAmplitude,
        ref float previousAmplitude)
    {
        if (track == null || arrow == null)
        {
            return;
        }

        float frequencyDelta = 0f;
        if (track.Frequency != null)
        {
            float currentFrequency = track.Frequency.DominantFrequencyHz;
            frequencyDelta = hasPreviousFrequency
                ? currentFrequency - previousFrequency
                : 0f;

            previousFrequency = currentFrequency;
            hasPreviousFrequency = true;
        }

        float amplitudeDelta = 0f;
        if (track.Amplitude != null)
        {
            float currentAmplitude = track.Amplitude.RmsEnergy;
            amplitudeDelta = hasPreviousAmplitude
                ? currentAmplitude - previousAmplitude
                : 0f;

            previousAmplitude = currentAmplitude;
            hasPreviousAmplitude = true;
        }

        arrow.ChangeYOffset(frequencyDelta, amplitudeDelta);
    }

    private void ProcessDominanceList(List<TrackDominance> ranked)
    {
        if (ranked == null || ranked.Count == 0) return;

        for (int i = 0; i < Mathf.Min(3, ranked.Count); i++)
        {
            string track = ranked[i].TrackName.ToLower();

            if (track == "bass") bassVisual.Spawn();
            else if (track == "drums") drumsVisual.Spawn();
            else if (track == "guitar") guitarVisual.Spawn();
        }
    }
}

// problems arose: oscillation to display visuals made me need to change void to gameobject; coroutine that controls oscillation on base behavior script conflicts with coroutine that syncs to music --> made music silent