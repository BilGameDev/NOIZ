using UnityEngine;

public class ScoreHandler : MonoBehaviour
{
    public float currentScore;
    public float currentCombo;

    public static event System.Action<float> OnScoreChanged;
    public static event System.Action<float> OnComboChanged;
    void OnEnable()
    {
        NOIZEventHandler.OnHitNote += HandleCut;
        NOIZEventHandler.OnMissNote += HandleMiss;
        NOIZEventHandler.OnWrongCut += HandleWrongCut;
    }

    void OnDisable()
    {
        NOIZEventHandler.OnHitNote -= HandleCut;
        NOIZEventHandler.OnMissNote -= HandleMiss;
        NOIZEventHandler.OnWrongCut -= HandleWrongCut;
    }

    private void HandleMiss()
    {
        currentCombo = 0;
        currentScore = Mathf.Max(0, currentScore - NOIZManager.Settings.minusScorePerMiss);
        OnScoreChanged?.Invoke(currentScore);
        OnComboChanged?.Invoke(currentCombo);
    }

    private void HandleCut(NoteMover mover)
    {
        float multiplier = Mathf.Clamp((currentCombo / Mathf.Max(1, NOIZManager.Settings.scorePerCombo)) + 1, 1, NOIZManager.Settings.maxComboMultiplier);
        currentCombo += NOIZManager.Settings.scorePerCombo;

        int gained = Mathf.RoundToInt(NOIZManager.Settings.scorePerNote * multiplier);
        currentScore += gained;
        OnScoreChanged?.Invoke(currentScore);
        OnComboChanged?.Invoke(currentCombo);
    }

    private void HandleWrongCut()
    {
        currentCombo = 0;
        OnComboChanged?.Invoke(currentCombo);
    }

}
