using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using MusicVisualizer;

public class DisplayVisuals : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private string jsonFileName = "song_data.json";

    [SerializeField] private DominanceList dominanceList;

    [SerializeField] private BassTrack bassVisual;
    [SerializeField] private DrumsTrack drumsVisual;
    [SerializeField] private GuitarTrack guitarVisual;
    [SerializeField] private InstrumentalTrack instrumentalVisual;
    [SerializeField] private OtherTrack otherVisual;
    [SerializeField] private PianoTrack pianoVisual;
    [SerializeField] private VocalsTrack vocalsVisual;

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
            ProcessDominanceList(ranked);
        }
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
            else if (track == "instrumental") instrumentalVisual.Spawn();
            else if (track == "other") otherVisual.Spawn();
            else if (track == "piano") pianoVisual.Spawn();
            else if (track == "vocals") vocalsVisual.Spawn();
        }
    }
}

// problems arose: oscillation to display visuals made me need to change void to gameobject; coroutine that controls oscillation on base behavior script conflicts with coroutine that syncs to music --> made music silent