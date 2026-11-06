using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GuitarTuner.Logic
{
    public class NoteAnalyzer
    {
        #region Constants
        private const double A4_standard = 440.0;
        private const int halfsteps_count = 12;
        private const int A_note_index = 9;
        private const int A4_octave_number = 4;
        #endregion

        private static readonly string[] NoteNames =
         { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };

        private static readonly (string Name, double Freq)[] OpenStrings = 
        { 
            ("E2", 82.41),
            ("A2", 110.00),
            ("D3", 146.83),
            ("G3", 196.00),
            ("B3", 246.94),
            ("E4", 329.63)
        };

        public static (string Note, double TargetFreq) Analyze(double freq, double a4Ref = 440.0)
        {
            if (freq <= 0) return ("-", 0);

            double ratio = a4Ref / A4_standard;
            double adjustFreq = freq / ratio;

            double halfStepsFromA4 = halfsteps_count * Math.Log2(adjustFreq / A4_standard);
            int roundedHalfSteps = (int)Math.Round(halfStepsFromA4);

            double targetFreq = A4_standard * Math.Pow(2, roundedHalfSteps / (float)halfsteps_count) * ratio;

            int absoluteNoteIndex = A_note_index + roundedHalfSteps;
            int noteIndex = (absoluteNoteIndex % halfsteps_count + halfsteps_count) % halfsteps_count;
            int octave = A4_octave_number + (absoluteNoteIndex >= 0
                ? absoluteNoteIndex / halfsteps_count
                : (absoluteNoteIndex - (halfsteps_count - 1)) / halfsteps_count);

            string noteName = $"{NoteNames[noteIndex]}{octave}";

            return (noteName, targetFreq);
        }
    }
}