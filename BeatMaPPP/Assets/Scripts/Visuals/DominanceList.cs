using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MusicVisualizer
{
    public class TrackDominance
    {
        public string TrackName;
        public float Weight;

        public TrackDominance(string trackName, float weight)
        {
            TrackName = trackName;
            Weight = weight;
        }

        public override string ToString() => $"{TrackName}: {Weight:F4}";
    }

    public class DominanceList : MonoBehaviour
    {
        public List<TrackDominance> Current { get; private set; } = new();

        [Tooltip("Tracks below this weight are excluded from rare visual appearance")]
        [SerializeField] private float weightThreshold = 0.01f;

        [Tooltip("Chance (0-1) of a rare visual appearance occurring")]
        [SerializeField] private float rareChance = 0.3f;

        private static readonly string[] _instrumentNames =
        {
            "bass", "drums", "guitar", "instrumental", "other", "piano", "vocals"
        };

        /// <summary>
        /// Builds the dominance list from a SongFrame, applies RareVisualAppearance,
        /// and returns the final ordered list.
        /// </summary>
        public List<TrackDominance> BuildFromFrame(SongFrame frame)
        {
            var list = new List<TrackDominance>();

            foreach (string instrument in _instrumentNames)
            {
                TrackFrameData track = frame.GetTrack(instrument);

                if (track != null)
                    list.Add(new TrackDominance(instrument, track.Weight));
                else
                    Debug.LogWarning($"[DominanceList] Could not find track for '{instrument}' in this frame.");
            }

            // Sort greatest to least by weight
            list.Sort((a, b) => b.Weight.CompareTo(a.Weight));

            // Apply rare visual appearance before finalizing
            list = RareVisualAppearance(list);

            Current = list;
            return list;
        }

        /// <summary>
        /// 30% chance to pick a random track from index 3 onwards (that is above
        /// the weight threshold) and move it to the front of the list.
        /// </summary>
        private List<TrackDominance> RareVisualAppearance(List<TrackDominance> list)
        {
            // Need at least 4 entries for indices 3+ to exist
            if (list.Count <= 3)
                return list;

            // Roll for rare appearance
            if (Random.value > rareChance)
                return list;

            // Gather eligible candidates: index 3 or later, above weight threshold
            var candidates = list
                .Skip(3)
                .Where(t => t.Weight >= weightThreshold)
                .ToList();

            if (candidates.Count == 0)
            {
                Debug.Log("[DominanceList] Rare roll succeeded but no eligible candidates above threshold.");
                return list;
            }

            // Pick a random candidate
            TrackDominance chosen = candidates[Random.Range(0, candidates.Count)];

            Debug.Log($"[DominanceList] Rare visual appearance! '{chosen.TrackName}' promoted to front.");

            // Remove from its current position and insert at the front
            list.Remove(chosen);
            list.Insert(0, chosen);

            return list;
        }

        /// <summary>
        /// Returns only tracks above the weight threshold.
        /// </summary>
        public List<TrackDominance> GetActiveTracks()
        {
            return Current.Where(t => t.Weight >= weightThreshold).ToList();
        }

        /// <summary>
        /// Returns the name of the track at the front of the list.
        /// </summary>
        public string GetDominantTrack()
        {
            return Current.Count > 0 ? Current[0].TrackName : null;
        }

        /// <summary>
        /// Logs the current dominance list in [vocals, instrumental, guitar, ...] format.
        /// </summary>
        public void LogCurrent()
        {
            var names = Current.ConvertAll(t => t.TrackName);
            Debug.Log("[" + string.Join(", ", names) + "]");
        }
    }
}