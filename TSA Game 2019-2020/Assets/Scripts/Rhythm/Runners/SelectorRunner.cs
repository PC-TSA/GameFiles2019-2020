using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelectorRunner : MonoBehaviour
{
    public int laneNumber; //0 = left lane, 1 = mid lane, 2 = right lane; defines it's keycode
    public KeyCode key;

    public List<GameObject> selectableNotes = new List<GameObject>();
    public GameObject selectableSlider;

    public RhythmRunner rhythmRunner;

    private void Start()
    {
        key = rhythmRunner.laneKeycodes[laneNumber]; //Gets this selector's keycode from it's lane index & the keycode list in RhythmController.cs
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag == "Note")
            selectableNotes.Add(collision.gameObject);
        else if (collision.tag == "Slider" && selectableSlider == null)
        {
            selectableSlider = collision.gameObject;
            collision.gameObject.GetComponent<SliderController>().StartCanBeHitTimer();
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.tag == "Note" && !collision.GetComponent<NoteController>().hasBeenHit)
        {
            selectableNotes.Remove(collision.gameObject);
            collision.GetComponent<NoteController>().StartDeathFade();
            rhythmRunner.UpdateNotesMissed(1);
        }
        else if (collision.tag == "Slider")
        {
            if (!collision.GetComponent<SliderController>().hasBeenHit || collision.GetComponent<SliderController>().incompleteHit)
            {
                collision.GetComponent<SliderController>().StartDeathFade();
                rhythmRunner.UpdateNotesMissed(1);
                selectableSlider = null;
            }
            else
            {
                collision.GetComponent<SliderController>().HitDeath();
                rhythmRunner.UpdateNotesHit(1);
            }
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(key))
        {
            bool somethingClicked = false; //If false by the end of this if, this click counts as a misclick
            if (selectableNotes.Count != 0)
            {
                somethingClicked = true;

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
            
            if(selectableSlider != null && !selectableSlider.GetComponent<SliderController>().canBeHit && !selectableSlider.GetComponent<SliderController>().hasBeenHit) //If there is a selectableSlider that can be hit and hasnt been hit yet
            {
                selectableSlider.GetComponent<SliderController>().hasBeenHit = true;
                somethingClicked = true;
            }

            if (!somethingClicked)
                rhythmRunner.UpdateMissclicks(1);
        }

        //If you stop hitting a slider mid way
        if((selectableSlider != null && selectableSlider.GetComponent<SliderController>().hasBeenHit && Input.GetKeyUp(key)))
            selectableSlider.GetComponent<SliderController>().incompleteHit = true;
    }
}
