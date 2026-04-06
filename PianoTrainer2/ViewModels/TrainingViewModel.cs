using Microsoft.Win32;
using PianoTrainer2.Controls;
using PianoTrainer2.Models;
using PianoTrainer2.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace PianoTrainer2.ViewModels
{
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;
        public RelayCommand(Action execute, Func<bool>? canExecute = null) { _execute = execute; _canExecute = canExecute; }
        public bool CanExecute(object? p) => _canExecute?.Invoke() ?? true;
        public void Execute(object? p) => _execute();
        public event EventHandler? CanExecuteChanged { add => CommandManager.RequerySuggested += value; remove => CommandManager.RequerySuggested -= value; }
    }

    public class TrainingViewModel : INotifyPropertyChanged
    {
        // ── Highway reference (set after control is created) ──────────────
        private NoteHighwayControl? _highway;
        private bool[] _pendingKeys = new bool[128];
        public bool[] PendingKeys { get => _pendingKeys; private set { _pendingKeys = value; OnPropertyChanged(); } }

        private bool[] _activeKeys = new bool[128];
        public bool[] ActiveKeys { get => _activeKeys; set { _activeKeys = value; OnPropertyChanged(); } }

        public void AttachHighway(NoteHighwayControl hw)
        {
            _highway = hw;
            _highway.NoteHit            += _ => { Score += 10; Combo++; };
            _highway.NoteMissed         += _ => { Combo = 0; };
            _highway.PendingKeysChanged += keys => PendingKeys = keys;
            _highway.SongCompleted      += OnSongCompleted;
            _highway.Mode          = SelectedMode;
            _highway.TrackDuration = TrackDuration;

            // Auto-play MIDI output wiring
            _highway.AutoPlayNoteOn += (note, velocity) =>
            {
                SendNoteOn?.Invoke(note, velocity);
                var keys = (bool[])ActiveKeys.Clone();
                keys[note] = true;
                ActiveKeys = keys;
            };
            _highway.AutoPlayNoteOff += note =>
            {
                SendNoteOff?.Invoke(note);
                var keys = (bool[])ActiveKeys.Clone();
                keys[note] = false;
                ActiveKeys = keys;
            };
        }

        // ── MIDI output callbacks (set by MainViewModel) ────────────────
        public Action<int, int>? SendNoteOn { get; set; }
        public Action<int>? SendNoteOff { get; set; }

        // ── Built-in song list ────────────────────────────────────────────
        public IReadOnlyList<BuiltInSong> BuiltInSongs => SongLibrary.Songs;

        private BuiltInSong? _selectedBuiltIn;
        public BuiltInSong? SelectedBuiltIn
        {
            get => _selectedBuiltIn;
            set { _selectedBuiltIn = value; OnPropertyChanged(); CustomMidiPath = null; }
        }

        // ── Mode ──────────────────────────────────────────────────────────
        public Array ModeValues => Enum.GetValues(typeof(TrainingMode));

        private TrainingMode _mode = TrainingMode.Continuous;
        public TrainingMode SelectedMode
        {
            get => _mode;
            set { _mode = value; OnPropertyChanged(); if (_highway != null) _highway.Mode = value; }
        }

        // ── Track duration ────────────────────────────────────────────────
        private bool _trackDuration = false;
        public bool TrackDuration
        {
            get => _trackDuration;
            set { _trackDuration = value; OnPropertyChanged(); if (_highway != null) _highway.TrackDuration = value; }
        }

        // ── Speed ─────────────────────────────────────────────────────────
        public double[] SpeedValues { get; } = [0.25, 0.5, 0.75, 1.0, 1.25, 1.5, 2.0];

        private int _speedIndex = 3; // default 1.0×
        public int SpeedIndex
        {
            get => _speedIndex;
            set { _speedIndex = value; _speed = SpeedValues[value]; OnPropertyChanged(); OnPropertyChanged(nameof(SpeedLabel)); }
        }

        private double _speed = 1.0;
        public double Speed => _speed;
        public string SpeedLabel => $"{Speed:0.##}×";

        // ── Score / Combo ─────────────────────────────────────────────────
        private int _score;
        public int Score { get => _score; set { _score = value; OnPropertyChanged(); } }

        private int _combo;
        public int Combo
        {
            get => _combo;
            set
            {
                _combo = value;
                OnPropertyChanged();
                if (value > MaxCombo) MaxCombo = value;
            }
        }

        private int _maxCombo;
        public int MaxCombo { get => _maxCombo; set { _maxCombo = value; OnPropertyChanged(); } }

        // ── End-of-song stats ─────────────────────────────────────────────
        private bool _showEndScreen;
        public bool ShowEndScreen { get => _showEndScreen; set { _showEndScreen = value; OnPropertyChanged(); } }

        private string _endSongTitle = "";
        public string EndSongTitle { get => _endSongTitle; set { _endSongTitle = value; OnPropertyChanged(); } }

        private int _endNotesHit;
        public int EndNotesHit { get => _endNotesHit; set { _endNotesHit = value; OnPropertyChanged(); } }

        private int _endNotesTotal;
        public int EndNotesTotal { get => _endNotesTotal; set { _endNotesTotal = value; OnPropertyChanged(); } }

        private int _endAccuracy;
        public int EndAccuracy { get => _endAccuracy; set { _endAccuracy = value; OnPropertyChanged(); } }

        private int _endScore;
        public int EndScore { get => _endScore; set { _endScore = value; OnPropertyChanged(); } }

        private int _endMaxCombo;
        public int EndMaxCombo { get => _endMaxCombo; set { _endMaxCombo = value; OnPropertyChanged(); } }

        // ── Status / progress ─────────────────────────────────────────────
        private string _status = "Select a song and press Start.";
        public string Status { get => _status; set { _status = value; OnPropertyChanged(); } }

        private int _downloadProgress;
        public int DownloadProgress { get => _downloadProgress; set { _downloadProgress = value; OnPropertyChanged(); } }

        private bool _isDownloading;
        public bool IsDownloading { get => _isDownloading; set { _isDownloading = value; OnPropertyChanged(); } }

        private bool _isPlaying;
        public bool IsPlaying { get => _isPlaying; set { _isPlaying = value; OnPropertyChanged(); } }

        private bool _isAutoPlaying;
        public bool IsAutoPlaying { get => _isAutoPlaying; set { _isAutoPlaying = value; OnPropertyChanged(); } }

        // ── Custom file path ──────────────────────────────────────────────
        private string? _customMidiPath;
        public string? CustomMidiPath
        {
            get => _customMidiPath;
            set { _customMidiPath = value; OnPropertyChanged(); OnPropertyChanged(nameof(CurrentSongLabel)); }
        }

        public string CurrentSongLabel =>
            CustomMidiPath != null ? System.IO.Path.GetFileName(CustomMidiPath) :
            SelectedBuiltIn != null ? SelectedBuiltIn.ToString() :
            "(none)";

        // ── Commands ──────────────────────────────────────────────────────
        public ICommand BrowseCommand { get; }
        public ICommand StartCommand { get; }
        public ICommand StopCommand { get; }
        public ICommand AutoPlayCommand { get; }
        public ICommand PlayAgainCommand { get; }
        public ICommand DismissCommand { get; }

        public TrainingViewModel()
        {
            BrowseCommand   = new RelayCommand(Browse);
            StartCommand    = new RelayCommand(async () => await StartAsync(autoPlay: false), () => !IsPlaying && !IsDownloading);
            StopCommand     = new RelayCommand(StopPlayback, () => IsPlaying);
            AutoPlayCommand = new RelayCommand(async () => await StartAsync(autoPlay: true),  () => !IsPlaying && !IsDownloading);
            PlayAgainCommand = new RelayCommand(async () => { ShowEndScreen = false; await StartAsync(_lastAutoPlay); }, () => !IsPlaying && !IsDownloading);
            DismissCommand   = new RelayCommand(() => ShowEndScreen = false, () => !IsPlaying);
        }

        private void Browse()
        {
            var dlg = new OpenFileDialog { Filter = "MIDI files (*.mid;*.midi)|*.mid;*.midi|All files|*.*" };
            if (dlg.ShowDialog() == true)
            {
                CustomMidiPath = dlg.FileName;
                SelectedBuiltIn = null;
                OnPropertyChanged(nameof(CurrentSongLabel));
            }
        }

        private async Task StartAsync(bool autoPlay)
        {
            if (_highway == null) return;
            Score = 0; Combo = 0; MaxCombo = 0;
            ShowEndScreen = false;
            _lastAutoPlay = autoPlay;
            _highway.AutoPlay = autoPlay;
            IsAutoPlaying = autoPlay;

            // Embedded songs need no download or parsing
            if (SelectedBuiltIn?.EmbeddedSong != null)
            {
                var embedded = SelectedBuiltIn.EmbeddedSong;
                Status = $"{(autoPlay ? "Auto-Playing" : "Playing")}: {embedded.Title}  ({embedded.Notes.Count} notes)";
                _highway.Mode = autoPlay ? TrainingMode.Continuous : SelectedMode;
                _highway.TrackDuration = TrackDuration;
                _highway.LoadSong(embedded, Speed);
                _highway.Start();
                IsPlaying = true;
                return;
            }

            string? path = CustomMidiPath;

            if (path == null && SelectedBuiltIn != null)
            {
                if (!SongDownloader.IsCached(SelectedBuiltIn))
                {
                    IsDownloading = true;
                    Status = $"Downloading {SelectedBuiltIn.Title}...";
                    try
                    {
                        var prog = new Progress<int>(p => DownloadProgress = p);
                        path = await SongDownloader.EnsureDownloadedAsync(SelectedBuiltIn, prog);
                    }
                    catch (Exception ex)
                    {
                        Status = $"Download failed: {ex.Message}";
                        IsDownloading = false;
                        return;
                    }
                    IsDownloading = false;
                }
                else
                {
                    path = SongDownloader.GetCachedPath(SelectedBuiltIn);
                }
            }

            if (path == null) { Status = "No song selected."; return; }

            try
            {
                Status = "Parsing MIDI...";
                var song = await Task.Run(() => MidiSongParser.Parse(path, SelectedBuiltIn?.Title ?? ""));
                Status = $"{(autoPlay ? "Auto-Playing" : "Playing")}: {song.Title}  ({song.Notes.Count} notes)";
                _highway.Mode = autoPlay ? TrainingMode.Continuous : SelectedMode;
                _highway.TrackDuration = TrackDuration;
                _highway.LoadSong(song, Speed);
                _highway.Start();
                IsPlaying = true;
            }
            catch (Exception ex)
            {
                Status = $"Error: {ex.Message}";
            }
        }

        private bool _lastAutoPlay;

        private void OnSongCompleted(int notesHit, int notesTotal)
        {
            IsPlaying = false;
            IsAutoPlaying = false;
            if (_highway != null) _highway.AutoPlay = false;

            EndSongTitle  = CurrentSongLabel;
            EndNotesHit   = notesHit;
            EndNotesTotal = notesTotal;
            EndAccuracy   = notesTotal > 0 ? (int)Math.Round(notesHit * 100.0 / notesTotal) : 0;
            EndScore      = Score;
            EndMaxCombo   = MaxCombo;
            ShowEndScreen = true;
            Status        = "Song complete!";
        }

        private void StopPlayback()
        {
            _highway?.Stop();
            IsPlaying = false;
            IsAutoPlaying = false;
            if (_highway != null) _highway.AutoPlay = false;
            Status = "Stopped.";
        }

        // ── MIDI forwarding ───────────────────────────────────────────────
        public void OnNoteOn(NoteEventArgs e) => _highway?.KeyPressed(e.NoteNumber);
        public void OnNoteOff(NoteEventArgs e) => _highway?.KeyReleased(e.NoteNumber);

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
