using UnityEngine;

namespace RhythmGen
{
    /// <summary>
    /// Drop this on an object in your gameplay scene, assign an AudioClip
    /// (import setting: Load Type = Decompress On Load, so GetData works),
    /// and call Build() before starting playback. Analysis is offline/one-shot,
    /// so for longer tracks consider running it in the loading screen or
    /// caching the resulting ChartData to disk (e.g. JsonUtility.ToJson).
    /// </summary>
    public class ChartBuilder : MonoBehaviour
    {
        public AudioClip song;
        public Difficulty difficulty = Difficulty.Normal;
        public AnalysisSettings analysisSettings = new AnalysisSettings();
        public ChartGenSettings genSettings = new ChartGenSettings();

        public ChartData Chart { get; private set; }

        [ContextMenu("Build Chart Now")]
        public void Build()
        {
            if (song == null)
            {
                Debug.LogError("ChartBuilder: no AudioClip assigned.");
                return;
            }

            var analysis = AudioAnalyzer.Analyze(song, analysisSettings);
            Debug.Log($"ChartBuilder: estimated BPM = {analysis.bpm:F1}, " +
                      $"beat offset = {analysis.beatOffset:F3}s, raw onsets = {analysis.onsets.Count}");

            Chart = ChartGenerator.Generate(analysis, genSettings, difficulty);
            Debug.Log($"ChartBuilder: generated {Chart.notes.Count} notes for {difficulty}.");
        }

        // Example of how a gameplay controller would consume this:
        // foreach (var note in chart.Chart.notes) SpawnNote(note);
        // and drive spawn timing off (AudioSettings.dspTime) vs note.time,
        // the same way you'd schedule any rhythm-game note.
    }
}
