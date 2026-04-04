using System.Collections.Generic;

namespace PianoTrainer2.Models
{
    public class Song
    {
        public string Title { get; init; } = "";
        public IReadOnlyList<SongNote> Notes { get; init; } = [];
        public double TotalDurationMs { get; init; }
    }
}
