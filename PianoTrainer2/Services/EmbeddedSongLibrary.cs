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

        private static Song Build(string title, IEnumerable<(int note, double dur)> notes)
        {
            var list = new List<SongNote>();
            double t = 0;
            foreach (var (note, dur) in notes)
            {
                list.Add(new SongNote(note, t, dur * 0.9)); // slight gap between notes
                t += dur;
            }
            return new Song
            {
                Title = title,
                Notes = list,
                TotalDurationMs = t + 500,
                InitialUsPerBeat = 666_667 // 90 BPM
            };
        }

        // beat durations at 90 BPM
        private const double Q  = 667;   // quarter note
        private const double H  = 1333;  // half note
        private const double DH = 2000;  // dotted half
        private const double W  = 2667;  // whole note
        private const double E  = 333;   // eighth note

        // note constants
        private const int C4 = 60, D4 = 62, E4 = 64, F4 = 65, G4 = 67, A4 = 69, B4 = 71, C5 = 72;
        private const int G3 = 55, A3 = 57, B3 = 59;

        public static IReadOnlyList<Song> Songs { get; } =
        [
            // ── Hot Cross Buns (E D C) ───────────────────────────────────────
            Build("Hot Cross Buns", [
                (E4,Q),(D4,Q),(C4,H),
                (E4,Q),(D4,Q),(C4,H),
                (C4,E),(C4,E),(C4,E),(C4,E),
                (D4,E),(D4,E),(D4,E),(D4,E),
                (E4,Q),(D4,Q),(C4,H),
            ]),

            // ── Mary Had a Little Lamb ───────────────────────────────────────
            Build("Mary Had a Little Lamb", [
                (E4,Q),(D4,Q),(C4,Q),(D4,Q),
                (E4,Q),(E4,Q),(E4,H),
                (D4,Q),(D4,Q),(D4,H),
                (E4,Q),(G4,Q),(G4,H),
                (E4,Q),(D4,Q),(C4,Q),(D4,Q),
                (E4,Q),(E4,Q),(E4,Q),(E4,Q),
                (D4,Q),(D4,Q),(E4,Q),(D4,Q),
                (C4,W),
            ]),

            // ── Twinkle Twinkle Little Star ──────────────────────────────────
            Build("Twinkle Twinkle", [
                (C4,Q),(C4,Q),(G4,Q),(G4,Q),
                (A4,Q),(A4,Q),(G4,H),
                (F4,Q),(F4,Q),(E4,Q),(E4,Q),
                (D4,Q),(D4,Q),(C4,H),
                (G4,Q),(G4,Q),(F4,Q),(F4,Q),
                (E4,Q),(E4,Q),(D4,H),
                (G4,Q),(G4,Q),(F4,Q),(F4,Q),
                (E4,Q),(E4,Q),(D4,H),
                (C4,Q),(C4,Q),(G4,Q),(G4,Q),
                (A4,Q),(A4,Q),(G4,H),
                (F4,Q),(F4,Q),(E4,Q),(E4,Q),
                (D4,Q),(D4,Q),(C4,H),
            ]),

            // ── Ode to Joy (right hand only) ─────────────────────────────────
            Build("Ode to Joy (Right Hand)", [
                (E4,Q),(E4,Q),(F4,Q),(G4,Q),
                (G4,Q),(F4,Q),(E4,Q),(D4,Q),
                (C4,Q),(C4,Q),(D4,Q),(E4,Q),
                (E4,Q),(D4,Q),(D4,H),
                (E4,Q),(E4,Q),(F4,Q),(G4,Q),
                (G4,Q),(F4,Q),(E4,Q),(D4,Q),
                (C4,Q),(C4,Q),(D4,Q),(E4,Q),
                (D4,Q),(C4,Q),(C4,H),
            ]),

            // ── Jingle Bells (chorus only) ───────────────────────────────────
            Build("Jingle Bells (Chorus)", [
                (E4,Q),(E4,Q),(E4,H),
                (E4,Q),(E4,Q),(E4,H),
                (E4,Q),(G4,Q),(C4,Q),(D4,Q),
                (E4,W),
                (F4,Q),(F4,Q),(F4,Q),(F4,Q),
                (F4,Q),(E4,Q),(E4,Q),(E4,Q),
                (E4,Q),(D4,Q),(D4,Q),(E4,Q),
                (D4,H),(G4,H),
                (E4,Q),(E4,Q),(E4,H),
                (E4,Q),(E4,Q),(E4,H),
                (E4,Q),(G4,Q),(C4,Q),(D4,Q),
                (E4,W),
                (F4,Q),(F4,Q),(F4,Q),(F4,Q),
                (F4,Q),(E4,Q),(E4,Q),(E4,Q),
                (G4,Q),(G4,Q),(F4,Q),(D4,Q),
                (C4,W),
            ]),
        ];
    }
}
