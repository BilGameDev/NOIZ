using System.Collections.Generic;
using UnityEngine;

public class NotePool : MonoBehaviour
{
    [SerializeField] private GameObject notePrefab;
    private readonly Queue<GameObject> pool = new();

    public GameObject Get()
    {
        GameObject note = pool.Count > 0 ? pool.Dequeue() : Instantiate(notePrefab, transform);
        note.SetActive(true);
        return note;
    }

    public void Return(GameObject note)
    {
        if (note == null)
            return;

        note.SetActive(false);
        pool.Enqueue(note);
    }
}