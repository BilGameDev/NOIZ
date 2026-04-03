using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class TrackItem : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI trackInfoText; // Combined text for track number, title, and album
    public Button swapButton;
    public Image albumArtImage; // New: Album art image

    
    [Header("Text Format")]
    public string trackFormat = "<b>{number}</b>  {title}";
    public string albumFormat = "\n<size=18><color=#888888>{album}</color></size>";
    
    private DeezerTrack track;
    private int trackIndex;
    private System.Action<DeezerTrack, int> onSwapCallback;
    private Coroutine imageLoadCoroutine;
    
    public void Setup(DeezerTrack track, int index, 
        System.Action<DeezerTrack, int> onSwap)
    {
        this.track = track;
        this.trackIndex = index;
        this.onSwapCallback = onSwap;
        
        UpdateUI();
        LoadAlbumArt();
        
        // Setup swap button only (no select button)
        if (swapButton != null)
        {
            swapButton.onClick.RemoveAllListeners();
            swapButton.onClick.AddListener(OnSwapClicked);
        }
        
        // Optional: Make the whole item clickable for swap
        Button itemButton = GetComponent<Button>();
        if (itemButton != null)
        {
            itemButton.onClick.RemoveAllListeners();
            itemButton.onClick.AddListener(OnSwapClicked);
        }
    }
    
    public void UpdateTrack(DeezerTrack newTrack, int index)
    {
        this.track = newTrack;
        this.trackIndex = index;
        UpdateUI();
        LoadAlbumArt();
    }
    
    private void UpdateUI()
    {
        if (trackInfoText != null)
        {
            // Format the combined track info
            string trackNumber = $"{trackIndex + 1}";
            string trackTitle = track.title;
            string albumName = track.album?.title ?? "Unknown Album";
            
            // Build the formatted string
            string formattedText = trackFormat
                .Replace("{number}", trackNumber)
                .Replace("{title}", trackTitle);
            
            formattedText += albumFormat.Replace("{album}", albumName);
            
            trackInfoText.text = formattedText;
        }
    }
    
    private void LoadAlbumArt()
    {
        if (albumArtImage == null) return;
        
        string albumArtUrl = track.album?.cover_medium ?? "";
        
        if (!string.IsNullOrEmpty(albumArtUrl))
        {
            if (imageLoadCoroutine != null)
                StopCoroutine(imageLoadCoroutine);
            
            imageLoadCoroutine = StartCoroutine(LoadImage(albumArtUrl, albumArtImage));
        }
        else
        {
            // Set default/placeholder image
            albumArtImage.color = new Color(0.3f, 0.3f, 0.3f);
        }
    }
    
    IEnumerator LoadImage(string url, Image targetImage)
    {
        using (UnityEngine.Networking.UnityWebRequest request = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(url))
        {
            yield return request.SendWebRequest();
            
            if (this == null || gameObject == null) yield break;
            
            if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                Texture2D texture = UnityEngine.Networking.DownloadHandlerTexture.GetContent(request);
                if (texture != null && targetImage != null)
                {
                    Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                    targetImage.sprite = sprite;
                    targetImage.color = Color.white;
                }
            }
            else
            {
                Debug.LogWarning($"Failed to load album art: {request.error}");
                targetImage.color = new Color(0.3f, 0.3f, 0.3f);
            }
        }
    }
    
    public void ShowLoading(bool show)
    {
        // Disable swap button while loading
        if (swapButton != null)
            swapButton.interactable = !show;
    }
    
    private void OnSwapClicked()
    {
        onSwapCallback?.Invoke(track, trackIndex);
    }
    
    void OnDestroy()
    {
        if (swapButton != null)
            swapButton.onClick.RemoveListener(OnSwapClicked);
        
        if (imageLoadCoroutine != null)
            StopCoroutine(imageLoadCoroutine);
    }
}