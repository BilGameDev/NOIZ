using System.Collections.Generic;
using UnityEngine;

public class ParticlePool : MonoBehaviour
{
    public GameObject particlePrefab;
    public int poolSize = 10;

    private Queue<GameObject> pool = new();

    void Awake()
    {
        NOIZEventHandler.OnHitNote += HandleHitNote;
    }

    void OnDestroy()
    {
        NOIZEventHandler.OnHitNote -= HandleHitNote;
    }

    private void HandleHitNote(NoteMover mover)
    {
        if (mover != null)
        {
            PlayEffect(mover.transform.position);
        }
    }

    void Start()
    {
        for (int i = 0; i < poolSize; i++)
        {
            GameObject go = Instantiate(particlePrefab, transform);
            go.SetActive(false);
            pool.Enqueue(go);
        }
    }

    public void PlayEffect(Vector3 position)
    {
        GameObject effect = pool.Count > 0 ? pool.Dequeue() : Instantiate(particlePrefab);

        effect.transform.position = position;
        effect.SetActive(true);

        ParticleSystem ps = effect.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            ps.Play();
            StartCoroutine(ReturnAfterDuration(effect, ps.main.duration));
        }
        else
        {
            // fallback in case of no particle system
            StartCoroutine(ReturnAfterDuration(effect, 2f));
        }
    }

    private System.Collections.IEnumerator ReturnAfterDuration(GameObject effect, float duration)
    {
        yield return new WaitForSeconds(duration);
        effect.SetActive(false);
        pool.Enqueue(effect);
    }
}
