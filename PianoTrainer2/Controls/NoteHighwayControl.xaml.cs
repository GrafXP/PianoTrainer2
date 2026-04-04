using PianoTrainer2.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace PianoTrainer2.Controls
{
    public enum TrainingMode { Continuous, WaitForPress }

    public enum HitState { Active, Frozen, Hit, Miss }

    public class FallingNote
    {
        public SongNote Source { get; init; } = null!;
        public Rectangle Visual { get; init; } = null!;
        public TextBlock Label { get; init; } = null!;
        public HitState State { get; set; } = HitState.Active;
        public double CanvasTop { get; set; }
        public double FrozenAt { get; set; }   // ms when frozen
        public double KeyDownAt { get; set; } = -1;
        public bool KeyIsDown { get; set; }
    }

    public partial class NoteHighwayControl : UserControl
    {
        // ── Public API ───────────────────────────────────────────────────────
        public TrainingMode Mode { get; set; } = TrainingMode.Continuous;
        public double PixelsPerMs { get; set; } = 0.25;

        public event Action<int>? NoteHit;
        public event Action<int>? NoteMissed;

        // ── State ─────────────────────────────────────────────────────────
        private Song? _song;
        private readonly List<FallingNote> _falling = new();
        private int _nextNoteIndex;
        private double _playbackMs;
        private TimeSpan _lastRender;
        private bool _playing;
        private bool _frozen;

        private Rectangle? _hitZone;
        private const double HitZoneHeight = 10;
        private const double HitWindowMs = 150;
        private const double FreezeGraceMs = 3000;
        // How many ms ahead of the hit zone we spawn notes (fixed, independent of control height)
        private const double LookaheadMs = 4000;

        public NoteHighwayControl()
        {
            InitializeComponent();
            Loaded += (_, _) => BuildHitZone();
            SizeChanged += (_, _) => BuildHitZone();
        }

        private void BuildHitZone()
        {
            if (_hitZone != null) HighwayCanvas.Children.Remove(_hitZone);
            _hitZone = new Rectangle
            {
                Width = PianoKeyboardLayout.TotalWidth,
                Height = HitZoneHeight,
                Fill = new SolidColorBrush(Color.FromArgb(80, 255, 255, 255))
            };
            Canvas.SetLeft(_hitZone, 0);
            Canvas.SetTop(_hitZone, ActualHeight - HitZoneHeight);
            Canvas.SetZIndex(_hitZone, 10);
            HighwayCanvas.Children.Add(_hitZone);
        }

        // ── Public control methods ────────────────────────────────────────
        public void LoadSong(Song song)
        {
            Stop();
            _song = song;
        }

        public void Start()
        {
            if (_song == null) return;
            _falling.Clear();
            HighwayCanvas.Children.Clear();
            BuildHitZone();
            _nextNoteIndex = 0;
            _playbackMs = 0; // playbackMs = song time in ms; notes spawn LookaheadMs before their StartMs
            _frozen = false;
            _playing = true;
            CompositionTarget.Rendering += OnRendering;
        }

        public void Stop()
        {
            _playing = false;
            CompositionTarget.Rendering -= OnRendering;
            HighwayCanvas.Children.Clear();
            _falling.Clear();
            BuildHitZone();
        }

        // ── MIDI input ────────────────────────────────────────────────────
        public void KeyPressed(int noteNumber)
        {
            if (!_playing) return;
            foreach (var fn in _falling.Where(f => f.Source.NoteNumber == noteNumber))
            {
                if (fn.State == HitState.Frozen)
                {
                    fn.State = HitState.Hit;
                    fn.KeyDownAt = _playbackMs;
                    fn.KeyIsDown = true;
                    _frozen = false;
                    FlashNote(fn, true);
                    return;
                }
                if (fn.State == HitState.Active)
                {
                    double noteHitMs = fn.Source.StartMs;
                    double timeDiff = Math.Abs(_playbackMs - noteHitMs);
                    if (timeDiff <= HitWindowMs)
                    {
                        fn.State = HitState.Hit;
                        fn.KeyDownAt = _playbackMs;
                        fn.KeyIsDown = true;
                        FlashNote(fn, true);
                        return;
                    }
                }
            }
        }

        public void KeyReleased(int noteNumber)
        {
            if (!_playing) return;
            foreach (var fn in _falling.Where(f => f.Source.NoteNumber == noteNumber && f.KeyIsDown))
            {
                fn.KeyIsDown = false;
                double held = _playbackMs - fn.KeyDownAt;
                bool goodDuration = held >= fn.Source.DurationMs * 0.8;
                if (!goodDuration)
                {
                    fn.State = HitState.Miss;
                    FlashNote(fn, false);
                    NoteMissed?.Invoke(noteNumber);
                }
                else
                {
                    NoteHit?.Invoke(noteNumber);
                }
            }
        }

        // ── Animation loop ────────────────────────────────────────────────
        private void OnRendering(object? sender, EventArgs e)
        {
            if (!_playing || _song == null) return;

            var args = (RenderingEventArgs)e;
            if (_lastRender == TimeSpan.Zero) { _lastRender = args.RenderingTime; return; }
            double deltaMs = (args.RenderingTime - _lastRender).TotalMilliseconds;
            _lastRender = args.RenderingTime;

            if (!_frozen)
                _playbackMs += deltaMs;

            SpawnNotes();
            MoveNotes(deltaMs);
            CheckHitZone();
            RemoveExpired();

            // Check song end
            if (_playbackMs > _song.TotalDurationMs + 3000 && _falling.Count == 0)
                Stop();
        }

        private void SpawnNotes()
        {
            if (_song == null) return;
            double spawnThreshold = _playbackMs + LookaheadMs;
            while (_nextNoteIndex < _song.Notes.Count &&
                   _song.Notes[_nextNoteIndex].StartMs <= spawnThreshold)
            {
                var note = _song.Notes[_nextNoteIndex++];
                if (!PianoKeyboardLayout.IsInRange(note.NoteNumber)) continue;

                double x = PianoKeyboardLayout.GetKeyXCenter(note.NoteNumber)
                           - PianoKeyboardLayout.GetKeyWidth(note.NoteNumber) / 2.0;
                double w = PianoKeyboardLayout.GetKeyWidth(note.NoteNumber);
                double h = Math.Max(10, note.DurationMs * PixelsPerMs);

                bool isBlack = PianoKeyboardLayout.IsBlack(note.NoteNumber);
                var fill = isBlack
                    ? new SolidColorBrush(Color.FromRgb(70, 130, 180))
                    : new SolidColorBrush(Color.FromRgb(100, 180, 255));

                var rect = new Rectangle
                {
                    Width = w - 1,
                    Height = h,
                    Fill = fill,
                    RadiusX = 3,
                    RadiusY = 3
                };

                var label = new TextBlock
                {
                    Text = MainViewModel.NoteName(note.NoteNumber),
                    FontSize = 8,
                    Foreground = Brushes.White,
                    IsHitTestVisible = false
                };

                // msUntilHit > 0 means note hasn't reached hit zone yet
                // note bottom should be at hitY when playbackMs == note.StartMs
                double hitY = ActualHeight - HitZoneHeight;
                double msUntilHit = note.StartMs - _playbackMs;
                double startTop = hitY - msUntilHit * PixelsPerMs - h;

                Canvas.SetLeft(rect, x);
                Canvas.SetTop(rect, startTop);
                Canvas.SetZIndex(rect, 5);
                Canvas.SetLeft(label, x);
                Canvas.SetTop(label, startTop + 2);
                Canvas.SetZIndex(label, 6);

                HighwayCanvas.Children.Add(rect);
                HighwayCanvas.Children.Add(label);

                _falling.Add(new FallingNote
                {
                    Source = note,
                    Visual = rect,
                    Label = label,
                    CanvasTop = startTop
                });
            }
        }

        private void MoveNotes(double deltaMs)
        {
            double move = deltaMs * PixelsPerMs;
            foreach (var fn in _falling)
            {
                if (fn.State == HitState.Frozen) continue;
                // In WaitForPress mode the whole timeline is frozen; only non-frozen notes move
                if (_frozen && fn.State == HitState.Active) continue;
                fn.CanvasTop += move;
                Canvas.SetTop(fn.Visual, fn.CanvasTop);
                Canvas.SetTop(fn.Label, fn.CanvasTop + 2);
            }
        }

        private void CheckHitZone()
        {
            double hitY = ActualHeight - HitZoneHeight;

            foreach (var fn in _falling.Where(f => f.State == HitState.Active))
            {
                double bottom = fn.CanvasTop + fn.Visual.Height;
                if (bottom >= hitY)
                {
                    if (Mode == TrainingMode.WaitForPress)
                    {
                        fn.State = HitState.Frozen;
                        fn.FrozenAt = _playbackMs;
                        _frozen = true;
                    }
                    // Continuous: miss if bottom passes beyond hit zone + grace
                    else if (fn.CanvasTop > hitY + HitZoneHeight)
                    {
                        fn.State = HitState.Miss;
                        FlashNote(fn, false);
                        NoteMissed?.Invoke(fn.Source.NoteNumber);
                    }
                }
            }

            // Freeze timeout in wait mode
            if (Mode == TrainingMode.WaitForPress)
            {
                foreach (var fn in _falling.Where(f => f.State == HitState.Frozen))
                {
                    if (_playbackMs - fn.FrozenAt > FreezeGraceMs)
                    {
                        fn.State = HitState.Miss;
                        FlashNote(fn, false);
                        NoteMissed?.Invoke(fn.Source.NoteNumber);
                        _frozen = _falling.Any(f => f.State == HitState.Frozen);
                    }
                }
            }
        }

        private void RemoveExpired()
        {
            var toRemove = _falling.Where(f =>
                f.State is HitState.Hit or HitState.Miss &&
                !f.KeyIsDown &&
                fn_IsOffscreen(f)).ToList();

            foreach (var fn in toRemove)
            {
                HighwayCanvas.Children.Remove(fn.Visual);
                HighwayCanvas.Children.Remove(fn.Label);
                _falling.Remove(fn);
            }
        }

        private bool fn_IsOffscreen(FallingNote fn)
            => fn.CanvasTop > ActualHeight + 50 || fn.Visual.Opacity < 0.05;

        private void FlashNote(FallingNote fn, bool hit)
        {
            fn.Visual.Fill = hit
                ? new SolidColorBrush(Colors.LimeGreen)
                : new SolidColorBrush(Colors.OrangeRed);

            var anim = new DoubleAnimation(1, 0, new Duration(TimeSpan.FromMilliseconds(400)));
            fn.Visual.BeginAnimation(OpacityProperty, anim);
            fn.Label.BeginAnimation(OpacityProperty, anim);
        }
    }
}
