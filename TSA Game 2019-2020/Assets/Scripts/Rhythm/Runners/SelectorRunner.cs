using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelectorRunner : MonoBehaviour
{
    public int laneNumber; //0 = left lane, 1 = mid lane, 2 = right lane; defines it's keycode
    public KeyCode key;

    public List<GameObject> selectableNotes = new List<GameObject>();

    public RhythmRunner rhythmRunner;

    private void Start()
    {
        key = rhythmRunner.laneKeycodes[laneNumber]; //Gets this selector's keycode from it's lane index & the keycode list in RhythmController.cs
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag == "Note")
            selectableNotes.Add(collision.gameObject);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.tag == "Note" && !collision.GetComponent<NoteController>().hasBeenHit)
        {
            selectableNotes.Remove(collision.gameObject);
            collision.GetComponent<NoteController>().StartDeathFade();
            rhythmRunner.UpdateNotesMissed(1);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(key))
        {
            if (selectableNotes.Count != 0)
            {
                //Removes oldest note in the selectable notes list
                selectableNotes[0].GetComponent<NoteController>().Hit();
                selectableNotes.RemoveAt(0);
                rhythmRunner.UpdateNotesHit(1);

                /* CODE TO KILL MULTIPLE NODES WITH SINGLE CLICK
                List<GameObject> notesToRemove = new List<GameObject>();
                foreach (GameObject note in selectableNotes)
                {
                    note.GetComponent<NoteController>().Hit();
                    notesToRemove.Add(note);
                }
                foreach (GameObject note in notesToRemove)
                    selectableNotes.Remove(note);
                notesToRemove.Clear();*/
            }
            else
                rhythmRunner.UpdateMissclicks(1);
        }
    }
}
