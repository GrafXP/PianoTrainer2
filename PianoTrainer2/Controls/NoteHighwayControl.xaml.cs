using PianoTrainer2.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace PianoTrainer2.Controls
{
    public enum TrainingMode { Continuous, WaitForPress }
    public enum HitState { Active, Frozen, Hit, Miss }

    public class FallingNote
    {
        public SongNote Source    { get; init; } = null!;
        public Rectangle Visual   { get; init; } = null!;
        public TextBlock Label    { get; init; } = null!;
        public HitState  State    { get; set; }  = HitState.Active;
        public double    KeyDownAt { get; set; } = -1;
        public bool      KeyIsDown { get; set; }
    }

    public partial class NoteHighwayControl : UserControl
    {
        // ── tunables ──────────────────────────────────────────────────────────
        public TrainingMode Mode        { get; set; } = TrainingMode.Continuous;
        public double       FallSeconds { get; set; } = 5.0;
        private double _pixelsPerMs = 0.3;
        private const int    TickMs        = 16;
        private const double HitZoneHeight = 8;
        private const double HitWindowMs   = 200;
        private const double FreezeGraceMs = 4000;
        // Keys are "pending" (dim hint on keyboard) when within this many ms of hitting
        private const double PendingWindowMs = 2000;
        private double LookaheadMs => ActualHeight / _pixelsPerMs;

        // ── events ────────────────────────────────────────────────────────────
        public event Action<int>?       NoteHit;
        public event Action<int>?       NoteMissed;
        /// Fired each tick with which notes are approaching (for keyboard hint)
        public event Action<bool[]>?    PendingKeysChanged;

        // ── private state ─────────────────────────────────────────────────────
        private Song?                   _song;
        private readonly List<FallingNote> _falling = new();
        private int                     _nextNoteIndex;
        private double                  _playbackMs;
        private bool                    _frozen;
        private readonly Stopwatch      _clock = new();
        private double                  _clockOffsetMs;
        private readonly DispatcherTimer _timer;

        private Rectangle?              _hitZone;
        private readonly List<Line>     _rails = new();
        private bool[]                  _lastPending = new bool[128];

        public NoteHighwayControl()
        {
            InitializeComponent();
            _timer = new DispatcherTimer(DispatcherPriority.Render)
            {
                Interval = TimeSpan.FromMilliseconds(TickMs)
            };
            _timer.Tick  += OnTick;
            Loaded       += (_, _) => { BuildRails(); RedrawHitZone(); };
            SizeChanged  += (_, _) => { BuildRails(); RedrawHitZone(); };
        }

        // ── public API ────────────────────────────────────────────────────────
        public void LoadSong(Song song) { Stop(); _song = song; }

        public void Start()
        {
            if (_song == null) return;
            double h      = ActualHeight > 10 ? ActualHeight : 600;
            _pixelsPerMs  = h / (FallSeconds * 1000.0);

            _falling.Clear();
            HighwayCanvas.Children.Clear();
            _rails.Clear();
            BuildRails();
            RedrawHitZone();

            _nextNoteIndex = 0;
            _playbackMs    = -(FallSeconds * 1000.0);
            _clockOffsetMs = _playbackMs;
            _frozen        = false;
            _clock.Restart();
            _timer.Start();
        }

        public void Stop()
        {
            _timer.Stop();
            _clock.Stop();
            HighwayCanvas.Children.Clear();
            _falling.Clear();
            _rails.Clear();
            _hitZone = null;
            BuildRails();
            RedrawHitZone();
            // Clear pending hints
            PendingKeysChanged?.Invoke(new bool[128]);
        }

        // ── MIDI forwarding ───────────────────────────────────────────────────
        public void KeyPressed(int noteNumber)
        {
            foreach (var fn in _falling.Where(f => f.Source.NoteNumber == noteNumber))
            {
                if (fn.State == HitState.Frozen)
                {
                    fn.State      = HitState.Hit;
                    fn.KeyDownAt  = _playbackMs;
                    fn.KeyIsDown  = true;
                    _frozen = _falling.Any(f => f.State == HitState.Frozen);
                    if (!_frozen) { _clockOffsetMs = _playbackMs; _clock.Restart(); }
                    FlashNote(fn, true);
                    return;
                }
                if (fn.State == HitState.Active)
                {
                    double diff = Math.Abs(_playbackMs - fn.Source.StartMs);
                    if (diff <= HitWindowMs)
                    {
                        fn.State      = HitState.Hit;
                        fn.KeyDownAt  = _playbackMs;
                        fn.KeyIsDown  = true;
                        FlashNote(fn, true);
                        return;
                    }
                }
            }
        }

        public void KeyReleased(int noteNumber)
        {
            foreach (var fn in _falling.Where(f => f.Source.NoteNumber == noteNumber && f.KeyIsDown))
            {
                fn.KeyIsDown = false;
                double held  = _playbackMs - fn.KeyDownAt;
                if (held >= fn.Source.DurationMs * 0.7)
                    NoteHit?.Invoke(noteNumber);
                else
                {
                    fn.State = HitState.Miss;
                    FlashNote(fn, false);
                    NoteMissed?.Invoke(noteNumber);
                }
            }
        }

        // ── tick ──────────────────────────────────────────────────────────────
        private void OnTick(object? sender, EventArgs e)
        {
            if (_song == null) return;
            if (!_frozen)
                _playbackMs = _clockOffsetMs + _clock.Elapsed.TotalMilliseconds;

            SpawnNotes();
            PositionNotes();
            CheckHitZone();
            RemoveExpired();
            UpdatePendingKeys();

            if (!_frozen && _playbackMs > _song.TotalDurationMs + 3000 && _falling.Count == 0)
                Stop();
        }

        // ── rails ─────────────────────────────────────────────────────────────
        private void BuildRails()
        {
            foreach (var r in _rails) HighwayCanvas.Children.Remove(r);
            _rails.Clear();

            double h = ActualHeight > 0 ? ActualHeight : 600;

            for (int n = PianoKeyboardLayout.FirstNote; n <= PianoKeyboardLayout.LastNote; n++)
            {
                bool isBlack = PianoKeyboardLayout.IsBlack(n);
                double cx    = PianoKeyboardLayout.GetKeyXCenter(n);

                // One thin vertical line per key, running full height
                var line = new Line
                {
                    X1              = cx,
                    Y1              = 0,
                    X2              = cx,
                    Y2              = h,
                    Stroke          = isBlack
                                        ? new SolidColorBrush(Color.FromArgb(40, 150, 180, 255))
                                        : new SolidColorBrush(Color.FromArgb(25, 200, 220, 255)),
                    StrokeThickness = isBlack ? 1.0 : PianoKeyboardLayout.WhiteKeyWidth - 1,
                    IsHitTestVisible = false
                };
                Canvas.SetZIndex(line, 1);
                HighwayCanvas.Children.Add(line);
                _rails.Add(line);
            }
        }

        // ── spawn ─────────────────────────────────────────────────────────────
        private void SpawnNotes()
        {
            if (_song == null) return;
            double threshold = _playbackMs + LookaheadMs;
            while (_nextNoteIndex < _song.Notes.Count &&
                   _song.Notes[_nextNoteIndex].StartMs <= threshold)
            {
                var note = _song.Notes[_nextNoteIndex++];
                if (!PianoKeyboardLayout.IsInRange(note.NoteNumber)) continue;

                double x = PianoKeyboardLayout.GetKeyXCenter(note.NoteNumber)
                           - PianoKeyboardLayout.GetKeyWidth(note.NoteNumber) / 2.0;
                double w = PianoKeyboardLayout.GetKeyWidth(note.NoteNumber) - 1;
                double h = Math.Max(8, note.DurationMs * _pixelsPerMs);

                bool isBlack = PianoKeyboardLayout.IsBlack(note.NoteNumber);
                var fill = isBlack
                    ? new SolidColorBrush(Color.FromRgb(60, 120, 200))
                    : new SolidColorBrush(Color.FromRgb(90, 170, 255));

                var rect  = new Rectangle { Width = w, Height = h, Fill = fill, RadiusX = 3, RadiusY = 3 };
                var label = new TextBlock
                {
                    Text = MainViewModel.NoteName(note.NoteNumber),
                    FontSize = 7, Foreground = Brushes.White, IsHitTestVisible = false
                };

                Canvas.SetLeft(rect,  x);
                Canvas.SetLeft(label, x + 1);
                Canvas.SetZIndex(rect,  5);
                Canvas.SetZIndex(label, 6);
                HighwayCanvas.Children.Add(rect);
                HighwayCanvas.Children.Add(label);

                _falling.Add(new FallingNote { Source = note, Visual = rect, Label = label });
            }
        }

        // ── position ──────────────────────────────────────────────────────────
        private void PositionNotes()
        {
            double hitY = ActualHeight - HitZoneHeight;
            foreach (var fn in _falling)
            {
                if (fn.State == HitState.Frozen) continue;
                double top = hitY - (fn.Source.StartMs - _playbackMs) * _pixelsPerMs - fn.Visual.Height;
                Canvas.SetTop(fn.Visual, top);
                Canvas.SetTop(fn.Label,  top + 2);
            }
        }

        // ── hit zone ──────────────────────────────────────────────────────────
        private void CheckHitZone()
        {
            double hitY = ActualHeight - HitZoneHeight;

            foreach (var fn in _falling.Where(f => f.State == HitState.Active).ToList())
            {
                double top    = hitY - (fn.Source.StartMs - _playbackMs) * _pixelsPerMs - fn.Visual.Height;
                double bottom = top + fn.Visual.Height;
                if (bottom < hitY) continue;

                if (Mode == TrainingMode.WaitForPress)
                {
                    fn.State       = HitState.Frozen;
                    _clockOffsetMs = _playbackMs;
                    _clock.Restart();
                    _frozen        = true;
                }
                else
                {
                    if (top > hitY + HitZoneHeight)
                    {
                        fn.State = HitState.Miss;
                        FlashNote(fn, false);
                        NoteMissed?.Invoke(fn.Source.NoteNumber);
                    }
                }
            }

            if (Mode == TrainingMode.WaitForPress && _frozen)
            {
                foreach (var fn in _falling.Where(f => f.State == HitState.Frozen).ToList())
                {
                    if (_playbackMs - fn.Source.StartMs > FreezeGraceMs)
                    {
                        fn.State = HitState.Miss;
                        FlashNote(fn, false);
                        NoteMissed?.Invoke(fn.Source.NoteNumber);
                    }
                }
                _frozen = _falling.Any(f => f.State == HitState.Frozen);
                if (!_frozen) { _clockOffsetMs = _playbackMs; _clock.Restart(); }
            }
        }

        // ── pending key hints ─────────────────────────────────────────────────
        private void UpdatePendingKeys()
        {
            var pending = new bool[128];
            foreach (var fn in _falling.Where(f => f.State is HitState.Active or HitState.Frozen))
            {
                double msUntilHit = fn.Source.StartMs - _playbackMs;
                if (msUntilHit <= PendingWindowMs)
                    pending[fn.Source.NoteNumber] = true;
            }

            // Only fire if changed
            bool changed = false;
            for (int i = 0; i < 128; i++)
                if (pending[i] != _lastPending[i]) { changed = true; break; }

            if (changed)
            {
                _lastPending = pending;
                PendingKeysChanged?.Invoke(pending);
            }
        }

        // ── cleanup ───────────────────────────────────────────────────────────
        private void RemoveExpired()
        {
            var toRemove = _falling
                .Where(f => f.State is HitState.Hit or HitState.Miss && !f.KeyIsDown)
                .Where(f => Canvas.GetTop(f.Visual) > ActualHeight + 20 || f.Visual.Opacity < 0.05)
                .ToList();

            foreach (var fn in toRemove)
            {
                HighwayCanvas.Children.Remove(fn.Visual);
                HighwayCanvas.Children.Remove(fn.Label);
                _falling.Remove(fn);
            }
        }

        // ── helpers ───────────────────────────────────────────────────────────
        private void RedrawHitZone()
        {
            if (_hitZone != null) HighwayCanvas.Children.Remove(_hitZone);
            _hitZone = new Rectangle
            {
                Width  = PianoKeyboardLayout.TotalWidth,
                Height = HitZoneHeight,
                Fill   = new SolidColorBrush(Color.FromArgb(120, 255, 255, 255))
            };
            Canvas.SetLeft(_hitZone, 0);
            Canvas.SetTop (_hitZone, ActualHeight - HitZoneHeight);
            Canvas.SetZIndex(_hitZone, 10);
            HighwayCanvas.Children.Add(_hitZone);
        }

        private void FlashNote(FallingNote fn, bool hit)
        {
            fn.Visual.Fill = hit ? new SolidColorBrush(Colors.LimeGreen)
                                 : new SolidColorBrush(Colors.OrangeRed);
            var anim = new DoubleAnimation(1, 0, new Duration(TimeSpan.FromMilliseconds(500)));
            fn.Visual.BeginAnimation(OpacityProperty, anim);
            fn.Label .BeginAnimation(OpacityProperty, anim);
        }
    }
}
