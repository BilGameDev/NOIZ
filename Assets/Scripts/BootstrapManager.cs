using System.Collections;
using UnityEngine;

public class BootstrapManager : MonoBehaviour
{
    void Start()
    {
        StartCoroutine(DelayedStart());
    }

    IEnumerator DelayedStart()
    {
        yield return new WaitForSeconds(2f);
        NOIZEventHandler.GoToMainScene();
    }
}
