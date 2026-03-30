// This program defines the shape of the data in C# and provides a way to look up tracks

using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace MusicVisualizer
{
    [System.Serializable]
    public class AmplitudeData
    {
        [JsonProperty("rms_energy")]
        public float RmsEnergy;

        [JsonProperty("peak_amplitude")]
        public float PeakAmplitude;
    }

    [System.Serializable]
    public class FrequencyData
    {
        [JsonProperty("dominant_frequency_hz")]
        public float DominantFrequencyHz;

        [JsonProperty("spectral_centroid_hz")]
        public float SpectralCentroidHz;
    }

    [System.Serializable]
    public class TrackFrameData
    {
        [JsonProperty("amplitude")]
        public AmplitudeData Amplitude;

        [JsonProperty("frequency")]
        public FrequencyData Frequency;

        [JsonProperty("weight")]
        public float Weight;
    }

    [System.Serializable]
    public class SongFrame
    {
        // Captures all track keys dynamically — works with "ng_bass", "bass", anything
        [JsonExtensionData]
        public Dictionary<string, Newtonsoft.Json.Linq.JToken> RawTracks { get; set; } = new();

        private Dictionary<string, TrackFrameData> _parsedCache;

        /// <summary>
        /// Retrieve a track by searching for a key that contains the instrument name.
        /// e.g. GetTrack("bass") matches "ng_bass", "bass", "bass_track", etc.
        /// Case-insensitive.
        /// </summary>
        public TrackFrameData GetTrack(string instrumentName)
        {
            if (_parsedCache == null)
                BuildCache();

            string key = _parsedCache.Keys
                .FirstOrDefault(k => k.IndexOf(instrumentName, System.StringComparison.OrdinalIgnoreCase) >= 0);

            return key != null ? _parsedCache[key] : null;
        }

        /// <summary>
        /// Retrieve a track by its exact JSON key.
        /// e.g. GetTrackExact("ng_bass")
        /// </summary>
        public TrackFrameData GetTrackExact(string exactKey)
        {
            if (_parsedCache == null)
                BuildCache();

            return _parsedCache.TryGetValue(exactKey, out var data) ? data : null;
        }

        /// <summary>Returns all track keys found in this frame.</summary>
        public IEnumerable<string> GetAllTrackKeys()
        {
            if (_parsedCache == null)
                BuildCache();

            return _parsedCache.Keys;
        }

        private void BuildCache()
        {
            _parsedCache = new Dictionary<string, TrackFrameData>(System.StringComparer.OrdinalIgnoreCase);

            if (RawTracks == null) return;

            foreach (var kvp in RawTracks)
            {
                var trackData = kvp.Value.ToObject<TrackFrameData>();
                if (trackData != null)
                    _parsedCache[kvp.Key] = trackData;
            }
        }
    }

    [System.Serializable]
    public class SongData
    {
        public Dictionary<string, SongFrame> frames;
    }
}