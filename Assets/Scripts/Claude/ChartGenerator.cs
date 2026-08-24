using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RhythmGen
{
    [Serializable]
    public class ChartGenSettings
    {
        [Tooltip("Number of playable lanes.")]
        public int laneCount = 4;

        [Tooltip("Quantize onsets to the nearest grid subdivision (e.g. 4 = 16th notes if beat = quarter note).")]
        public int gridSubdivision = 4;

        [Tooltip("Max seconds an onset can be off-grid and still get snapped (beyond this it's dropped as noise).")]
        public float maxSnapErrorSec = 0.09f;

        [Tooltip("Discard sustains shorter than this and treat them as taps instead.")]
        public float minHoldDurationSec = 0.18f;

        [Tooltip("Never allow more simultaneous notes than this, regardless of difficulty.")]
        public int hardMaxSimultaneousLanes = 2;

        [Range(0, 1)]
        [Tooltip("Chance to intentionally skip a low-strength onset even if it passes the difficulty filter, " +
                 "purely to keep patterns from feeling like a machine-gun of notes.")]
        public float breathingRoomSkipChance = 0.15f;

        [Tooltip("Avoid the same lane repeating more than this many times in a row (0 = no limit).")]
        public int maxLaneRepeat = 3;
    }

    public static class ChartGenerator
    {
        // Per-difficulty: minimum onset strength (percentile-based, 0..1) and max notes/sec allowed.
        private static readonly Dictionary<Difficulty, (float minStrength, float maxNotesPerSec)> DifficultyProfile =
            new()
            {
                { Difficulty.Easy,    (0.55f, 2.5f) },
                { Difficulty.Normal,  (0.40f, 4.0f) },
                { Difficulty.Hard,    (0.25f, 6.0f) },
                { Difficulty.Expert,  (0.12f, 9.0f) },
            };

        public static ChartData Generate(AnalysisResult analysis, ChartGenSettings genSettings, Difficulty difficulty)
        {
            var chart = new ChartData
            {
                bpm = analysis.bpm,
                beatOffset = analysis.beatOffset,
                laneCount = genSettings.laneCount,
                difficulty = difficulty
            };

            if (analysis.bpm <= 0f || analysis.onsets.Count == 0) return chart;

            var (minStrength, maxNotesPerSec) = DifficultyProfile[difficulty];
            float gridStep = (60f / analysis.bpm) / genSettings.gridSubdivision;

            // 1. Quantize + filter by strength
            var candidates = new List<OnsetEvent>();
            foreach (var o in analysis.onsets)
            {
                if (o.strength < minStrength) continue;

                float snapped = SnapToGrid(o.time, analysis.beatOffset, gridStep);
                if (Mathf.Abs(snapped - o.time) > genSettings.maxSnapErrorSec) continue;

                var snappedOnset = o;
                float shift = snapped - o.time;
                snappedOnset.time = snapped;
                snappedOnset.sustainEnd = Mathf.Max(snapped, o.sustainEnd + shift);
                candidates.Add(snappedOnset);
            }

            // Multiple bands can snap to the same grid slot; keep the strongest per (time, band)
            candidates = candidates
                .GroupBy(o => (o.time, o.band))
                .Select(g => g.OrderByDescending(x => x.strength).First())
                .OrderBy(o => o.time)
                .ToList();

            // 2. Density control: enforce max notes/sec via random-ish thinning weighted by strength
            candidates = ThinByDensity(candidates, maxNotesPerSec);

            // 3. Breathing room: occasionally drop weak onsets even after filtering
            var rng = new System.Random(12345); // deterministic; swap for a seeded run-specific value if desired
            candidates = candidates
                .Where(o => o.strength > 0.6f || rng.NextDouble() > genSettings.breathingRoomSkipChance)
                .ToList();

            // 4. Lane assignment + hold conversion + repeat smoothing
            int lastLane = -1;
            int repeatCount = 0;
            int bandCount = 4; // matches default AnalysisSettings.bandEdgesHz length; adjust if you change bands

            foreach (var o in candidates)
            {
                int lane = AssignLane(o.band, bandCount, genSettings.laneCount, lastLane, repeatCount,
                    genSettings.maxLaneRepeat, rng);

                if (lane == lastLane) repeatCount++;
                else repeatCount = 0;
                lastLane = lane;

                float sustainLen = o.sustainEnd - o.time;
                bool isHold = sustainLen >= genSettings.minHoldDurationSec;

                chart.notes.Add(new NoteData
                {
                    time = o.time,
                    lane = lane,
                    type = isHold ? NoteType.Hold : NoteType.Tap,
                    holdDuration = isHold ? sustainLen : 0f,
                    strength = o.strength
                });
            }

            // 5. Cap simultaneous notes (chords) to hardMaxSimultaneousLanes
            chart.notes = CapChords(chart.notes, genSettings.hardMaxSimultaneousLanes);

            chart.SortByTime();
            return chart;
        }

        private static float SnapToGrid(float time, float offset, float gridStep)
        {
            float rel = time - offset;
            float snappedRel = Mathf.Round(rel / gridStep) * gridStep;
            return offset + snappedRel;
        }

        private static List<OnsetEvent> ThinByDensity(List<OnsetEvent> sorted, float maxNotesPerSec)
        {
            if (sorted.Count == 0) return sorted;
            var result = new List<OnsetEvent>();
            float windowStart = sorted[0].time;
            var window = new List<OnsetEvent>();

            void FlushWindow(float windowEnd)
            {
                if (window.Count == 0) return;
                int allowed = Mathf.Max(1, Mathf.RoundToInt(maxNotesPerSec * (windowEnd - windowStart)));
                var kept = window.OrderByDescending(o => o.strength).Take(allowed).OrderBy(o => o.time);
                result.AddRange(kept);
                window.Clear();
            }

            foreach (var o in sorted)
            {
                if (o.time - windowStart > 1f) // 1-second sliding window
                {
                    FlushWindow(o.time);
                    windowStart = o.time;
                }
                window.Add(o);
            }
            FlushWindow(sorted[^1].time + 0.01f);

            return result.OrderBy(o => o.time).ToList();
        }

        private static int AssignLane(int band, int bandCount, int laneCount, int lastLane, int repeatCount,
            int maxRepeat, System.Random rng)
        {
            // Map frequency band to a lane range so low sounds cluster on one side,
            // high sounds on the other -- mirrors how hand-charted games often use
            // pitch/instrument to imply lane, which reads as "intentional" to players.
            float t = bandCount > 1 ? band / (float)(bandCount - 1) : 0f;
            int center = Mathf.RoundToInt(t * (laneCount - 1));

            // Pick from a small spread around the mapped center so the same band
            // doesn't always hit the exact same lane (that reads as robotic).
            int spread = Mathf.Max(1, laneCount / 3);
            int lane = Mathf.Clamp(center + rng.Next(-spread, spread + 1), 0, laneCount - 1);

            if (maxRepeat > 0 && lane == lastLane && repeatCount >= maxRepeat)
            {
                // Force a different lane to break the streak
                int alt = (lane + 1 + rng.Next(laneCount - 1)) % laneCount;
                lane = alt;
            }

            return lane;
        }

        private static List<NoteData> CapChords(List<NoteData> notes, int maxSimultaneous)
        {
            var groups = notes.GroupBy(n => Mathf.RoundToInt(n.time * 1000f)); // group by ms to catch exact-time chords
            var result = new List<NoteData>();
            foreach (var g in groups)
            {
                if (g.Count() <= maxSimultaneous)
                {
                    result.AddRange(g);
                }
                else
                {
                    result.AddRange(g.OrderByDescending(n => n.strength).Take(maxSimultaneous));
                }
            }
            return result;
        }
    }
}
