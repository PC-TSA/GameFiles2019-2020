using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ScrollerController : MonoBehaviour
{
    public GameObject notePrefab;
    public GameObject sliderPrefab;
    public GameObject notesParent;
    public GameObject slidersParent;

    public RhythmController rhythmController;

    public GameObject[] lanes;
    public GameObject[] selectors;

    public AudioSource audioSource;

    public float scrollSpeed;

    public float bpm;
    public float delay;

    public float lastTime, deltaTime, timer;

    public Vector3 originalPos; //Is reset to this

    public GameObject speedPickerInputField;

    public bool slideScrollOverride; //If this scroller's position is being overriden by the UI slider in the rhythm maker

    private void Start()
    {
        originalPos = transform.localPosition;
    }

    private void FixedUpdate()
    { 
        //If the level has started
        if(FindObjectOfType<RhythmController>().isPlaying)
        {
            //Prevents first few note spawns from spawning multiple at once
            if (audioSource.time > 2f)
            {
                deltaTime = audioSource.time - lastTime;
                timer += deltaTime;
            }
            else
                lastTime = audioSource.time;

            if (rhythmController.editMode == 0) //If auto gen is on (instead of manual placement)
            {
                if (timer >= (delay / bpm))
                {
                    //Create the note in a random lane
                    SpawnNote(Random.Range(0, lanes.Length), false);
                    timer -= (delay / bpm);
                }
            }

            lastTime = audioSource.time;

            //Scroller
            if(!slideScrollOverride)
                transform.localPosition = new Vector3(0, transform.localPosition.y - scrollSpeed, 0);
        }
    }
     
    public void DrainTimer() //Used to bring the timer back to near 0 (but keep it's beat placement) when re-enabling auto gen mode
    {
        while(timer - (delay / bpm) > 0)
            timer -= (delay / bpm);
    }

    public void SpawnNote(int lane, bool manualDevMode)
    {
        GameObject newNote = null;

        if(manualDevMode)
            newNote = Instantiate(notePrefab, selectors[lane].transform.position, transform.rotation, notesParent.transform);
        else
            newNote = Instantiate(notePrefab, lanes[lane].transform.position, transform.rotation, notesParent.transform);

        //Rotate Arrow
        switch (lane)
        {
            case 0:
                newNote.transform.eulerAngles = new Vector3(0, 0, -90);
                break;
            case 1:
                newNote.transform.eulerAngles = new Vector3(0, 0, 180);
                break;
            case 2:
                newNote.transform.eulerAngles = new Vector3(0, 0, 90);
                break;
        }

        rhythmController.noteGameObjects.Add(newNote);
        Note n = new Note(lane, new Vector3(newNote.transform.localPosition.x, newNote.transform.localPosition.y, newNote.transform.localPosition.z));
        rhythmController.currentRecording.notes.Add(n);
        newNote.GetComponent<NoteController>().noteCodeObject = n;
    }

    public GameObject SpawnSlider(int lane)
    {
        GameObject newSlider = null;

        newSlider = Instantiate(sliderPrefab, selectors[lane].transform.position, transform.rotation, slidersParent.transform);

        rhythmController.sliderGameObjects.Add(newSlider);
        SliderObj s = new SliderObj(lane, new Vector3(newSlider.transform.localPosition.x, newSlider.transform.localPosition.y, newSlider.transform.localPosition.z));
        rhythmController.currentRecording.sliders.Add(s);
        newSlider.GetComponent<SliderController>().sliderCodeObject = s;

        return newSlider;
    }

    public void DeserializeNote(int lane, Vector3 pos)
    {
        GameObject newNote = Instantiate(notePrefab, new Vector3(0, 0, 0), transform.rotation, notesParent.transform);
        newNote.transform.localPosition = new Vector3(pos.x, pos.y, pos.z);

        //Rotate Arrow
        switch (lane)
        {
            case 0:
                newNote.transform.localEulerAngles = new Vector3(0, 0, -90);
                break;
            case 1:
                newNote.transform.localEulerAngles = new Vector3(0, 0, 180);
                break;
            case 2:
                newNote.transform.localEulerAngles = new Vector3(0, 0, 90);
                break;
        }

        rhythmController.noteGameObjects.Add(newNote);
    }

    public void DeserializeSlider(int lane, Vector3 pos, float height)
    {
        GameObject newSlider = Instantiate(sliderPrefab, new Vector3(0, 0, 0), transform.rotation, slidersParent.transform);
        newSlider.transform.localPosition = new Vector3(pos.x, pos.y, pos.z);
        newSlider.GetComponent<RectTransform>().sizeDelta = new Vector2(newSlider.GetComponent<RectTransform>().sizeDelta.x, height);
        newSlider.GetComponent<BoxCollider2D>().size = new Vector2(newSlider.GetComponent<BoxCollider2D>().size.x, height);
        rhythmController.sliderGameObjects.Add(newSlider);
    }

    public void ChangeScrollSpeed()
    {
        scrollSpeed = float.Parse(speedPickerInputField.GetComponent<TMP_InputField>().text);
    }
}
