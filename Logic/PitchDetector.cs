using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GuitarTuner.Logic
{
    public static class PitchDetector
    {
        public static double DetectPitch(float[] buffer, int sample_rate)
        {
            int minLag = sample_rate / 1300;
            int maxLag = sample_rate / 60;

            double best_correlation = 0;
            int best_lag = -1;

            for (int lag = minLag; lag <= maxLag; ++lag)
            {
                double corr = 0;
                for (int i = 0; i < buffer.Length - lag; ++i)
                {
                    corr += buffer[i] * buffer[i + lag];
                }

                if (corr > best_correlation)
                {
                    best_correlation = corr;
                    best_lag = lag;
                }
            }

            if (best_lag <= 0 || best_lag == minLag || best_lag == maxLag) return 0;

            double corrLeft = 0;
            double corrRight = 0;
            for (int i = 0; i < buffer.Length - best_lag + 1; i++) corrLeft += buffer[i] * buffer[i + best_lag - 1];
            for (int i = 0; i < buffer.Length - best_lag - 1; i++) corrRight += buffer[i] * buffer[i + best_lag + 1];

            double denominator = (corrLeft - 2 * best_correlation + corrRight);
            if (Math.Abs(denominator) < 1e-6) { return (double)sample_rate / best_lag; }

            double delta = 0.5 * (corrLeft - corrRight) / denominator;

            delta = Math.Clamp(delta, -1.0, 1.0);

            double exactLag = best_lag + delta;
            return (double)sample_rate / exactLag;
        }
    }
}
