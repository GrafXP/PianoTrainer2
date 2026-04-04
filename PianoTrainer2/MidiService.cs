using NAudio.Midi;
using System;

namespace PianoTrainer2
{
    public class NoteEventArgs : EventArgs
    {
        public int NoteNumber { get; }
        public int Velocity { get; }
        public NoteEventArgs(int note, int velocity) { NoteNumber = note; Velocity = velocity; }
    }

    public class MidiService : IDisposable
    {
        private MidiIn? _midiIn;

        public event EventHandler<NoteEventArgs>? NoteOn;
        public event EventHandler<NoteEventArgs>? NoteOff;

        public static string[] DeviceNames()
        {
            var names = new string[MidiIn.NumberOfDevices];
            for (int i = 0; i < MidiIn.NumberOfDevices; i++)
                names[i] = MidiIn.DeviceInfo(i).ProductName;
            return names;
        }

        public void Open(int deviceIndex)
        {
            _midiIn = new MidiIn(deviceIndex);
            _midiIn.MessageReceived += OnMessage;
            _midiIn.Start();
        }

        private void OnMessage(object? sender, MidiInMessageEventArgs e)
        {
            if (e.MidiEvent is NoteOnEvent noteOn)
            {
                if (noteOn.Velocity == 0)
                    NoteOff?.Invoke(this, new NoteEventArgs(noteOn.NoteNumber, 0));
                else
                    NoteOn?.Invoke(this, new NoteEventArgs(noteOn.NoteNumber, noteOn.Velocity));
            }
            else if (e.MidiEvent.CommandCode == MidiCommandCode.NoteOff)
            {
                var noteOff = (NoteEvent)e.MidiEvent;
                NoteOff?.Invoke(this, new NoteEventArgs(noteOff.NoteNumber, 0));
            }
        }

        public void Dispose()
        {
            _midiIn?.Stop();
            _midiIn?.Dispose();
        }
    }
}
