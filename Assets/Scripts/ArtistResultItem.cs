using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using DG.Tweening;

public class ArtistResultItem : MonoBehaviour
{
    [Header("UI Elements")]
    public Image artistImage;
    public TextMeshProUGUI artistNameText;
    public Button selectButton;

    [Header("Entrance Animation")]
    public float animDuration = 0.35f;
    public float animDelay = 0f;
    public Ease animEase = Ease.OutBack;

    private ArtistData artist;
    private int index;
    private System.Action<ArtistData> onSelectCallback;
    private Coroutine imageLoadCoroutine;
    private CanvasGroup canvasGroup;
    
    public void Setup(ArtistData artist, int index, System.Action<ArtistData> onSelect)
    {
        this.artist = artist;
        this.index = index;
        this.onSelectCallback = onSelect;
    
        // Set artist name
        if (artistNameText != null)
            artistNameText.text = artist.name;
        
        // Setup button
        if (selectButton != null)
        {
            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(OnSelectClicked);
        }
        
        // Load artist image
        LoadArtistImage();

        // Auto-animate entrance
        AnimateIn();
    }

    public void AnimateIn()
    {
        // Ensure we have a CanvasGroup for fade
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        canvasGroup.alpha = 0f;

        // Start scale at 0
        transform.localScale = Vector3.zero;

        // Staggered entrance sequence
        Sequence seq = DOTween.Sequence();
        seq.SetDelay(animDelay);
        seq.Append(transform.DOScale(1f, animDuration).SetEase(animEase));
        seq.Join(canvasGroup.DOFade(1f, animDuration * 0.8f).SetEase(Ease.OutQuad));
        seq.SetTarget(gameObject);
    }

    public void KillAnimations()
    {
        DOTween.Kill(transform);
        DOTween.Kill(canvasGroup);
    }
    
    public void SetBackgroundColor(Color color)
    {
        var bgImage = GetComponent<Image>();
        if (bgImage != null)
            bgImage.color = color;
    }
    
    void LoadArtistImage()
    {
        if (artistImage == null) return;
        
        string imageUrl = !string.IsNullOrEmpty(artist.picture_medium) 
            ? artist.picture_medium 
            : artist.picture_big;
        
        if (!string.IsNullOrEmpty(imageUrl))
        {      
            if (imageLoadCoroutine != null)
                StopCoroutine(imageLoadCoroutine);
            
            imageLoadCoroutine = StartCoroutine(LoadImage(imageUrl, artistImage));
        }
        else
        {
            // Set default image
            artistImage.color = new Color(0.3f, 0.3f, 0.3f);
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
                Debug.LogWarning($"Failed to load artist image: {request.error}");
                targetImage.color = new Color(0.3f, 0.3f, 0.3f);
            }
        }
    }
    
    void OnSelectClicked()
    {
        onSelectCallback?.Invoke(artist);
    }
    
    void OnDestroy()
    {
        KillAnimations();

        if (selectButton != null)
            selectButton.onClick.RemoveListener(OnSelectClicked);
        
        if (imageLoadCoroutine != null)
            StopCoroutine(imageLoadCoroutine);
    }
}