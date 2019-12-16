using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ScrollerController : MonoBehaviour
{
    public List<GameObject> arrowPrefabs;
    public List<GameObject> sliderPrefabs;
    public GameObject spacePrefab;

    public GameObject notesParent;
    public GameObject slidersParent;
    public GameObject spacesParent;

    public RhythmController rhythmController;

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
                    SpawnNote(Random.Range(0, rhythmController.laneCount));
                    timer -= (delay / bpm);
                }
            }

            lastTime = audioSource.time;

            //Scroller
            if(!slideScrollOverride)
                transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y - scrollSpeed, transform.localPosition.z);
        }
    }
     
    public void DrainTimer() //Used to bring the timer back to near 0 (but keep it's beat placement) when re-enabling auto gen mode
    {
        while(timer - (delay / bpm) > 0)
            timer -= (delay / bpm);
    }

    public void SpawnNote(int lane)
    {
        GameObject newNote = null;

        newNote = Instantiate(arrowPrefabs[lane], selectors[lane].transform.position, transform.rotation, notesParent.transform);

        rhythmController.noteGameObjects.Add(newNote);
        Note n = new Note(lane, newNote.transform.localPosition);
        rhythmController.currentRecording.notes.Add(n);
        newNote.GetComponent<NoteController>().noteCodeObject = n;
        rhythmController.UpdateNoteCount(1);
    }

    public GameObject SpawnSlider(int lane)
    {
        GameObject newSlider = null;

        newSlider = Instantiate(sliderPrefabs[lane], selectors[lane].transform.position, transform.rotation, slidersParent.transform);

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

        float width = rhythmController.spaceSelector.GetComponent<RectTransform>().sizeDelta.x;
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

    public void DeserializeNote(Note n)
    {
        GameObject newNote = Instantiate(arrowPrefabs[n.lane], new Vector3(0, 0, 0), transform.rotation, notesParent.transform);
        newNote.transform.localPosition = n.pos;
        newNote.GetComponent<NoteController>().noteCodeObject = n;
        rhythmController.noteGameObjects.Add(newNote);
    }

    public void DeserializeSlider(SliderObj s)
    {
        GameObject newSlider = Instantiate(sliderPrefabs[s.lane], new Vector3(0, 0, 0), transform.rotation, slidersParent.transform);
        newSlider.transform.localPosition = s.pos;
        Transform newSliderChild = newSlider.transform.GetChild(0);
        newSliderChild.GetComponent<RectTransform>().sizeDelta = new Vector2(newSliderChild.GetComponent<RectTransform>().sizeDelta.x, s.height);
        newSliderChild.localPosition = new Vector3(newSliderChild.localPosition.x, s.childY, newSliderChild.localPosition.z);
        newSlider.GetComponent<BoxCollider2D>().size = new Vector2(newSlider.GetComponent<BoxCollider2D>().size.x, s.height);
        newSlider.GetComponent<BoxCollider2D>().offset = new Vector2(newSlider.GetComponent<BoxCollider2D>().offset.x, s.colliderCenterY);
        newSlider.GetComponent<SliderController>().sliderCodeObject = s;
        rhythmController.sliderGameObjects.Add(newSlider);
    }

    public void DeserializeSpace(SpaceObj s)
    {
        GameObject newSpace = Instantiate(spacePrefab, new Vector3(0, 0, 0), transform.rotation, spacesParent.transform);
        newSpace.transform.localPosition = s.pos;
        newSpace.GetComponent<RectTransform>().sizeDelta = new Vector2(s.width, newSpace.GetComponent<RectTransform>().sizeDelta.y);
        newSpace.GetComponent<BoxCollider2D>().size = new Vector2(s.width, newSpace.GetComponent<BoxCollider2D>().size.y);
        newSpace.GetComponent<SpaceController>().spaceCodeObject = s;
        rhythmController.spaceGameObjects.Add(newSpace);
    }

    public void ChangeScrollSpeed()
    {
        string temp = speedPickerInputField.GetComponent<TMP_InputField>().text;
        if(temp.Length > 0) //If is not null
            scrollSpeed = float.Parse(temp);
    }
}
