using Unity.VisualScripting;
using UnityEngine;

public class PopUpManager : MonoBehaviour
{
    [SerializeField] GameObject popupBlock;
    [SerializeField] Transform popupParent;
    [SerializeField] GameObject artistPopup;

    void OnEnable()
    {
        NOIZEventHandler.OnSelectArtist += OpenArtistPopup;
        NOIZEventHandler.OnClosePopup += ClosePopup;
    }

    void OnDisable()
    {
        NOIZEventHandler.OnSelectArtist -= OpenArtistPopup;
        NOIZEventHandler.OnClosePopup -= ClosePopup;
    }

    void OpenArtistPopup(ArtistData artistData)
    {
        ArtistDetailPopup popup = Instantiate(artistPopup, popupParent).GetComponent<ArtistDetailPopup>();
        popup.Setup(artistData);
        popupBlock.SetActive(true);
    }

    void ClosePopup()
    {
        popupBlock.SetActive(false);
    }
}
