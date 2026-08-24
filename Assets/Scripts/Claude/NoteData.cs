using System;
using System.Collections.Generic;

namespace RhythmGen
{
    public enum NoteType { Tap, Hold }

    public enum Difficulty { Easy, Normal, Hard, Expert }

    [Serializable]
    public struct NoteData
    {
        public float time;       // seconds, song time
        public int lane;         // 0..laneCount-1
        public NoteType type;
        public float holdDuration; // seconds, only used if type == Hold
        public float strength;   // 0..1, onset strength (useful for VFX scaling)

        public override string ToString() =>
            $"[{time:F3}s] lane {lane} {type} strength {strength:F2}" +
            (type == NoteType.Hold ? $" hold {holdDuration:F2}s" : "");
    }

    [Serializable]
    public class ChartData
    {
        public float bpm;
        public float beatOffset;       // seconds, phase of the first beat
        public int laneCount;
        public Difficulty difficulty;
        public List<NoteData> notes = new List<NoteData>();

        public void SortByTime() => notes.Sort((a, b) => a.time.CompareTo(b.time));
    }
}
