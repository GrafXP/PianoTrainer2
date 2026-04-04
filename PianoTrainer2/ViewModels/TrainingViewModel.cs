using Microsoft.Win32;
using PianoTrainer2.Controls;
using PianoTrainer2.Models;
using PianoTrainer2.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
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
        public void AttachHighway(NoteHighwayControl hw)
        {
            _highway = hw;
            _highway.NoteHit += _ => { Score += 10; Combo++; };
            _highway.NoteMissed += _ => { Combo = 0; };
        }

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

        // ── Score / Combo ─────────────────────────────────────────────────
        private int _score;
        public int Score { get => _score; set { _score = value; OnPropertyChanged(); } }

        private int _combo;
        public int Combo { get => _combo; set { _combo = value; OnPropertyChanged(); } }

        // ── Status / progress ─────────────────────────────────────────────
        private string _status = "Select a song and press Start.";
        public string Status { get => _status; set { _status = value; OnPropertyChanged(); } }

        private int _downloadProgress;
        public int DownloadProgress { get => _downloadProgress; set { _downloadProgress = value; OnPropertyChanged(); } }

        private bool _isDownloading;
        public bool IsDownloading { get => _isDownloading; set { _isDownloading = value; OnPropertyChanged(); } }

        private bool _isPlaying;
        public bool IsPlaying { get => _isPlaying; set { _isPlaying = value; OnPropertyChanged(); } }

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

        public TrainingViewModel()
        {
            BrowseCommand = new RelayCommand(Browse);
            StartCommand = new RelayCommand(async () => await StartAsync(), () => !IsPlaying && !IsDownloading);
            StopCommand = new RelayCommand(StopPlayback, () => IsPlaying);
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

        private async Task StartAsync()
        {
            if (_highway == null) return;
            Score = 0; Combo = 0;

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
                Status = $"Playing: {song.Title}  ({song.Notes.Count} notes)";
                _highway.Mode = SelectedMode;
                _highway.LoadSong(song);
                _highway.Start();
                IsPlaying = true;
            }
            catch (Exception ex)
            {
                Status = $"Error: {ex.Message}";
            }
        }

        private void StopPlayback()
        {
            _highway?.Stop();
            IsPlaying = false;
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
