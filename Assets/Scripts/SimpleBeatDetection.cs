using UnityEngine;
using System;

public class SimpleBeatDetection : MonoBehaviour
{
	public AudioSource audioSource;

	public delegate void OnBeatHandler();
	public event OnBeatHandler OnBeat;

	[Header("Settings")]
	public int bufferSize = 1024;
	public FFTWindow FFTWindow = FFTWindow.BlackmanHarris;
	public float beatCooldown = 0.15f;
	public float smoothingFactor = 0.3f;

	private float[] samples0Channel;
	private float[] samples1Channel;
	private float[] historyBuffer;
	private float previousEnergy;
	private float lastBeatTime;

	void Start()
	{
		samples0Channel = new float[bufferSize];
		samples1Channel = new float[bufferSize];
		historyBuffer = new float[63];
	}

	void Update()
	{
		if (audioSource == null || !audioSource.isPlaying)
			return;

		float instantEnergy = GetInstantEnergy();

		instantEnergy = SmoothEnergy(instantEnergy);

		float localAverageEnergy = GetLocalAverageEnergy();

		float varianceEnergies = ComputeVariance(localAverageEnergy);

		double constantC = 1.0 + (0.5 / (1.0 + varianceEnergies * 0.01));

		float[] shiftedHistoryBuffer = ShiftArray(historyBuffer, 1);
		shiftedHistoryBuffer[0] = instantEnergy;
		OverrideElementsToAnotherArray(shiftedHistoryBuffer, historyBuffer);

		if (instantEnergy > constantC * localAverageEnergy)
		{
			if (Time.time - lastBeatTime > beatCooldown)
			{
				lastBeatTime = Time.time;
				if (OnBeat != null)
					OnBeat();
			}
		}
	}

	#region FOR_SIMPLE_ALGORITHM_USE
	public float GetInstantEnergy()
	{
		float result = 0;

		audioSource.GetSpectrumData(samples0Channel, 0, FFTWindow);
		audioSource.GetSpectrumData(samples1Channel, 1, FFTWindow);

		for (int i = 0; i < bufferSize; i++)
		{
			float weight = 1f / (1f + i * 0.1f);
			result += ((samples0Channel[i] * samples0Channel[i]) + (samples1Channel[i] * samples1Channel[i])) * weight;
		}

		return result / bufferSize;
	}

	private float SmoothEnergy(float instantEnergy)
	{
		float smoothed = instantEnergy * (1f - smoothingFactor) + previousEnergy * smoothingFactor;
		previousEnergy = smoothed;
		return smoothed;
	}

	private float GetLocalAverageEnergy()
	{
		float result = 0;

		for (int i = 0; i < historyBuffer.Length; i++)
		{
			result += historyBuffer[i];
		}

		return result / historyBuffer.Length;
	}

	private float ComputeVariance(float _averageEnergy)
	{
		float result = 0;

		for (int i = 0; i < historyBuffer.Length; i++)
		{
			result += (historyBuffer[i] - _averageEnergy) * (historyBuffer[i] - _averageEnergy);
		}

		return result / historyBuffer.Length;
	}
	#endregion

	#region UTILITY_USE
	private void OverrideElementsToAnotherArray(float[] _from, float[] _to)
	{
		for (int i = 0; i < _from.Length; i++)
		{
			_to[i] = _from[i];
		}
	}

	private float[] ShiftArray(float[] _array, int amount)
	{
		float[] result = new float[_array.Length];

		for (int i = 0; i < _array.Length - amount; i++)
		{
			result[i + amount] = _array[i];
		}

		return result;
	}
	#endregion
}
