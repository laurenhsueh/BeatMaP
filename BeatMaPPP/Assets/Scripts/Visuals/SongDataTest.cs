// // using UnityEngine;
// // using MusicVisualizer;

// // public class SongDataTester : MonoBehaviour
// // {
// //     [SerializeField] private string jsonFileName = "song_data.json";

// //     private void Start()
// //     {
// //         // 1. Load and parse the JSON
// //         SongData songData = SongJsonParser.Load(jsonFileName);

// //         if (songData == null)
// //         {
// //             Debug.LogError("[Tester] SongData is null — check your file name and StreamingAssets folder.");
// //             return;
// //         }

// //         // 2. Convert to sorted frame list
// //         var frames = SongJsonParser.ToSortedFrameList(songData);
// //         Debug.Log($"[Tester] Total frames parsed: {frames.Count}");

// //         // 3. Print the first 3 frames so you can verify the data looks right
// //         int previewCount = Mathf.Min(3, frames.Count);
// //         for (int i = 0; i < previewCount; i++)
// //         {
// //             var (time, frame) = frames[i];
// //             Debug.Log($"[Tester] ---- Frame at t={time}s ----");

// //             // Print all track keys found in this frame
// //             foreach (string key in frame.GetAllTrackKeys())
// //             {
// //                 TrackFrameData track = frame.GetTrackExact(key);
// //                 Debug.Log($"[Tester]   {key} | weight={track.Weight} | " +
// //                           $"rms={track.Amplitude.RmsEnergy} | " +
// //                           $"peak={track.Amplitude.PeakAmplitude} | " +
// //                           $"dominant_hz={track.Frequency.DominantFrequencyHz} | " +
// //                           $"centroid_hz={track.Frequency.SpectralCentroidHz}");
// //             }
// //         }

// //         // 4. Test GetTrack() flexible lookup
// //         Debug.Log("[Tester] ---- Testing flexible GetTrack() lookup ----");
// //         var firstFrame = frames[1].frame; // use frame at t=1.0 where data is non-zero
// //         string[] instruments = { "bass", "drums", "guitar", "instrumental", "other", "piano", "vocals" };
// //         foreach (string instrument in instruments)
// //         {
// //             TrackFrameData track = firstFrame.GetTrack(instrument);
// //             if (track != null)
// //                 Debug.Log($"[Tester] GetTrack(\"{instrument}\") ✓ weight={track.Weight}");
// //             else
// //                 Debug.LogWarning($"[Tester] GetTrack(\"{instrument}\") ✗ not found — check your JSON keys");
// //         }
// //     }
// // }

// using System.Collections.Generic;
// using UnityEngine;
// using MusicVisualizer;

// public class SongDataTester : MonoBehaviour
// {
//     [SerializeField] private string jsonFileName = "song_data.json";

//     [Tooltip("How many frames to print dominance lists for")]
//     [SerializeField] private int previewFrameCount = 5;

//     private void Start()
//     {
//         SongData songData = SongJsonParser.Load(jsonFileName);

//         if (songData == null)
//         {
//             Debug.LogError("[Tester] SongData is null — check your file name and StreamingAssets folder.");
//             return;
//         }

//         var frames = SongJsonParser.ToSortedFrameList(songData);
//         Debug.Log($"[Tester] Total frames parsed: {frames.Count}");

//         DominanceList dominanceList = gameObject.AddComponent<DominanceList>();

//         int count = Mathf.Min(previewFrameCount, frames.Count);
//         for (int i = 0; i < count; i++)
//         {
//             var (time, frame) = frames[i];

//             dominanceList.BuildFromFrame(frame);

//             // Build formatted string: [vocals, instrumental, guitar, ...]
//             var names = dominanceList.Current.ConvertAll(t => t.TrackName);
//             string formatted = "[" + string.Join(", ", names) + "]";
//             Debug.Log(formatted);
//         }
//     }
// }