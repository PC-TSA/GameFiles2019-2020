using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ScrollerController : MonoBehaviour
{
    public GameObject notePrefab;
    public GameObject sliderPrefab;
    public GameObject spacePrefab;

    public GameObject notesParent;
    public GameObject slidersParent;
    public GameObject spacesParent;

    public RhythmController rhythmController;

    public GameObject[] lanes;
    public GameObject[] selectors;
    public GameObject spaceSelector;

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
                transform.position = new Vector3(transform.localPosition.x, transform.localPosition.y - scrollSpeed, transform.localPosition.z);
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
            newNote = Instantiate(notePrefab, selectors[lane - 1].transform.position, transform.rotation, notesParent.transform);
        else
            newNote = Instantiate(notePrefab, lanes[lane].transform.position, transform.rotation, notesParent.transform);

        //Rotate note
        switch (lane)
        {
            case 1:
                newNote.transform.eulerAngles = new Vector3(0, 0, 90); //Right
                break;
            case 2:
                newNote.transform.eulerAngles = new Vector3(0, 0, 180); //Up
                break;
            case 3:
                newNote.transform.eulerAngles = new Vector3(0, 0, -90); //Left
                break;
            case 4:
                newNote.transform.eulerAngles = new Vector3(0, 0, 0); //Down
                break;
            case 5:
                break; //5th lane
        }

        rhythmController.noteGameObjects.Add(newNote);
        Note n = new Note(lane, newNote.transform.localPosition);
        rhythmController.currentRecording.notes.Add(n);
        newNote.GetComponent<NoteController>().noteCodeObject = n;
        rhythmController.UpdateNoteCount(1);
    }

    public GameObject SpawnSlider(int lane)
    {
        GameObject newSlider = null;

        newSlider = Instantiate(sliderPrefab, selectors[lane - 1].transform.position, transform.rotation, slidersParent.transform);

        rhythmController.sliderGameObjects.Add(newSlider);
        SliderObj s = new SliderObj(lane, newSlider.transform.localPosition);
        rhythmController.currentRecording.sliders.Add(s);
        newSlider.GetComponent<SliderController>().sliderCodeObject = s;
        rhythmController.UpdateSliderCount(1);

        return newSlider;
    }

    public GameObject SpawnSpace()
    {
        GameObject newSpace = null;

        newSpace = Instantiate(spacePrefab, spaceSelector.transform.position, transform.rotation, spacesParent.transform);

        float width = (rhythmController.laneCount * rhythmController.backgroundWidth) + ((rhythmController.laneCount - 1) * rhythmController.dividerWidth);
        float newX = ((rhythmController.lanes[0].transform.localPosition.x + rhythmController.lanes[rhythmController.laneCount - 1].transform.localPosition.x) / 2) - transform.localPosition.x;
        newSpace.GetComponent<RectTransform>().sizeDelta = new Vector2(width, newSpace.GetComponent<RectTransform>().sizeDelta.y);
        newSpace.GetComponent<BoxCollider2D>().size = new Vector2(width, newSpace.GetComponent<BoxCollider2D>().size.y);
        newSpace.transform.localPosition = new Vector3(newX, newSpace.transform.localPosition.y, newSpace.transform.localPosition.z);

        rhythmController.spaceGameObjects.Add(newSpace);
        SpaceObj s = new SpaceObj(width, newSpace.transform.localPosition);
        rhythmController.currentRecording.spaces.Add(s);
        newSpace.GetComponent<SpaceController>().spaceCodeObject = s;
        rhythmController.UpdateSpaceCount(1);

        return newSpace;
    }

    public void DeserializeNote(int lane, Vector3 pos)
    {
        GameObject newNote = Instantiate(notePrefab, new Vector3(0, 0, 0), transform.rotation, notesParent.transform);
        newNote.transform.localPosition = new Vector3(pos.x, pos.y, pos.z);

        //Rotate note
        switch (lane)
        {
            case 1:
                newNote.transform.eulerAngles = new Vector3(0, 0, 90); //Right
                break;
            case 2:
                newNote.transform.eulerAngles = new Vector3(0, 0, 180); //Up
                break;
            case 3:
                newNote.transform.eulerAngles = new Vector3(0, 0, -90); //Left
                break;
            case 4:
                newNote.transform.eulerAngles = new Vector3(0, 0, 0); //Down
                break;
            case 5:
                break; //5th lane
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

    public void DeserializeSpace(float width, Vector3 pos)
    {
        GameObject newSpace = Instantiate(spacePrefab, new Vector3(0, 0, 0), transform.rotation, spacesParent.transform);
        newSpace.transform.localPosition = new Vector3(pos.x, pos.y, pos.z);
        newSpace.GetComponent<RectTransform>().sizeDelta = new Vector2(width, newSpace.GetComponent<RectTransform>().sizeDelta.y);
        newSpace.GetComponent<BoxCollider2D>().size = new Vector2(width, newSpace.GetComponent<BoxCollider2D>().size.y);
        rhythmController.spaceGameObjects.Add(newSpace);
    }

    public void ChangeScrollSpeed()
    {
        scrollSpeed = float.Parse(speedPickerInputField.GetComponent<TMP_InputField>().text);
    }
}
