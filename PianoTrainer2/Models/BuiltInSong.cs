namespace PianoTrainer2.Models
{
    public class BuiltInSong
    {
        public string Title { get; init; } = "";
        public string Composer { get; init; } = "";
        public string Url { get; init; } = "";
        public string FileName { get; init; } = "";
        public string Difficulty { get; init; } = "";

        /// <summary>If set, this song is played directly from memory — no download needed.</summary>
        public Song? EmbeddedSong { get; init; }

        public override string ToString() => $"{Title} — {Composer} ({Difficulty})";
    }
}
