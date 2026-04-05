using PianoTrainer2.ViewModels;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Threading;

namespace PianoTrainer2
{
    public class NoteLogEntry : INotifyPropertyChanged
    {
        public string NoteName { get; set; } = "";
        public int Velocity { get; set; }
        public DateTime StartTime { get; set; }

        private DateTime? _endTime;
        public DateTime? EndTime
        {
            get => _endTime;
            set { _endTime = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Duration))); }
        }

        public string Duration => EndTime.HasValue
            ? $"{(EndTime.Value - StartTime).TotalMilliseconds:F0} ms"
            : "...";
        public bool IsActive => !EndTime.HasValue;

        public event PropertyChangedEventHandler? PropertyChanged;
    }

    public class MainViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly MidiService _midi = new();
        private readonly DispatcherTimer _refreshTimer;
        private readonly System.Collections.Generic.Dictionary<int, NoteLogEntry> _active = new();

        public ObservableCollection<string> Devices { get; } = new();
        public ObservableCollection<NoteLogEntry> EventLog { get; } = new();
        public bool[] ActiveKeys { get; private set; } = new bool[128];
        public TrainingViewModel TrainingVm { get; } = new();

        private int _selectedDevice = -1;
        public int SelectedDevice
        {
            get => _selectedDevice;
            set { _selectedDevice = value; OnPropertyChanged(); ConnectMidi(); }
        }

        private string _status = "Select MIDI device";
        public string Status { get => _status; set { _status = value; OnPropertyChanged(); } }

        public MainViewModel()
        {
            foreach (var name in MidiService.DeviceNames())
                Devices.Add(name);

            if (Devices.Count == 0)
                Devices.Add("(no MIDI devices)");
            else
                SelectedDevice = 0;

            _midi.NoteOn += (_, e) => Application.Current.Dispatcher.Invoke(() => HandleNoteOn(e));
            _midi.NoteOff += (_, e) => Application.Current.Dispatcher.Invoke(() => HandleNoteOff(e));

            _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
            _refreshTimer.Tick += (_, _) => RefreshDurations();
            _refreshTimer.Start();
        }

        private void ConnectMidi()
        {
            if (_selectedDevice < 0 || _selectedDevice >= MidiService.DeviceNames().Length) return;
            try { _midi.Open(_selectedDevice); Status = $"Connected: {Devices[_selectedDevice]}"; }
            catch (Exception ex) { Status = $"Error: {ex.Message}"; }
        }

        private void HandleNoteOn(NoteEventArgs e)
        {
            TrainingVm.OnNoteOn(e);
            var name = NoteName(e.NoteNumber);
            var entry = new NoteLogEntry { NoteName = name, Velocity = e.Velocity, StartTime = DateTime.Now };
            _active[e.NoteNumber] = entry;
            EventLog.Insert(0, entry);
            if (EventLog.Count > 200) EventLog.RemoveAt(EventLog.Count - 1);
            var keysOn = (bool[])ActiveKeys.Clone();
            keysOn[e.NoteNumber] = true;
            ActiveKeys = keysOn;
            OnPropertyChanged(nameof(ActiveKeys));
            TrainingVm.ActiveKeys = keysOn;
        }

        private void HandleNoteOff(NoteEventArgs e)
        {
            TrainingVm.OnNoteOff(e);
            if (_active.TryGetValue(e.NoteNumber, out var entry))
            {
                entry.EndTime = DateTime.Now;
                _active.Remove(e.NoteNumber);
            }
            var keysOff = (bool[])ActiveKeys.Clone();
            keysOff[e.NoteNumber] = false;
            ActiveKeys = keysOff;
            OnPropertyChanged(nameof(ActiveKeys));
            TrainingVm.ActiveKeys = keysOff;
        }

        private void RefreshDurations()
        {
            // No-op: NoteLogEntry.EndTime setter fires PropertyChanged(Duration) on note-off.
        }

        public static string NoteName(int n)
        {
            string[] names = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
            return names[n % 12] + ((n / 12) - 1);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public void Dispose() => _midi.Dispose();
    }
}
