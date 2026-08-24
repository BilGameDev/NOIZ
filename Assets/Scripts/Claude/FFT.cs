using System;

namespace RhythmGen
{
    /// <summary>
    /// Minimal iterative radix-2 Cooley-Tukey FFT. Works in-place on
    /// power-of-two length real/imaginary arrays. Not the fastest possible
    /// implementation, but fast enough to analyze a full song offline in
    /// well under a second on desktop hardware.
    /// </summary>
    public static class FFT
    {
        public static void Transform(float[] real, float[] imag)
        {
            int n = real.Length;
            if (n != imag.Length)
                throw new ArgumentException("real/imag arrays must be same length");
            if ((n & (n - 1)) != 0)
                throw new ArgumentException("FFT length must be a power of two");

            // Bit-reversal permutation
            for (int i = 1, j = 0; i < n; i++)
            {
                int bit = n >> 1;
                for (; (j & bit) != 0; bit >>= 1)
                    j ^= bit;
                j ^= bit;
                if (i < j)
                {
                    (real[i], real[j]) = (real[j], real[i]);
                    (imag[i], imag[j]) = (imag[j], imag[i]);
                }
            }

            // Iterative butterfly
            for (int len = 2; len <= n; len <<= 1)
            {
                double angStep = -2.0 * Math.PI / len;
                float wRealStep = (float)Math.Cos(angStep);
                float wImagStep = (float)Math.Sin(angStep);

                for (int i = 0; i < n; i += len)
                {
                    float wReal = 1f, wImag = 0f;
                    int half = len >> 1;
                    for (int k = 0; k < half; k++)
                    {
                        int a = i + k;
                        int b = a + half;

                        float bReal = real[b] * wReal - imag[b] * wImag;
                        float bImag = real[b] * wImag + imag[b] * wReal;

                        real[b] = real[a] - bReal;
                        imag[b] = imag[a] - bImag;
                        real[a] += bReal;
                        imag[a] += bImag;

                        float nextWReal = wReal * wRealStep - wImag * wImagStep;
                        float nextWImag = wReal * wImagStep + wImag * wRealStep;
                        wReal = nextWReal;
                        wImag = nextWImag;
                    }
                }
            }
        }

        /// <summary>Builds a Hann window of the given size.</summary>
        public static float[] HannWindow(int size)
        {
            var w = new float[size];
            for (int i = 0; i < size; i++)
                w[i] = 0.5f - 0.5f * (float)Math.Cos(2.0 * Math.PI * i / (size - 1));
            return w;
        }

        /// <summary>Smallest power of two >= n.</summary>
        public static int NextPowerOfTwo(int n)
        {
            int p = 1;
            while (p < n) p <<= 1;
            return p;
        }
    }
}
