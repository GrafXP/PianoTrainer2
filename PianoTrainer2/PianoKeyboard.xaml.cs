using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace PianoTrainer2
{
    public partial class PianoKeyboard : UserControl
    {
        private const int FirstNote = 21; // A0
        private const int LastNote = 108; // C8
        private const int NoteCount = LastNote - FirstNote + 1; // 88

        private const double WhiteKeyWidth = 14;
        private const double WhiteKeyHeight = 100;
        private const double BlackKeyWidth = 9;
        private const double BlackKeyHeight = 62;

        private readonly Rectangle[] _keys = new Rectangle[128];

        public static readonly DependencyProperty ActiveKeysProperty =
            DependencyProperty.Register(nameof(ActiveKeys), typeof(bool[]), typeof(PianoKeyboard),
                new PropertyMetadata(null, OnActiveKeysChanged));

        public bool[]? ActiveKeys
        {
            get => (bool[]?)GetValue(ActiveKeysProperty);
            set => SetValue(ActiveKeysProperty, value);
        }

        private static void OnActiveKeysChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            => ((PianoKeyboard)d).Refresh();

        public PianoKeyboard()
        {
            InitializeComponent();
            Loaded += (_, _) => BuildKeys();
        }

        private static bool IsBlack(int note) => (note % 12) is 1 or 3 or 6 or 8 or 10;

        private void BuildKeys()
        {
            KeyCanvas.Children.Clear();

            // Count white keys to set canvas width
            int whiteCount = 0;
            for (int n = FirstNote; n <= LastNote; n++)
                if (!IsBlack(n)) whiteCount++;

            double totalWidth = whiteCount * WhiteKeyWidth;
            KeyCanvas.Width = totalWidth;
            Width = totalWidth;

            // First pass: white keys
            int wi = 0;
            for (int n = FirstNote; n <= LastNote; n++)
            {
                if (IsBlack(n)) continue;
                var r = new Rectangle
                {
                    Width = WhiteKeyWidth - 1,
                    Height = WhiteKeyHeight,
                    Fill = Brushes.White,
                    Stroke = Brushes.Black,
                    StrokeThickness = 0.5,
                    Tag = n
                };
                Canvas.SetLeft(r, wi * WhiteKeyWidth);
                Canvas.SetTop(r, 0);
                Canvas.SetZIndex(r, 0);
                KeyCanvas.Children.Add(r);
                _keys[n] = r;
                wi++;
            }

            // Second pass: black keys
            wi = 0;
            for (int n = FirstNote; n <= LastNote; n++)
            {
                if (IsBlack(n))
                {
                    var r = new Rectangle
                    {
                        Width = BlackKeyWidth,
                        Height = BlackKeyHeight,
                        Fill = Brushes.Black,
                        Tag = n
                    };
                    // Position: offset from previous white key
                    double x = wi * WhiteKeyWidth - BlackKeyWidth / 2.0;
                    Canvas.SetLeft(r, x);
                    Canvas.SetTop(r, 0);
                    Canvas.SetZIndex(r, 1);
                    KeyCanvas.Children.Add(r);
                    _keys[n] = r;
                }
                else
                {
                    wi++;
                }
            }
        }

        private void Refresh()
        {
            var active = ActiveKeys;
            if (active == null) return;
            for (int n = FirstNote; n <= LastNote; n++)
            {
                if (_keys[n] == null) continue;
                bool on = n < active.Length && active[n];
                if (IsBlack(n))
                    _keys[n].Fill = on ? Brushes.DeepSkyBlue : Brushes.Black;
                else
                    _keys[n].Fill = on ? Brushes.DeepSkyBlue : Brushes.White;
            }
        }
    }
}
