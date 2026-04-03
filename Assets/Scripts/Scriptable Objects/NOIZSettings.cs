using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NOIZ", menuName = "NOIZ Settings/Settings", order = 1)]
public class NOIZSettings : ScriptableObject
{
    public string artistName;
    public float noteDelay = 2;
    public float noteCooldown = 0.3f;
    public float fadeDuration = 1f;

    [Header("Audio Settings")]
    public float duckVolume = 0.2f;
    public float duckDuration = 0.1f;
    public float duckFadeDuration = 0.1f;
    public float restoreFadeDuration = 0.1f;

    [Header("Lane Settings")]
    public float noteStart = 20f;
    public float noteEnd = -5f;
    public float cutZone = 0.5f;

    [Header("Scoring Settings")]
    public int currentScore;
    public int currentCombo;
    public float scorePerNote = 100f;
    public float scorePerCombo = 10f;
    public int maxComboMultiplier = 8;
    public int minusScorePerMiss = 50;

    [Header("Haptic Settings")]
    public bool enableHaptics = true;

    public List<DeezerTrack> currentTracks;
}
