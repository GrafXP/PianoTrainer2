using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace PianoTrainer2
{
    public partial class PianoKeyboard : UserControl
    {
        private const int FirstNote = 21;
        private const int LastNote  = 108;

        private const double WhiteKeyWidth  = 14;
        private const double WhiteKeyHeight = 100;
        private const double BlackKeyWidth  = 9;
        private const double BlackKeyHeight = 62;

        private readonly Rectangle[] _keys = new Rectangle[128];

        // ── ActiveKeys (played / currently pressed) ───────────────────────────
        public static readonly DependencyProperty ActiveKeysProperty =
            DependencyProperty.Register(nameof(ActiveKeys), typeof(bool[]), typeof(PianoKeyboard),
                new PropertyMetadata(null, (d, _) => ((PianoKeyboard)d).Refresh()));

        public bool[]? ActiveKeys
        {
            get => (bool[]?)GetValue(ActiveKeysProperty);
            set => SetValue(ActiveKeysProperty, value);
        }

        // ── PendingKeys (about to be hit — dim hint) ──────────────────────────
        public static readonly DependencyProperty PendingKeysProperty =
            DependencyProperty.Register(nameof(PendingKeys), typeof(bool[]), typeof(PianoKeyboard),
                new PropertyMetadata(null, (d, _) => ((PianoKeyboard)d).Refresh()));

        public bool[]? PendingKeys
        {
            get => (bool[]?)GetValue(PendingKeysProperty);
            set => SetValue(PendingKeysProperty, value);
        }

        private static readonly SolidColorBrush _pendingWhite = new(Color.FromRgb(255, 230, 100));
        private static readonly SolidColorBrush _pendingBlack = new(Color.FromRgb(160, 120, 0));

        public PianoKeyboard()
        {
            InitializeComponent();
            Loaded += (_, _) => BuildKeys();
        }

        private static bool IsBlack(int note) => (note % 12) is 1 or 3 or 6 or 8 or 10;

        private void BuildKeys()
        {
            KeyCanvas.Children.Clear();

            int whiteCount = 0;
            for (int n = FirstNote; n <= LastNote; n++)
                if (!IsBlack(n)) whiteCount++;

            double totalWidth = whiteCount * WhiteKeyWidth;
            KeyCanvas.Width = totalWidth;
            Width = totalWidth;

            // White keys
            int wi = 0;
            for (int n = FirstNote; n <= LastNote; n++)
            {
                if (IsBlack(n)) continue;
                var r = new Rectangle
                {
                    Width = WhiteKeyWidth - 1, Height = WhiteKeyHeight,
                    Fill = Brushes.White, Stroke = Brushes.Black, StrokeThickness = 0.5, Tag = n
                };
                Canvas.SetLeft(r, wi * WhiteKeyWidth);
                Canvas.SetTop(r, 0);
                Canvas.SetZIndex(r, 0);
                KeyCanvas.Children.Add(r);
                _keys[n] = r;
                wi++;
            }

            // Black keys
            wi = 0;
            for (int n = FirstNote; n <= LastNote; n++)
            {
                if (IsBlack(n))
                {
                    var r = new Rectangle
                    {
                        Width = BlackKeyWidth, Height = BlackKeyHeight,
                        Fill = Brushes.Black, Tag = n
                    };
                    double x = wi * WhiteKeyWidth - BlackKeyWidth / 2.0;
                    Canvas.SetLeft(r, x);
                    Canvas.SetTop(r, 0);
                    Canvas.SetZIndex(r, 1);
                    KeyCanvas.Children.Add(r);
                    _keys[n] = r;
                }
                else wi++;
            }
        }

        private void Refresh()
        {
            var active  = ActiveKeys;
            var pending = PendingKeys;
            for (int n = FirstNote; n <= LastNote; n++)
            {
                if (_keys[n] == null) continue;
                bool isActive  = active  != null && n < active.Length  && active[n];
                bool isPending = pending != null && n < pending.Length && pending[n];
                bool black = IsBlack(n);

                _keys[n].Fill = isActive  ? Brushes.DeepSkyBlue :
                                isPending ? (black ? _pendingBlack : _pendingWhite) :
                                            (black ? Brushes.Black  : Brushes.White);
            }
        }
    }
}
