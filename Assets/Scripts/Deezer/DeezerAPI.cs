using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Linq;

public class DeezerAPI : MonoBehaviour
{
    public static DeezerAPI Instance;

    private const string BASE_URL = "https://api.deezer.com/";

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    // Search for artists by name
    public IEnumerator SearchArtist(string query, System.Action<List<ArtistData>> onComplete, System.Action<string> onError = null)
    {
        string url = $"{BASE_URL}search/artist?q={UnityWebRequest.EscapeURL(query)}&limit=50";

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string json = request.downloadHandler.text;
                ArtistSearchResponse response = JsonUtility.FromJson<ArtistSearchResponse>(json);

                if (response != null && response.data != null)
                {
                    onComplete?.Invoke(response.data.ToList());
                }
                else
                {
                    onComplete?.Invoke(new List<ArtistData>());
                }
            }
            else
            {
                Debug.LogError($"Deezer API Error: {request.error}");
                onError?.Invoke(request.error);
                onComplete?.Invoke(new List<ArtistData>());
            }
        }
    }

    // Get artist details by ID
    public IEnumerator GetArtistDetails(long artistId, System.Action<ArtistDetails> onComplete, System.Action<string> onError = null)
    {
        string url = $"{BASE_URL}artist/{artistId}";

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string json = request.downloadHandler.text;
                ArtistDetails details = JsonUtility.FromJson<ArtistDetails>(json);
                onComplete?.Invoke(details);
            }
            else
            {
                Debug.LogError($"Failed to get artist details: {request.error}");
                onError?.Invoke(request.error);
            }
        }
    }

    // Get artist's top tracks
    public IEnumerator GetArtistTopTracks(long artistId, System.Action<List<DeezerTrack>> onComplete, System.Action<string> onError = null)
    {
        string url = $"{BASE_URL}artist/{artistId}/top?limit=10";

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string json = request.downloadHandler.text;
                TrackSearchResponse response = JsonUtility.FromJson<TrackSearchResponse>(json);

                if (response != null && response.data != null)
                {
                    onComplete?.Invoke(response.data.ToList());
                }
                else
                {
                    onComplete?.Invoke(new List<DeezerTrack>());
                }
            }
            else
            {
                Debug.LogError($"Failed to get top tracks: {request.error}");
                onError?.Invoke(request.error);
            }
        }
    }
}

// Data Models
[System.Serializable]
public class ArtistSearchResponse
{
    public ArtistData[] data;
    public int total;
}

[System.Serializable]
public class ArtistData
{
    public int rank;
    public long id;
    public string name;
    public string picture_medium;
    public string picture_big;
    public int nb_fan;
    public string tracklist;
}

[System.Serializable]
public class ArtistDetails
{
    public long id;
    public string name;
    public string picture_big;
    public string picture_xl;
    public int nb_fan;
    public int nb_album;
    public string tracklist;
}

[System.Serializable]
public class TrackSearchResponse
{
    public DeezerTrack[] data;
}

[System.Serializable]
public class DeezerTrack
{
    public long id;
    public string title;
    public int duration;
    public ArtistData artist;
    public AlbumData album;
    public string preview;
}

[System.Serializable]
public class DeezerChartResponse
{
    public DeezerTrack[] data;
}

[System.Serializable]
public class AlbumData
{
    public long id;
    public string title;
    public string cover_medium;
}