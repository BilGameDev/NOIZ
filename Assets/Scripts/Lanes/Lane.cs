using System.Collections.Generic;
using UnityEngine;

public class Lane : MonoBehaviour
{
    private readonly List<NoteMover> notes = new();
    
    public void RegisterNote(NoteMover note)
    {
        if (note != null && !notes.Contains(note))
            notes.Add(note);
    }

    public void UnregisterNote(NoteMover note)
    {
        notes.Remove(note);
    }

    public NoteMover GetFrontMostCuttableNote()
    {
        NoteMover front = null;
        float lowestZ = float.MaxValue;

        for (int i = 0; i < notes.Count; i++)
        {
            NoteMover note = notes[i];
            if (note == null || !note.CanBeCut())
                continue;

            if (!note.IsWithinCuttableZRange())
                continue;

            float z = note.transform.position.z;
            
            if (z < lowestZ)
            {
                lowestZ = z;
                front = note;
            }
        }

        return front;
    }

    public List<NoteMover> GetAllCuttableNotesInOrder()
    {
        List<NoteMover> cuttableNotes = new();
        
        for (int i = 0; i < notes.Count; i++)
        {
            NoteMover note = notes[i];
            if (note != null && note.CanBeCut() && note.IsWithinCuttableZRange())
            {
                cuttableNotes.Add(note);
            }
        }
        
        cuttableNotes.Sort((a, b) => a.transform.position.z.CompareTo(b.transform.position.z));
        
        return cuttableNotes;
    }
}