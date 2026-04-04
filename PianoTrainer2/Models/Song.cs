using System.Collections.Generic;

namespace PianoTrainer2.Models
{
    public class Song
    {
        public string Title { get; init; } = "";
        public IReadOnlyList<SongNote> Notes { get; init; } = [];
        public double TotalDurationMs { get; init; }
        /// <summary>Microseconds per beat at the start of the song (first tempo event).</summary>
        public long InitialUsPerBeat { get; init; } = 500_000; // default 120 BPM
        public double InitialBpm => 60_000_000.0 / InitialUsPerBeat;
        public double BeatDurationMs => InitialUsPerBeat / 1000.0;
    }
}
