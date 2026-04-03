using System;
using UnityEngine;

public static class NOIZEventHandler
{
    public static event Action<ArtistData> OnSelectArtist;
    public static event Action<DeezerTrack> OnSelectTrack;
    public static event Action<AudioClip> OnTrackReady;
    public static event Action OnGameStart;
    public static event Action OnMissNote;
    public static event Action<NoteMover> OnHitNote;
    public static event Action OnWrongCut;
    public static event Action OnGameReset;
    public static event Action OnClosePopup;
    

    public static void SelectArtist(ArtistData artistData) => OnSelectArtist?.Invoke(artistData);
    public static void SelectTrack(DeezerTrack trackData) => OnSelectTrack?.Invoke(trackData);
    public static void TrackReady(AudioClip clip) => OnTrackReady?.Invoke(clip);
    public static void GameReset() => OnGameReset?.Invoke();
    public static void MissNote() => OnMissNote?.Invoke();
    public static void HitNote(NoteMover note) => OnHitNote?.Invoke(note);
    public static void WrongCut() => OnWrongCut?.Invoke();
    public static void ClosePopup() => OnClosePopup?.Invoke();
    public static void GameStart() => OnGameStart?.Invoke();


    public static void GoToMainScene() => UnityEngine.SceneManagement.SceneManager.LoadScene("MainScene");
    public static void GoToGameScene() => UnityEngine.SceneManagement.SceneManager.LoadScene("GameScene");
}
