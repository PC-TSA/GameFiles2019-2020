using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class SelectorController : MonoBehaviour
{
    public KeyCode key;

    public List<GameObject> selectableNotes = new List<GameObject>();

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag == "Note")
            selectableNotes.Add(collision.gameObject);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.tag == "Note")
            selectableNotes.Remove(collision.gameObject);
    }

    private void Update()
    {
        if(Input.GetKeyDown(key))
        {
            if (selectableNotes.Count != 0)
                foreach (GameObject note in selectableNotes)
                {
                    note.GetComponent<NoteController>().Hit();
                    FindObjectOfType<RhythmController>().UpdateNotesHit(1);
                }
            else
                FindObjectOfType<RhythmController>().UpdateMissclicks(1);
        }
    }
}
