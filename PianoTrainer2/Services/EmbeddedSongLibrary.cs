using PianoTrainer2.Models;
using System.Collections.Generic;
using System.Linq;

namespace PianoTrainer2.Services
{
    /// <summary>
    /// Hardcoded beginner songs — no download or MIDI file required.
    /// Notes are defined as (midiNote, durationMs) pairs at 90 BPM (one beat = 667ms).
    /// </summary>
    public static class EmbeddedSongLibrary
    {
        // MIDI note numbers: C4=60, D4=62, E4=64, F4=65, G4=67, A4=69, B4=71, C5=72
        //                    G3=55, A3=57, B3=59

        private static Song Build(string title, string difficulty, string composer, IEnumerable<(int note, double dur)> notes)
        {
            var list = new List<SongNote>();
            double t = 0;
            foreach (var (note, dur) in notes)
            {
                list.Add(new SongNote(note, t, dur * 0.9));
                t += dur;
            }
            return new Song { Title = title, Composer = composer, Difficulty = difficulty, Notes = list, TotalDurationMs = t + 500, InitialUsPerBeat = 666_667 };
        }

        // beat durations at 90 BPM
        private const double Q  = 667;   // quarter note
        private const double H  = 1333;  // half note
        private const double DH = 2000;  // dotted half
        private const double W  = 2667;  // whole note
        private const double E  = 333;   // eighth note

        // note constants
        private const int C4 = 60, D4 = 62, E4 = 64, F4 = 65, G4 = 67, A4 = 69, B4 = 71, C5 = 72;
        private const int D5 = 74, E5 = 76, F5 = 77, G5 = 79;
        private const int G3 = 55, A3 = 57, B3 = 59, C3 = 48, D3 = 50, E3 = 52, F3 = 53;
        private const int R  = 0;  // rest (skipped)

        private static Song BuildWithRests(string title, string difficulty, string composer, IEnumerable<(int note, double dur)> notes)
        {
            var list = new List<SongNote>();
            double t = 0;
            foreach (var (note, dur) in notes)
            {
                if (note != R)
                    list.Add(new SongNote(note, t, dur * 0.9));
                t += dur;
            }
            return new Song { Title = title, Composer = composer, Difficulty = difficulty, Notes = list, TotalDurationMs = t + 500, InitialUsPerBeat = 666_667 };
        }

        public static IReadOnlyList<Song> Songs { get; } =
        [
            // ════════════════════════════════════════════════════════════════
            // ABSOLUTE BEGINNER — right hand only, simple short songs
            Build("Hot Cross Buns",         "Absolute Beginner", "Traditional", [
                (E4,Q),(D4,Q),(C4,H),(E4,Q),(D4,Q),(C4,H),
                (C4,E),(C4,E),(C4,E),(C4,E),(D4,E),(D4,E),(D4,E),(D4,E),
                (E4,Q),(D4,Q),(C4,H),
            ]),
            Build("Mary Had a Little Lamb", "Absolute Beginner", "Traditional", [
                (E4,Q),(D4,Q),(C4,Q),(D4,Q),(E4,Q),(E4,Q),(E4,H),
                (D4,Q),(D4,Q),(D4,H),(E4,Q),(G4,Q),(G4,H),
                (E4,Q),(D4,Q),(C4,Q),(D4,Q),(E4,Q),(E4,Q),(E4,Q),(E4,Q),
                (D4,Q),(D4,Q),(E4,Q),(D4,Q),(C4,W),
            ]),
            Build("Twinkle Twinkle",        "Absolute Beginner", "Traditional", [
                (C4,Q),(C4,Q),(G4,Q),(G4,Q),(A4,Q),(A4,Q),(G4,H),
                (F4,Q),(F4,Q),(E4,Q),(E4,Q),(D4,Q),(D4,Q),(C4,H),
                (G4,Q),(G4,Q),(F4,Q),(F4,Q),(E4,Q),(E4,Q),(D4,H),
                (G4,Q),(G4,Q),(F4,Q),(F4,Q),(E4,Q),(E4,Q),(D4,H),
                (C4,Q),(C4,Q),(G4,Q),(G4,Q),(A4,Q),(A4,Q),(G4,H),
                (F4,Q),(F4,Q),(E4,Q),(E4,Q),(D4,Q),(D4,Q),(C4,H),
            ]),
            Build("Ode to Joy (Right Hand)","Absolute Beginner", "Beethoven",   [
                (E4,Q),(E4,Q),(F4,Q),(G4,Q),(G4,Q),(F4,Q),(E4,Q),(D4,Q),
                (C4,Q),(C4,Q),(D4,Q),(E4,Q),(E4,Q),(D4,Q),(D4,H),
                (E4,Q),(E4,Q),(F4,Q),(G4,Q),(G4,Q),(F4,Q),(E4,Q),(D4,Q),
                (C4,Q),(C4,Q),(D4,Q),(E4,Q),(D4,Q),(C4,Q),(C4,H),
            ]),
            Build("Jingle Bells (Chorus)",  "Absolute Beginner", "Traditional", [
                (E4,Q),(E4,Q),(E4,H),(E4,Q),(E4,Q),(E4,H),
                (E4,Q),(G4,Q),(C4,Q),(D4,Q),(E4,W),
                (F4,Q),(F4,Q),(F4,Q),(F4,Q),(F4,Q),(E4,Q),(E4,Q),(E4,Q),
                (E4,Q),(D4,Q),(D4,Q),(E4,Q),(D4,H),(G4,H),
                (E4,Q),(E4,Q),(E4,H),(E4,Q),(E4,Q),(E4,H),
                (E4,Q),(G4,Q),(C4,Q),(D4,Q),(E4,W),
                (F4,Q),(F4,Q),(F4,Q),(F4,Q),(F4,Q),(E4,Q),(E4,Q),(E4,Q),
                (G4,Q),(G4,Q),(F4,Q),(D4,Q),(C4,W),
            ]),

            // ════════════════════════════════════════════════════════════════
            // EASY — longer single-hand melodies
            Build("Ode to Joy (Full)",      "Easy", "Beethoven", [
                (E4,Q),(E4,Q),(F4,Q),(G4,Q),(G4,Q),(F4,Q),(E4,Q),(D4,Q),
                (C4,Q),(C4,Q),(D4,Q),(E4,Q),(E4,Q),(D4,Q),(D4,H),
                (E4,Q),(E4,Q),(F4,Q),(G4,Q),(G4,Q),(F4,Q),(E4,Q),(D4,Q),
                (C4,Q),(C4,Q),(D4,Q),(E4,Q),(D4,Q),(C4,Q),(C4,H),
                (D4,Q),(D4,Q),(E4,Q),(C4,Q),(D4,Q),(E4,E),(F4,E),(E4,Q),(C4,Q),
                (D4,Q),(E4,E),(F4,E),(E4,Q),(D4,Q),(C4,Q),(D4,Q),(G4,H),
                (E4,Q),(E4,Q),(F4,Q),(G4,Q),(G4,Q),(F4,Q),(E4,Q),(D4,Q),
                (C4,Q),(C4,Q),(D4,Q),(E4,Q),(D4,Q),(C4,Q),(C4,H),
            ]),
            BuildWithRests("Für Elise (Theme)", "Easy", "Beethoven", [
                (E5,E),(D5,E),(E5,E),(D5,E),(E5,E),(B4,E),(D5,E),(C5,E),
                (A4,Q),(R,E),(C4,E),(E4,E),(A4,E),
                (B4,Q),(R,E),(E4,E),(G4,E),(B4,E),
                (C5,Q),(R,E),(E4,E),(E5,E),(D5,E),
                (E5,E),(D5,E),(E5,E),(D5,E),(E5,E),(B4,E),(D5,E),(C5,E),
                (A4,Q),(R,E),(C4,E),(E4,E),(A4,E),
                (B4,Q),(R,E),(E4,E),(C5,E),(B4,E),
                (A4,W),
            ]),
            BuildWithRests("Happy Birthday", "Easy", "Traditional", [
                (R,E),(G4,E),(G4,E),(A4,Q),(G4,Q),(C5,Q),(B4,H),
                (R,E),(G4,E),(G4,E),(A4,Q),(G4,Q),(D5,Q),(C5,H),
                (R,E),(G4,E),(G4,E),(G5,Q),(E5,Q),(C5,Q),(B4,Q),(A4,H),
                (R,E),(F5,E),(F5,E),(E5,Q),(C5,Q),(D5,Q),(C5,H),
            ]),

            // ════════════════════════════════════════════════════════════════
            // EASY — alternating hands
            Build("Lightly Row (Alternating)", "Easy", "Traditional", [
                (G4,Q),(G4,Q),(F4,H),(F4,Q),(E4,Q),(D4,H),
                (D3,Q),(E3,Q),(F3,Q),(F3,Q),(E3,Q),(E3,Q),(D3,H),
                (G4,Q),(G4,Q),(F4,H),(F4,Q),(E4,Q),(D4,H),
                (A3,Q),(A3,Q),(G3,H),(G3,Q),(F3,Q),(E3,H),
                (D4,Q),(D4,Q),(D4,Q),(E4,Q),(F4,Q),(F4,Q),(F4,H),
                (E4,Q),(E4,Q),(E4,Q),(F4,Q),(G4,H),(G4,H),
                (D3,Q),(D3,Q),(D3,Q),(E3,Q),(F3,Q),(F3,Q),(F3,H),
                (G4,Q),(F4,Q),(E4,Q),(D4,Q),(D4,W),
            ]),
            Build("Row Your Boat (L+R)", "Easy", "Traditional", [
                (C3,DH),(C3,Q),(C3,Q),(D3,Q),
                (E3,DH),(E3,Q),(D3,Q),(E3,Q),
                (F3,H),(G3,H),
                (C5,E),(C5,E),(C5,E),(G4,E),(G4,E),(G4,E),
                (E4,E),(E4,E),(E4,E),(C4,E),(C4,E),(C4,E),
                (G4,Q),(F4,Q),(E4,Q),(D4,Q),(C4,W),
            ]),

            // ════════════════════════════════════════════════════════════════
            // SIMPLE TWO-HAND — sequential, no simultaneous notes
            BuildWithRests("Ode to Joy (Two Hands)", "Simple Two-Hand", "Beethoven", [
                (E4,Q),(E4,Q),(F4,Q),(G4,Q),(C3,H),(G3,H),
                (G4,Q),(F4,Q),(E4,Q),(D4,Q),(C3,H),(G3,H),
                (C4,Q),(C4,Q),(D4,Q),(E4,Q),(F3,H),(C3,H),
                (E4,Q),(D4,Q),(D4,H),(G3,H),(G3,H),
                (E4,Q),(E4,Q),(F4,Q),(G4,Q),(C3,H),(G3,H),
                (G4,Q),(F4,Q),(E4,Q),(D4,Q),(C3,H),(G3,H),
                (C4,Q),(C4,Q),(D4,Q),(E4,Q),(F3,H),(C3,H),
                (D4,Q),(C4,Q),(C4,H),(G3,W),
            ]),
            BuildWithRests("Twinkle (Two Hands)", "Simple Two-Hand", "Traditional", [
                (C4,Q),(C4,Q),(G4,Q),(G4,Q),(C3,H),(G3,H),
                (A4,Q),(A4,Q),(G4,H),(F3,H),(C3,H),
                (F4,Q),(F4,Q),(E4,Q),(E4,Q),(C3,H),(G3,H),
                (D4,Q),(D4,Q),(C4,H),(G3,H),(C3,H),
                (G4,Q),(G4,Q),(F4,Q),(F4,Q),(C3,H),(G3,H),
                (E4,Q),(E4,Q),(D4,H),(F3,H),(C3,H),
                (C4,Q),(C4,Q),(G4,Q),(G4,Q),(C3,H),(G3,H),
                (A4,Q),(A4,Q),(G4,H),(F3,H),(C3,H),
                (F4,Q),(F4,Q),(E4,Q),(E4,Q),(C3,H),(G3,H),
                (D4,Q),(D4,Q),(C4,H),(G3,H),(C3,H),
            ]),
        ];
    }
}
