# Procedural Rhythm Chart Generator (Unity)

Five scripts, drop into any Unity project (2020+, no external packages needed):

- `FFT.cs` — iterative radix-2 FFT + Hann window helper.
- `AudioAnalyzer.cs` — offline analysis: multi-band spectral-flux onset
  detection, sustain/hold envelope tracking, autocorrelation tempo & beat-phase
  estimation.
- `NoteData.cs` — `NoteData` / `ChartData` structs used everywhere else.
- `ChartGenerator.cs` — turns raw onsets into a playable chart: beat-grid
  quantization, difficulty-based density control, lane assignment with
  repeat-smoothing, hold conversion, chord capping.
- `ChartBuilder.cs` — example `MonoBehaviour` that wires the two together.

## Quick start

1. Import your song. In the import settings set **Load Type = Decompress On
   Load** (streaming clips won't let you call `GetData`).
2. Add `ChartBuilder` to a GameObject, assign the clip, right-click the
   component → **Build Chart Now** (or call `Build()` from code).
3. `chartBuilder.Chart.notes` is your `List<NoteData>` — feed it into whatever
   spawns your Guitar-Hero-style notes, scheduling against
   `AudioSettings.dspTime` the same way you would with a hand-authored chart.

## Why this should feel hand-charted, not generated

The analyzer alone gives you a firehose of timestamps — that's the "sounds
robotic" failure mode. The quality lives in `ChartGenerator`:

- **Grid quantization** snaps every note to a 16th-note grid derived from the
  estimated BPM, so nothing feels rhythmically "off."
- **Difficulty profiles** control both a strength threshold *and* a max
  notes/sec cap, so Easy isn't just "the same chart with random notes
  deleted" — it's built around what a beginner can physically play.
- **Lane assignment by frequency band** means bass hits, mids, and highs
  land in different, semi-consistent lane regions — mirroring how a human
  charter would map instrument/pitch to lane — with just enough randomness
  in the spread that it doesn't feel mechanically repetitive.
- **Repeat smoothing** stops the same lane firing forever in a row.
- **Breathing room skip chance** intentionally throws away some low-strength
  onsets even after filtering — real charts have rests; pure onset-following
  doesn't.
- **Chord capping** prevents "3 notes at once, faster than a human can hit."

## Tuning knobs worth playing with first

- `AnalysisSettings.bandEdgesHz` — the default `[20, 250, 2000, 8000]` splits
  roughly into bass/kick, mids/vocals-guitar, highs/cymbals. Tune per genre.
- `AnalysisSettings.thresholdMultiplier` — lower = more onsets detected
  (more sensitive), higher = fewer, stronger-only onsets.
- `ChartGenSettings.gridSubdivision` — 4 = sixteenth notes at the estimated
  BPM; bump to 8 for very fast/technical genres, drop to 2 for simpler tracks.
- `ChartGenSettings.breathingRoomSkipChance` / `maxLaneRepeat` — these two
  are the biggest levers on "does this feel handmade."

## Known limitations / next steps

- Tempo estimation assumes a roughly constant BPM. Songs with tempo changes
  will need segment-wise re-analysis (run `Analyze` on windows and re-estimate
  BPM per segment) — happy to build that next if you need it.
- Lane assignment is currently band-based only. A nice upgrade: pull a rough
  pitch estimate (e.g. autocorrelation or a simple YIN detector on the mid
  band) and use actual note pitch for lane, not just frequency band energy —
  gets you closer to true "melody-follows-lane" charts.
- Analysis is currently synchronous and will hitch on a long song. For
  production, run it on a background thread (the analyzer has no
  Unity-main-thread dependencies except `AudioClip.GetData`, so you can pull
  samples on the main thread then hand off to a thread/Job for the rest) or
  cache generated charts to disk instead of analyzing every load.
