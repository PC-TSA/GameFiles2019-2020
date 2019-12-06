using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SelectorController : MonoBehaviour
{
    public int laneNumber; //0 = left lane, 1 = mid lane, 2 = right lane; defines it's keycode
    public KeyCode key;
    public KeyCode manualGenKey; //Used for placing notes in manual mode
    public KeyCode sliderGenKey; //Used with the manualGenKey above for placing sliders in manual gen mode

    public bool isHoldingSliderKeycode;
    public GameObject spawnedSlider;
    public float sliderOriginalColliderY;
    public float sliderHeightChange; //How much the height (not scale) is increased by each FixedUpdate call on the spawned slider

    public List<GameObject> selectableNotes = new List<GameObject>();

    public bool shouldKillNotes; //If false, notes are hit by this selector wont die; For map making/testing purposes 

    public RhythmController rhythmController;
    public ScrollerController scrollerController;

    public Sprite normalSprite;
    public Sprite pressSprite;

    private void Start()
    {
        //KEY AND MANUAL GEN KEY ARE HARD SET IN INSPECTOR FOR EACH LANE IN NEW MULTI-LANE SYSTEM
        //key = rhythmController.laneKeycodes[laneNumber]; //Gets this selector's keycode from it's lane index & the keycode list in RhythmController.cs
        //manualGenKey = rhythmController.manualGenKeycodes[laneNumber];
        sliderGenKey = rhythmController.placeSliderKeycode;
    }

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

    private void FixedUpdate()
    {
        if (spawnedSlider != null && isHoldingSliderKeycode)
        {
            Transform spawnedSliderChild = spawnedSlider.transform.GetChild(0);
            spawnedSliderChild.GetComponent<RectTransform>().sizeDelta = new Vector2(spawnedSliderChild.GetComponent<RectTransform>().sizeDelta.x, spawnedSliderChild.GetComponent<RectTransform>().sizeDelta.y + sliderHeightChange);
            spawnedSliderChild.localPosition = new Vector3(spawnedSliderChild.localPosition.x, spawnedSliderChild.localPosition.y + sliderHeightChange / 2, spawnedSliderChild.localPosition.z);
            spawnedSlider.GetComponent<BoxCollider2D>().size = new Vector2(spawnedSlider.GetComponent<BoxCollider2D>().size.x, spawnedSliderChild.GetComponent<RectTransform>().sizeDelta.y);
            //If line below used instead of line above, slider collider includes small tip of slider, encompasing the entire note
            //spawnedSlider.GetComponent<BoxCollider2D>().size = new Vector2(spawnedSlider.GetComponent<BoxCollider2D>().size.x, sliderOriginalColliderY / 2 + spawnedSliderChild.GetComponent<RectTransform>().sizeDelta.y);
            spawnedSlider.GetComponent<BoxCollider2D>().offset = new Vector2(spawnedSlider.GetComponent<BoxCollider2D>().offset.x, spawnedSlider.GetComponent<BoxCollider2D>().size.y / 2 - sliderOriginalColliderY / 2);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(sliderGenKey))
            isHoldingSliderKeycode = true;
        if (Input.GetKeyUp(sliderGenKey) || Input.GetKeyUp(key))
        {
            isHoldingSliderKeycode = false;
            if(spawnedSlider != null)
            {
                spawnedSlider.GetComponent<SliderController>().sliderCodeObject.height = spawnedSlider.transform.GetChild(0).GetComponent<RectTransform>().sizeDelta.y;
                spawnedSlider.GetComponent<SliderController>().sliderCodeObject.colliderSizeY = spawnedSlider.GetComponent<BoxCollider2D>().size.y;
                spawnedSlider.GetComponent<SliderController>().sliderCodeObject.colliderCenterY = spawnedSlider.GetComponent<BoxCollider2D>().offset.y;
                spawnedSlider.GetComponent<SliderController>().sliderCodeObject.childY = spawnedSlider.transform.GetChild(0).localPosition.y;
                spawnedSlider.GetComponent<SliderController>().sliderCodeObject.pos = spawnedSlider.transform.localPosition;
                spawnedSlider = null;
            }
        }

        if (Input.GetKeyDown(key))
        {
            GetComponent<Image>().sprite = pressSprite;

            if (selectableNotes.Count != 0)
            {
                if (shouldKillNotes)
                    selectableNotes[0].GetComponent<NoteController>().Hit();
                else
                    selectableNotes[0].GetComponent<NoteController>().HitNoKill();
                selectableNotes.RemoveAt(0);

                /* CODE TO DELETE MULTIPLE NODES AT ONCE
                foreach (GameObject note in selectableNotes)
                {
                    if (shouldKillNotes)
                        note.GetComponent<NoteController>().Hit();
                    else
                        note.GetComponent<NoteController>().HitNoKill();

                    FindObjectOfType<RhythmController>().UpdateNotesHit(1);
                }
            else
                FindObjectOfType<RhythmController>().UpdateMissclicks(1);*/
            }
        }

        if (Input.GetKeyUp(key))
            GetComponent<Image>().sprite = normalSprite;

        if (Input.GetKeyDown(manualGenKey)) //If in manual gen edit mode
        {
            if (rhythmController.editMode == 1)
            {
                if (isHoldingSliderKeycode)
                {
                    sliderHeightChange = scrollerController.scrollSpeed;
                    spawnedSlider = scrollerController.SpawnSlider(laneNumber);
                    sliderOriginalColliderY = spawnedSlider.GetComponent<BoxCollider2D>().size.y;
                }
                else
                    scrollerController.SpawnNote(laneNumber);
            }
        }
    }
}
