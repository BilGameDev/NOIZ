using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RhythmGen
{
    public struct OnsetEvent
    {
        public float time;      // seconds
        public float strength;  // 0..1 normalized within its band
        public int band;        // index into AnalysisSettings.bandEdges
        public float sustainEnd; // seconds; time == sustainEnd for pure taps
    }

    [Serializable]
    public class AnalysisSettings
    {
        [Tooltip("FFT frame size. Must be a power of two. 2048 @ 44.1kHz ≈ 46ms window.")]
        public int frameSize = 2048;

        [Tooltip("Samples between successive frames. 512 @ 44.1kHz ≈ 11.6ms resolution.")]
        public int hopSize = 512;

        [Tooltip("Band edges in Hz. N edges define N-1 bands... wait, N edges define N bands here: " +
                 "[edge0,edge1), [edge1,edge2), ... last band goes to Nyquist.")]
        public float[] bandEdgesHz = { 20f, 250f, 2000f, 8000f };

        [Tooltip("Adaptive threshold: local mean multiplier.")]
        public float thresholdMultiplier = 1.6f;

        [Tooltip("Adaptive threshold: window (in frames) used to compute local mean.")]
        public int thresholdWindowFrames = 20;

        [Tooltip("Minimum time between two onsets in the SAME band, seconds.")]
        public float minOnsetIntervalSec = 0.06f;

        [Tooltip("Fraction of an onset's peak energy that must be sustained to keep counting it as a held note.")]
        public float sustainEnergyRatio = 0.35f;

        [Tooltip("Maximum time a sustain can run without a fresh envelope check invalidating it, seconds.")]
        public float maxSustainSec = 8f;

        [Tooltip("Minimum BPM to consider during tempo estimation.")]
        public float minBpm = 70f;

        [Tooltip("Maximum BPM to consider during tempo estimation.")]
        public float maxBpm = 200f;
    }

    public class AnalysisResult
    {
        public float bpm;
        public float beatOffset;
        public float[] beatTimes;
        public List<OnsetEvent> onsets = new List<OnsetEvent>();
        public float songLength;
    }

    /// <summary>
    /// Runs a full offline analysis pass over an AudioClip: per-band spectral-flux
    /// onset detection, envelope-based sustain tracking, and autocorrelation-based
    /// tempo/beat-phase estimation. Requires the clip's load type to be
    /// DecompressOnLoad (or otherwise have readable sample data) so GetData works.
    /// </summary>
    public static class AudioAnalyzer
    {
        public static AnalysisResult Analyze(AudioClip clip, AnalysisSettings settings)
        {
            if (clip == null) throw new ArgumentNullException(nameof(clip));

            var samples = GetMonoSamples(clip);
            int sampleRate = clip.frequency;

            int frameSize = Mathf.NextPowerOfTwo(settings.frameSize);
            int hop = settings.hopSize;
            var window = FFT.HannWindow(frameSize);

            int numFrames = Math.Max(0, (samples.Length - frameSize) / hop);
            int numBins = frameSize / 2;
            int bandCount = settings.bandEdgesHz.Length; // last band extends to Nyquist
            int[] bandStartBin = new int[bandCount];
            int[] bandEndBin = new int[bandCount];
            for (int b = 0; b < bandCount; b++)
            {
                float lowHz = settings.bandEdgesHz[b];
                float highHz = (b + 1 < bandCount) ? settings.bandEdgesHz[b + 1] : sampleRate / 2f;
                bandStartBin[b] = HzToBin(lowHz, sampleRate, frameSize);
                bandEndBin[b] = Math.Max(bandStartBin[b] + 1, HzToBin(highHz, sampleRate, frameSize));
            }

            // Per-frame, per-band magnitude sum (used for flux + sustain envelope)
            var bandMag = new float[numFrames, bandCount];
            var real = new float[frameSize];
            var imag = new float[frameSize];

            for (int f = 0; f < numFrames; f++)
            {
                int start = f * hop;
                for (int i = 0; i < frameSize; i++)
                {
                    real[i] = samples[start + i] * window[i];
                    imag[i] = 0f;
                }
                FFT.Transform(real, imag);

                for (int b = 0; b < bandCount; b++)
                {
                    float sum = 0f;
                    for (int bin = bandStartBin[b]; bin < bandEndBin[b] && bin < numBins; bin++)
                        sum += Mathf.Sqrt(real[bin] * real[bin] + imag[bin] * imag[bin]);
                    bandMag[f, b] = sum;
                }
            }

            // Spectral flux per band (half-wave rectified frame-to-frame increase)
            var flux = new float[numFrames, bandCount];
            for (int f = 1; f < numFrames; f++)
                for (int b = 0; b < bandCount; b++)
                {
                    float d = bandMag[f, b] - bandMag[f - 1, b];
                    flux[f, b] = d > 0f ? d : 0f;
                }

            // Overall envelope (summed across bands) for tempo estimation
            var overallFlux = new float[numFrames];
            for (int f = 0; f < numFrames; f++)
            {
                float s = 0f;
                for (int b = 0; b < bandCount; b++) s += flux[f, b];
                overallFlux[f] = s;
            }

            float frameTime = hop / (float)sampleRate;

            var result = new AnalysisResult
            {
                songLength = samples.Length / (float)sampleRate
            };

            // Per-band peak picking with adaptive threshold + sustain tracking
            for (int b = 0; b < bandCount; b++)
            {
                float lastOnsetTime = -999f;
                int sustainFrame = -1;
                float sustainPeak = 0f;
                int sustainStartFrame = -1;

                for (int f = 1; f < numFrames - 1; f++)
                {
                    float local = LocalMean(flux, f, b, settings.thresholdWindowFrames, numFrames);
                    float threshold = local * settings.thresholdMultiplier + 1e-6f;
                    bool isPeak = flux[f, b] > threshold &&
                                  flux[f, b] >= flux[f - 1, b] &&
                                  flux[f, b] >= flux[f + 1, b];

                    float t = f * frameTime;

                    if (isPeak && (t - lastOnsetTime) >= settings.minOnsetIntervalSec)
                    {
                        // Close any prior open sustain in this band first
                        if (sustainStartFrame >= 0)
                            CloseSustain(result, b, sustainStartFrame, f - 1, frameTime, sustainPeak);

                        lastOnsetTime = t;
                        sustainStartFrame = f;
                        sustainPeak = bandMag[f, b];
                        sustainFrame = f;
                    }
                    else if (sustainStartFrame >= 0)
                    {
                        // Continue or close the current sustain based on envelope energy
                        float energyRatio = sustainPeak > 1e-6f ? bandMag[f, b] / sustainPeak : 0f;
                        float sustainDuration = (f - sustainStartFrame) * frameTime;
                        if (energyRatio < settings.sustainEnergyRatio || sustainDuration > settings.maxSustainSec)
                        {
                            CloseSustain(result, b, sustainStartFrame, f, frameTime, sustainPeak);
                            sustainStartFrame = -1;
                        }
                        else
                        {
                            sustainFrame = f;
                        }
                    }
                }

                if (sustainStartFrame >= 0)
                    CloseSustain(result, b, sustainStartFrame, numFrames - 1, frameTime, sustainPeak);
            }

            result.onsets = result.onsets.OrderBy(o => o.time).ToList();

            // Normalize strength per band to 0..1
            NormalizeStrengths(result.onsets, bandCount);

            // Tempo + beat phase estimation from the overall onset envelope
            EstimateTempo(overallFlux, frameTime, settings, out float bpm, out float beatOffset);
            result.bpm = bpm;
            result.beatOffset = beatOffset;
            result.beatTimes = BuildBeatGrid(bpm, beatOffset, result.songLength);

            return result;
        }

        private static void CloseSustain(AnalysisResult result, int band, int startFrame, int endFrame,
            float frameTime, float peakMag)
        {
            float startTime = startFrame * frameTime;
            float endTime = Mathf.Max(startTime, endFrame * frameTime);
            result.onsets.Add(new OnsetEvent
            {
                time = startTime,
                sustainEnd = endTime,
                band = band,
                strength = peakMag // normalized later
            });
        }

        private static void NormalizeStrengths(List<OnsetEvent> onsets, int bandCount)
        {
            for (int b = 0; b < bandCount; b++)
            {
                float max = 0f;
                foreach (var o in onsets) if (o.band == b) max = Mathf.Max(max, o.strength);
                if (max <= 1e-6f) continue;
                for (int i = 0; i < onsets.Count; i++)
                {
                    if (onsets[i].band != b) continue;
                    var o = onsets[i];
                    o.strength = Mathf.Clamp01(o.strength / max);
                    onsets[i] = o;
                }
            }
        }

        private static float LocalMean(float[,] flux, int frame, int band, int windowFrames, int numFrames)
        {
            int start = Math.Max(0, frame - windowFrames);
            int end = Math.Min(numFrames - 1, frame + windowFrames);
            float sum = 0f;
            int count = 0;
            for (int i = start; i <= end; i++) { sum += flux[i, band]; count++; }
            return count > 0 ? sum / count : 0f;
        }

        private static void EstimateTempo(float[] envelope, float frameTime, AnalysisSettings settings,
            out float bpm, out float beatOffset)
        {
            int n = envelope.Length;
            int minLag = Mathf.Max(1, Mathf.RoundToInt(60f / settings.maxBpm / frameTime));
            int maxLag = Mathf.Max(minLag + 1, Mathf.RoundToInt(60f / settings.minBpm / frameTime));
            maxLag = Mathf.Min(maxLag, n - 1);

            float bestScore = -1f;
            int bestLag = minLag;
            for (int lag = minLag; lag <= maxLag; lag++)
            {
                float score = 0f;
                for (int i = 0; i + lag < n; i++)
                    score += envelope[i] * envelope[i + lag];
                if (score > bestScore) { bestScore = score; bestLag = lag; }
            }

            bpm = 60f / (bestLag * frameTime);

            // Beat phase: try a handful of phase offsets within one period and
            // pick the one whose grid best aligns with envelope energy.
            int periodFrames = bestLag;
            float bestPhaseScore = -1f;
            int bestPhaseFrame = 0;
            int phaseSteps = Mathf.Min(periodFrames, 32);
            for (int p = 0; p < phaseSteps; p++)
            {
                int phaseFrame = Mathf.RoundToInt(p * (periodFrames / (float)phaseSteps));
                float score = 0f;
                for (int i = phaseFrame; i < n; i += periodFrames)
                    score += envelope[i];
                if (score > bestPhaseScore) { bestPhaseScore = score; bestPhaseFrame = phaseFrame; }
            }

            beatOffset = bestPhaseFrame * frameTime;
        }

        private static float[] BuildBeatGrid(float bpm, float offset, float songLength)
        {
            if (bpm <= 0f) return Array.Empty<float>();
            float beatDur = 60f / bpm;
            int count = Mathf.Max(0, Mathf.FloorToInt((songLength - offset) / beatDur) + 1);
            var beats = new float[count];
            for (int i = 0; i < count; i++) beats[i] = offset + i * beatDur;
            return beats;
        }

        private static int HzToBin(float hz, int sampleRate, int frameSize)
        {
            int bin = Mathf.RoundToInt(hz / sampleRate * frameSize);
            return Mathf.Clamp(bin, 0, frameSize / 2 - 1);
        }

        private static float[] GetMonoSamples(AudioClip clip)
        {
            int channels = clip.channels;
            var raw = new float[clip.samples * channels];
            clip.GetData(raw, 0);

            if (channels == 1) return raw;

            var mono = new float[clip.samples];
            for (int i = 0; i < clip.samples; i++)
            {
                float sum = 0f;
                for (int c = 0; c < channels; c++) sum += raw[i * channels + c];
                mono[i] = sum / channels;
            }
            return mono;
        }
    }
}
