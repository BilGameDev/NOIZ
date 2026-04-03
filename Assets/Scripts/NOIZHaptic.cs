using Lofelt.NiceVibrations;
using UnityEngine;

public class NOIZHaptic : MonoBehaviour
{
    [Header("Haptic Feedback")]
    public bool enableHaptics = true;
    public float maxExpectedVelocity = 2000f;
    public float minHapticAmplitude = 0.2f;
    public float maxHapticAmplitude = 1.0f;
    public float minHapticFrequency = 0.3f;
    public float maxHapticFrequency = 0.9f;

    [Header("Haptic Presets")]
    public float sliceSuccessAmplitude = 1.0f;
    public float sliceSuccessFrequency = 0.8f;
    public float sliceFailAmplitude = 0.6f;
    public float sliceFailFrequency = 0.2f;
    public float wrongDirectionAmplitude = 0.5f;
    public float wrongDirectionFrequency = 0.1f;

    private bool hapticActive = false;

    void Awake()
    {
        HapticController.hapticsEnabled = true;
    }

    public void StartContinuousHaptic()
    {
        if (hapticActive) return;

        hapticActive = true;
        HapticPatterns.PlayConstant(minHapticAmplitude, minHapticFrequency, 2f);
    }

    public void UpdateContinuousHaptic(float swipeVelocity)
    {
        if (!hapticActive) return;

        float intensity = Mathf.Clamp01(swipeVelocity / maxExpectedVelocity);

        float amplitude = Mathf.Lerp(minHapticAmplitude, maxHapticAmplitude, intensity);
        float frequency = Mathf.Lerp(minHapticFrequency, maxHapticFrequency, intensity);

        HapticController.clipLevel = amplitude;
        HapticController.clipFrequencyShift = frequency;
    }

    public void EndSwipeHaptic(bool success)
    {
        if (!hapticActive) return;

        hapticActive = false;
        HapticController.Stop();

        if (success)
        {
            PlaySliceSuccessHaptic();
        }
        else
        {
            PlaySliceFailHaptic();
        }
    }

    public void PlaySliceSuccessHaptic()
    {
        HapticPatterns.PlayEmphasis(sliceSuccessAmplitude, sliceSuccessFrequency);
    }

    public void PlaySliceFailHaptic()
    {
        HapticPatterns.PlayEmphasis(sliceFailAmplitude, sliceFailFrequency);
    }

    public void PlayWrongDirectionHaptic()
    {
        HapticPatterns.PlayEmphasis(wrongDirectionAmplitude, wrongDirectionFrequency);
    }

    private void OnDestroy()
    {
        if (hapticActive)
        {
            HapticController.Stop();
        }
    }
}
