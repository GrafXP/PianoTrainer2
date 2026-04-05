using PianoTrainer2.Models;
using System.Collections.Generic;

namespace PianoTrainer2.Services
{
    public static class SongLibrary
    {
        public static IReadOnlyList<BuiltInSong> Songs { get; } =
        [
            new BuiltInSong { Title = "Hot Cross Buns",      Composer = "Traditional", Difficulty = "Absolute Beginner", FileName = "hot_cross_buns.mid",     Url = "https://bitmidi.com/uploads/75861.mid" },
            new BuiltInSong { Title = "Mary Had a Little Lamb", Composer = "Traditional", Difficulty = "Absolute Beginner", FileName = "mary_little_lamb.mid", Url = "https://bitmidi.com/uploads/75862.mid" },
            new BuiltInSong { Title = "Jingle Bells (Simple)", Composer = "Traditional", Difficulty = "Absolute Beginner", FileName = "jingle_bells_simple.mid", Url = "https://bitmidi.com/uploads/36322.mid" },
            new BuiltInSong { Title = "Happy Birthday",      Composer = "Traditional", Difficulty = "Absolute Beginner", FileName = "happy_birthday.mid",     Url = "https://bitmidi.com/uploads/36231.mid" },
            new BuiltInSong { Title = "Ode to Joy (Right Hand)", Composer = "Beethoven", Difficulty = "Absolute Beginner", FileName = "ode_to_joy_rh.mid",   Url = "https://bitmidi.com/uploads/81798.mid" },
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
