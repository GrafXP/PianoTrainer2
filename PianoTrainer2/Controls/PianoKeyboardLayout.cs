namespace PianoTrainer2.Controls
{
    public static class PianoKeyboardLayout
    {
        public const int FirstNote = 21;  // A0
        public const int LastNote = 108;  // C8
        public const double WhiteKeyWidth = 14.0;
        public const double BlackKeyWidth = 9.0;
        public const double WhiteKeyHeight = 100.0;
        public const double BlackKeyHeight = 62.0;

        // Total canvas width: count white keys
        public static readonly double TotalWidth;

        private static readonly double[] _keyXCenter = new double[128];
        private static readonly double[] _keyWidth = new double[128];
        private static readonly bool[] _isBlack = new bool[128];

        static PianoKeyboardLayout()
        {
            // Precompute
            int whiteIndex = 0;
            // First pass: white keys
            for (int n = FirstNote; n <= LastNote; n++)
            {
                _isBlack[n] = (n % 12) is 1 or 3 or 6 or 8 or 10;
                if (!_isBlack[n])
                {
                    _keyXCenter[n] = whiteIndex * WhiteKeyWidth + (WhiteKeyWidth - 1) / 2.0;
                    _keyWidth[n] = WhiteKeyWidth - 1;
                    whiteIndex++;
                }
            }
            TotalWidth = whiteIndex * WhiteKeyWidth;

            // Second pass: black keys (positioned between white keys)
            whiteIndex = 0;
            for (int n = FirstNote; n <= LastNote; n++)
            {
                if (_isBlack[n])
                {
                    double x = whiteIndex * WhiteKeyWidth - BlackKeyWidth / 2.0;
                    _keyXCenter[n] = x + BlackKeyWidth / 2.0;
                    _keyWidth[n] = BlackKeyWidth;
                }
                else
                {
                    whiteIndex++;
                }
            }
        }

        public static bool IsBlack(int noteNumber) => _isBlack[noteNumber];
        public static double GetKeyXCenter(int noteNumber) => _keyXCenter[noteNumber];
        public static double GetKeyWidth(int noteNumber) => _keyWidth[noteNumber];
        public static bool IsInRange(int noteNumber) => noteNumber >= FirstNote && noteNumber <= LastNote;
    }
}
