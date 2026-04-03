using System.Collections;
using TelePresent.AudioSyncPro;
using UnityEngine;

public class BeatManager : MonoBehaviour
{
    [SerializeField] public AudioSourcePlus songSource;
    [SerializeField] private SimpleBeatDetection beatDetection;
    [SerializeField] private AudioSource beatSource;
    [SerializeField] private LaneManager laneManager;
    [SerializeField] private NotePool notePool;
    [SerializeField] private BeatPulse beatPulse;

    public static bool isSongPlaying { get; private set; }
    private float lastGlobalSpawnTime;
    

    void Awake()
    {
        NOIZEventHandler.OnTrackReady += SetupGame;
        NOIZEventHandler.OnMissNote += MissNote;
        NOIZEventHandler.OnHitNote += CutNote;
        beatDetection.OnBeat += MyCallbackEventHandler;
    }

    private void OnDestroy()
    {
        NOIZEventHandler.OnTrackReady -= SetupGame;
        NOIZEventHandler.OnMissNote -= MissNote;
        NOIZEventHandler.OnHitNote -= CutNote;
        beatDetection.OnBeat -= MyCallbackEventHandler;
    }

    public void SetupGame(AudioClip song)
    {
        beatSource.clip = song;
        songSource.audioSource.clip = song;
        lastGlobalSpawnTime = -NOIZManager.Settings.noteCooldown;
        NOIZEventHandler.GameReset();

        StartGame();
    }

    void StartGame()
    {
        beatSource.Play();
        StartCoroutine(PlayAndMonitor(songSource.audioSource, NOIZManager.Settings.noteDelay));
    }

    private IEnumerator PlayAndMonitor(AudioSource source, float delay)
    {
        source.volume = 0f;
        isSongPlaying = true;

        yield return new WaitForSeconds(delay);
        source.Play();

        yield return new WaitUntil(() => source.time > 0f);

        float timer = 0f;
        while (timer < NOIZManager.Settings.fadeDuration)
        {
            timer += Time.deltaTime;
            source.volume = Mathf.Lerp(0f, 1f, timer / NOIZManager.Settings.fadeDuration);
            yield return null;
        }

        source.volume = 1f;

        yield return new WaitUntil(() => source.isPlaying && source.time >= source.clip.length - NOIZManager.Settings.fadeDuration);

        timer = 0f;
        float startVolume = source.volume;

        while (timer < NOIZManager.Settings.fadeDuration)
        {
            timer += Time.deltaTime;
            source.volume = Mathf.Lerp(startVolume, 0f, timer / NOIZManager.Settings.fadeDuration);
            yield return null;
        }

        source.Stop();
        source.volume = 0f;
        isSongPlaying = false;
    }

    public void MyCallbackEventHandler()
    {
        if (!isSongPlaying)
            return;

        beatPulse.Pulse();

        if (Time.time - lastGlobalSpawnTime < NOIZManager.Settings.noteCooldown)
            return;

        int selectedLane = laneManager.GetNextLane();

        if (selectedLane == -1)
        {
            Debug.LogWarning("No available lanes found!");
            return;
        }

        lastGlobalSpawnTime = Time.time;
        laneManager.SetLaneLastUsedTime(selectedLane, Time.time);

        laneManager.SpawnNoteOnLane(selectedLane, notePool);
    }

    public void MissNote()
    {
        DuckAudio();
    }

    public void CutNote(NoteMover note)
    {
        RestoreVolume();
    }

    public void DuckAudio()
    {
        StartCoroutine(AudioUtils.DuckRoutine(songSource.audioSource, NOIZManager.Settings.duckVolume, NOIZManager.Settings.duckDuration, NOIZManager.Settings.duckFadeDuration));
    }

    public void RestoreVolume()
    {
        StartCoroutine(AudioUtils.RestoreRoutine(songSource, 1, NOIZManager.Settings.restoreFadeDuration));
    }
}
