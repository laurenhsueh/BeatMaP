using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json.Linq;

namespace MusicVisualizer
{
    public static class SongJsonParser
    {
        public static IEnumerator Load(string fileName, System.Action<SongData> onComplete)
        {
            string path = System.IO.Path.Combine(Application.streamingAssetsPath, fileName);

#if UNITY_EDITOR
            if (!System.IO.File.Exists(path))
            {
                Debug.LogError($"[SongJsonParser] File not found: {path}");
                onComplete(null);
                yield break;
            }

            string json = System.IO.File.ReadAllText(path);
            onComplete(Parse(json));
#else
            using (UnityWebRequest request = UnityWebRequest.Get(path))
            {
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"[SongJsonParser] Failed to load: {request.error}");
                    onComplete(null);
                    yield break;
                }

                onComplete(Parse(request.downloadHandler.text));
            }
#endif
        }

        public static SongData Parse(string json)
        {
            var songData = new SongData
            {
                frames = new Dictionary<string, SongFrame>()
            };

            var root = JObject.Parse(json);

            foreach (var timestampProp in root.Properties())
            {
                string key = timestampProp.Name;
                SongFrame frame = timestampProp.Value.ToObject<SongFrame>();
                songData.frames[key] = frame;
            }

            Debug.Log($"[SongJsonParser] Parsed {songData.frames.Count} frames.");
            return songData;
        }

        public static List<(float time, SongFrame frame)> ToSortedFrameList(SongData songData)
        {
            var list = new List<(float, SongFrame)>();

            foreach (var kvp in songData.frames)
            {
                if (float.TryParse(kvp.Key,
                        System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out float t))
                {
                    list.Add((t, kvp.Value));
                }
            }

            list.Sort((a, b) => a.Item1.CompareTo(b.Item1));
            return list;
        }
    }
}




// // This program turns JSON data into a usable C# format.

// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;
// using UnityEngine.Networking;
// using Newtonsoft.Json;
// using Newtonsoft.Json.Linq;

// namespace MusicVisualizer
// {
//     public static class SongJsonParser
//     {
//         // Load and parse the song JSON from a StreamingAssets path, JSON files stored in a special folder called StreamingAssets. (e.g. fileName = "song_data.json"  →  StreamingAssets/song_data.json)
//         public static SongData Load(string fileName)
//         {
//             string path = System.IO.Path.Combine(Application.streamingAssetsPath, fileName);

//             if (!System.IO.File.Exists(path))
//             {
//                 Debug.LogError($"[SongJsonParser] File not found: {path}");
//                 return null;
//             }

//             string json = System.IO.File.ReadAllText(path);
//             return Parse(json);
//         }


//         // Parse a raw JSON string into SongData object. JObject handles the dynamic timestamp keys ("0.0", "1.0" ...) cleanly. Sorts into time order.
//         public static SongData Parse(string json)
//         {
//             var songData = new SongData
//             {
//                 frames = new Dictionary<string, SongFrame>()
//             };

//             var root = JObject.Parse(json);

//             foreach (var timestampProp in root.Properties())
//             {
//                 string key = timestampProp.Name;
//                 SongFrame frame = timestampProp.Value.ToObject<SongFrame>();
//                 songData.frames[key] = frame;
//             }

//             Debug.Log($"[SongJsonParser] Parsed {songData.frames.Count} frames.");
//             return songData;
//         }

//         // Convert the string-keyed dictionary to a sorted float-keyed list so the main code can step through frames in order.
//         public static List<(float time, SongFrame frame)> ToSortedFrameList(SongData songData)
//         {
//             var list = new List<(float, SongFrame)>();

//             foreach (var kvp in songData.frames)
//             {
//                 if (float.TryParse(kvp.Key,
//                         System.Globalization.NumberStyles.Float,
//                         System.Globalization.CultureInfo.InvariantCulture,
//                         out float t))
//                 {
//                     list.Add((t, kvp.Value));
//                 }
//             }

//             list.Sort((a, b) => a.Item1.CompareTo(b.Item1));
//             return list;
//         }
//     }
// }