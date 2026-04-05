using PianoTrainer2.Models;
using System.Collections.Generic;
using System.Linq;

namespace PianoTrainer2.Services
{
    public static class SongLibrary
    {
        private static BuiltInSong Embedded(Song song, string composer) =>
            new() { Title = song.Title, Composer = composer, Difficulty = "Absolute Beginner", EmbeddedSong = song };

        public static IReadOnlyList<BuiltInSong> Songs { get; } =
        [
            ..EmbeddedSongLibrary.Songs.Select(s => Embedded(s, "Traditional")),
            new BuiltInSong { Title = "Für Elise",           Composer = "Beethoven", Difficulty = "Beginner",     FileName = "fur_elise.mid",          Url = "https://bitmidi.com/uploads/28362.mid" },
            new BuiltInSong { Title = "Ode to Joy",          Composer = "Beethoven", Difficulty = "Beginner",     FileName = "ode_to_joy.mid",          Url = "https://bitmidi.com/uploads/81798.mid" },
            new BuiltInSong { Title = "Minuet in G",         Composer = "Bach",      Difficulty = "Beginner",     FileName = "minuet_g.mid",            Url = "https://bitmidi.com/uploads/28316.mid" },
            new BuiltInSong { Title = "Twinkle Twinkle",     Composer = "Traditional",Difficulty = "Beginner",   FileName = "twinkle.mid",             Url = "https://bitmidi.com/uploads/35393.mid" },
            new BuiltInSong { Title = "Canon in D",          Composer = "Pachelbel", Difficulty = "Intermediate", FileName = "canon_d.mid",             Url = "https://bitmidi.com/uploads/21743.mid" },
            new BuiltInSong { Title = "Moonlight Sonata",    Composer = "Beethoven", Difficulty = "Intermediate", FileName = "moonlight_sonata.mid",    Url = "https://bitmidi.com/uploads/16752.mid" },
            new BuiltInSong { Title = "Rondo Alla Turca",    Composer = "Mozart",    Difficulty = "Intermediate", FileName = "rondo_alla_turca.mid",    Url = "https://bitmidi.com/uploads/33638.mid" },
            new BuiltInSong { Title = "Nocturne Op.9 No.2",  Composer = "Chopin",    Difficulty = "Advanced",     FileName = "nocturne_op9_2.mid",      Url = "https://bitmidi.com/uploads/32490.mid" },
            new BuiltInSong { Title = "Prelude BWV 847a",    Composer = "Bach",      Difficulty = "Advanced",     FileName = "prelude_bwv847a.mid",     Url = "https://bitmidi.com/uploads/31816.mid" },
        ];
    }
}
