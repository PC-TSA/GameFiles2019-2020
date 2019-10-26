using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class SelectorController : MonoBehaviour
{
    public int laneNumber; //0 = left lane, 1 = mid lane, 2 = right lane; defines it's keycode
    public KeyCode key;
    public KeyCode manualGenKey; //Used for placing notes in manual mode

    public List<GameObject> selectableNotes = new List<GameObject>();

    public bool shouldKillNotes = true; //If false, notes are hit by this selector wont die; For map making/testing purposes 
    public bool shouldKillMissedNotes = true; //If false, notes that go past this selector wont die; For map making/testing purposes 

    public RhythmController rhythmController;
    public ScrollerController scrollerController;

    private void Start()
    {
        key = rhythmController.laneKeycodes[laneNumber]; //Gets this selector's keycode from it's lane index & the keycode list in RhythmController.cs
        manualGenKey = rhythmController.manualGenKeycodes[laneNumber];
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag == "Note")
            selectableNotes.Add(collision.gameObject);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.tag == "Note")
        {
            selectableNotes.Remove(collision.gameObject);
            collision.GetComponent<NoteController>().StartDeathFade();
            rhythmController.UpdateNotesMissed(1);
        }
    }

    private void Update()
    {
        if(Input.GetKeyDown(key))
        {
            if (selectableNotes.Count != 0)
                foreach (GameObject note in selectableNotes)
                {
                    if(shouldKillNotes)
                        note.GetComponent<NoteController>().Hit();
                    else
                        note.GetComponent<NoteController>().HitNoKill();

                    FindObjectOfType<RhythmController>().UpdateNotesHit(1);
                }
            else
                FindObjectOfType<RhythmController>().UpdateMissclicks(1);
        }

        if (Input.GetKeyDown(manualGenKey) && rhythmController.editMode == 1) //If in manual gen edit mode
            scrollerController.SpawnNote(laneNumber, true);
    }

    public void IsTestingToggle()
    {
        shouldKillNotes = !shouldKillNotes;
    }
}
