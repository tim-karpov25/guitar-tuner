using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GuitarTuner.Logic
{
    public class NoteAnalyzer
    {
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

            double ratio = a4Ref / 440.0;
            double adjustFreq = freq / ratio;

            double halfStepsFromA4 = 12 * Math.Log2(adjustFreq / 440.0);
            int roundedHalfSteps = (int)Math.Round(halfStepsFromA4);

            double targetFreq = 440.0 * Math.Pow(2, roundedHalfSteps / 12.0) * ratio;

            int absoluteNoteIndex = 9 + roundedHalfSteps;
            int noteIndex = (absoluteNoteIndex % 12 + 12) % 12;
            int octave = 4 + (absoluteNoteIndex >= 0 ? absoluteNoteIndex / 12 : (absoluteNoteIndex - 11) / 12);

            string noteName = $"{NoteNames[noteIndex]}{octave}";

            return (noteName, targetFreq);
        }
    }
}