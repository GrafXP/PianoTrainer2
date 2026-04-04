using NAudio.Midi;
using PianoTrainer2.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PianoTrainer2.Services
{
    public static class MidiSongParser
    {
        public static Song Parse(string filePath, string title = "")
        {
            var midi = new MidiFile(filePath, strictChecking: false);
            int tpqn = midi.DeltaTicksPerQuarterNote;

            // Build tempo map from track 0: list of (absoluteTick, microsecondsPerBeat)
            var tempoMap = new List<(long tick, long uspb)> { (0, 500_000) }; // default 120bpm
            foreach (var ev in midi.Events[0])
            {
                if (ev is TempoEvent te)
                    tempoMap.Add((ev.AbsoluteTime, te.MicrosecondsPerQuarterNote));
            }
            tempoMap.Sort((a, b) => a.tick.CompareTo(b.tick));

            double TicksToMs(long ticks)
            {
                double ms = 0;
                long remaining = ticks;
                for (int i = 0; i < tempoMap.Count; i++)
                {
                    long segStart = tempoMap[i].tick;
                    long segEnd = i + 1 < tempoMap.Count ? tempoMap[i + 1].tick : long.MaxValue;
                    long segTicks = Math.Min(remaining, segEnd - segStart);
                    if (segTicks <= 0) break;
                    ms += (double)segTicks / tpqn * tempoMap[i].uspb / 1000.0;
                    remaining -= segTicks;
                    if (remaining <= 0) break;
                }
                return ms;
            }

            // Collect note on/off pairs across all tracks
            var openNotes = new Dictionary<(int ch, int note), long>();
            var notes = new List<SongNote>();

            for (int t = 0; t < midi.Tracks; t++)
            {
                openNotes.Clear();
                foreach (var ev in midi.Events[t])
                {
                    if (ev is NoteOnEvent noteOn)
                    {
                        var key = (noteOn.Channel, noteOn.NoteNumber);
                        if (noteOn.Velocity > 0)
                        {
                            openNotes[key] = ev.AbsoluteTime;
                        }
                        else
                        {
                            // NoteOn velocity=0 = NoteOff
                            if (openNotes.TryGetValue(key, out long startTick))
                            {
                                openNotes.Remove(key);
                                double startMs = TicksToMs(startTick);
                                double durMs = TicksToMs(ev.AbsoluteTime) - startMs;
                                if (noteOn.NoteNumber >= 21 && noteOn.NoteNumber <= 108)
                                    notes.Add(new SongNote(noteOn.NoteNumber, startMs, Math.Max(50, durMs)));
                            }
                        }
                    }
                    else if (ev is NoteEvent noteOff && ev.CommandCode == MidiCommandCode.NoteOff)
                    {
                        var key = (noteOff.Channel, noteOff.NoteNumber);
                        if (openNotes.TryGetValue(key, out long startTick))
                        {
                            openNotes.Remove(key);
                            double startMs = TicksToMs(startTick);
                            double durMs = TicksToMs(ev.AbsoluteTime) - startMs;
                            if (noteOff.NoteNumber >= 21 && noteOff.NoteNumber <= 108)
                                notes.Add(new SongNote(noteOff.NoteNumber, startMs, Math.Max(50, durMs)));
                        }
                    }
                }
                // Close any still-open notes with synthetic 250ms duration
                foreach (var kv in openNotes)
                {
                    double startMs = TicksToMs(kv.Value);
                    int n = kv.Key.note;
                    if (n >= 21 && n <= 108)
                        notes.Add(new SongNote(n, startMs, 250));
                }
            }

            notes.Sort((a, b) => a.StartMs.CompareTo(b.StartMs));
            double total = notes.Count > 0 ? notes.Max(n => n.StartMs + n.DurationMs) : 0;
            long firstTempo = tempoMap.Count > 0 ? tempoMap[0].uspb : 500_000;

            return new Song
            {
                Title = string.IsNullOrEmpty(title) ? System.IO.Path.GetFileNameWithoutExtension(filePath) : title,
                Notes = notes,
                TotalDurationMs = total,
                InitialUsPerBeat = firstTempo
            };
        }
    }
}
