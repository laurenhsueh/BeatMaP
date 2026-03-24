# Initialize dictionary
# Pull audio files from directory
# For loop that loops through every track in the directory
# Grab the length of the song (inner for loop); for n in range(song_length)
# Analyze music using librosa - frequency, amplitude, weight (calculations to determine this)
    # Add to dictionary
# Convert to JSON (dump)
# Return JSON file (one file for each song)

# Timestamp --> track type --> details of the track at a specific timestamp (convert to timestamp used by unity)


import json
import librosa
import numpy as np
import os


def analyze_multitrack_audio(directory: str, output_path: str = "output.json") -> dict:
    """
    Analyzes multitrack audio files in a directory and returns a JSON file
    with per-second audio features for each track.

    Args:
        directory: Path to folder containing audio tracks
        output_path: Path for the output JSON file

    Returns:
        The song analysis dictionary
    """

    # 1. Initialize dictionary
    song_analysis = {}

    # 2. Pull audio files from directory
    supported_formats = (".wav", ".mp3", ".flac", ".ogg", ".aiff")
    audio_files = [
        f for f in os.listdir(directory)
        if f.lower().endswith(supported_formats)
    ]

    if not audio_files:
        raise ValueError(f"No supported audio files found in: {directory}")

    # 3. Loop through every track in the directory
    for filename in audio_files:
        track_name = os.path.splitext(filename)[0]
        filepath = os.path.join(directory, filename)

        print(f"Analyzing track: {track_name}...")

        y, sr = librosa.load(filepath, mono=True)
        song_length = librosa.get_duration(y=y, sr=sr)
        samples_per_second = sr

        # 4. Inner loop: iterate over each second of the song
        for n in range(int(song_length)):
            timestamp_key = round(float(n), 1)

            if timestamp_key not in song_analysis:
                song_analysis[timestamp_key] = {}

            start_sample = n * samples_per_second
            end_sample = start_sample + samples_per_second
            segment = y[start_sample:end_sample]

            if len(segment) == 0:
                continue

            # 5. Analyze audio features

            # Amplitude
            rms = float(np.sqrt(np.mean(segment ** 2)))
            peak_amplitude = float(np.max(np.abs(segment)))

            # Frequency
            fft = np.fft.rfft(segment)
            fft_magnitudes = np.abs(fft)
            freqs = np.fft.rfftfreq(len(segment), d=1.0 / sr)
            dominant_frequency = float(freqs[np.argmax(fft_magnitudes)])
            # Average for the whole second
            spectral_centroid = float(np.mean(librosa.feature.spectral_centroid(y=segment, sr=sr)))

            # 6. Add to dictionary under timestamp -> track
            song_analysis[timestamp_key][track_name] = {
                "amplitude": {
                    "rms_energy": rms,
                    "peak_amplitude": peak_amplitude
                },
                "frequency": {
                    "dominant_frequency_hz": dominant_frequency,
                    "spectral_centroid_hz": spectral_centroid
                },
                "weight": None  # Placeholder; calculated in second pass below
            }

    # 7. Second pass: compute relative dominance weight per track at each timestamp
    for timestamp_key, tracks in song_analysis.items():
        total_rms = sum(
            data["amplitude"]["rms_energy"]
            for data in tracks.values()
        )

        for track, data in tracks.items():
            dominance = (
                round(data["amplitude"]["rms_energy"] / total_rms, 4)
                if total_rms > 0 else 0.0
            )
            song_analysis[timestamp_key][track]["weight"] = dominance

    # 8. Convert to JSON and write output file
    with open(output_path, "w") as f:
        json.dump(song_analysis, f, indent=2)

    print(f"\nAnalysis complete. JSON saved to: {output_path}")
    return song_analysis


# --- Entry point ---
if __name__ == "__main__":
    result1 = analyze_multitrack_audio(
        directory="./swv.mp3",
        output_path="swv_analysis.json"
    )
    result2 = analyze_multitrack_audio(
        directory="./never_gonna.mp3",
        output_path="never_gonna_analysis.json"
    )


# Outputs in the JSON
# rms_energy (root mean square): avg loudness of track over the entire second
# peak_amplitude: single loudest moment within that second
# dominant_frequency_hz: single highest frequency in that second
# spectral_centroid_hz: weighted avg of all frequencies in that second (low number = lower pitch, high number = higher pitch)
# weight: relative loudness dominance compared to other tracks at the same timestamp; which track is driving the song (higher number = more dominant)

# Spectral centroid defined
# Low value (200-2000 Hz): energy of track is concentrated in lower frequencies
# Mid value (2000-5000 Hz): vocals in natural speaking/singing range, sounds in this range feel prominent
# High value (5000+ Hz): energy of track is concentrated in higher frequencies