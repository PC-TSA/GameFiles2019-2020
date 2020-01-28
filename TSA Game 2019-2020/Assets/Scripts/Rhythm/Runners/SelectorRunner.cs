using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SelectorRunner : MonoBehaviour
{
    public int laneNumber; //0 = left lane, 1 = mid lane, 2 = right lane; defines it's keycode
    public KeyCode key;

    public List<GameObject> selectableNotes = new List<GameObject>();
    public GameObject selectableSlider;
    public bool selectableSliderBeingHit;
    public float sliderAccuracyMultiplier;
    public float sliderHeightChange; //How much the height (not scale) is increased by each FixedUpdate call on the spawned slider; Set to scroll speed by rhythmRunner

    public RhythmRunner rhythmRunner;

    public ParticleSystem noteHitParticle;

    public Sprite normalSprite;
    public Sprite pressSprite;

    public List<Sprite> splashImages;

    private void Start()
    {
        //KEY HARD CODED IN INSPECTOR ON SELECTOR IN NEW MULTI-LANE SYSTEM
        //key = rhythmRunner.laneKeycodes[laneNumber]; //Gets this selector's keycode from it's lane index & the keycode list in RhythmController.cs
    }

    private void OnTriggerEnter(Collider collision)
    {
        if (collision.tag == "Note")
        {
            selectableNotes.Add(collision.gameObject);
        }
        else if (collision.tag == "SliderArrow" && selectableSlider == null)
            selectableSlider = collision.transform.parent.gameObject;
    }

    private void OnTriggerExit(Collider collision)
    {
        if (collision.gameObject != null && collision.tag == "Note" && !collision.GetComponent<NoteController>().hasBeenHit)
        {
            selectableNotes.Remove(collision.gameObject);
            collision.GetComponent<NoteController>().StartDeathFade();
            rhythmRunner.UpdateNotesMissed(1);
        }
        else if (collision.tag == "SliderArrow")
        {
            Transform parent = collision.transform.parent;
            if (parent.GetComponent<SliderController>() != null)
            {
                if (!parent.GetComponent<SliderController>().hasBeenHit)
                {
                    parent.GetComponent<SliderController>().canBeHit = false;
                    rhythmRunner.UpdateNotesMissed(1);
                    selectableSlider = null;
                }
            }

        }
        else if (collision.tag == "SliderSprite")
        {
            Transform parent = collision.transform.parent.parent;
            if (parent.GetComponent<SliderController>() != null)
            {
                if (parent.GetComponent<SliderController>().incompleteHit || !parent.GetComponent<SliderController>().hasBeenHit) //Separate from top if b/c notesMissed is called when the slider stops being hit half way instead of doing it here when it is dissapearing
                {
                    parent.GetComponent<SliderController>().StartDeathFade();
                }
                else
                {
                    rhythmRunner.UpdateNotesHit(1);
                    rhythmRunner.UpdateScore(1);
                    parent.GetComponent<SliderController>().HitDeath();
                    selectableSliderBeingHit = false;
                    noteHitParticle.Stop();
                }
                selectableSlider = null;
            }
        }
    }

    private void Update()
    {
        if(rhythmRunner.isRunning)
        {
            if (Input.GetKeyDown(key))
            {
                GetComponent<Image>().sprite = pressSprite;

                bool somethingClicked = false; //If false by the end of this if, this click counts as a misclick
                if (selectableNotes.Count != 0)
                {
                    if (selectableNotes[0] != null)
                    {
                        somethingClicked = true;
                        rhythmRunner.UpdateNotesHit(1);
                        NoteHitAccuracy(selectableNotes[0]); //Updates score based on accuracy of note hit

                        //Removes oldest note in the selectable notes list
                        selectableNotes[0].GetComponent<NoteController>().Hit();
                        selectableNotes.RemoveAt(0);
                    }
                    else
                        selectableNotes.RemoveAt(0);
                }

                if (selectableSlider != null && selectableSlider.GetComponent<SliderController>().canBeHit && !selectableSlider.GetComponent<SliderController>().hasBeenHit) //If there is a selectableSlider that can be hit and hasnt been hit yet
                {
                    selectableSlider.GetComponent<SliderController>().hasBeenHit = true;
                    somethingClicked = true;
                    noteHitParticle.Play();
                    selectableSliderBeingHit = true;
                    SliderHitAccuracy(selectableSlider.GetComponent<SliderController>().arrowSpriteChild);
                }

                if (!somethingClicked)
                    rhythmRunner.UpdateMissclicks(1);
            }

            if (Input.GetKeyUp(key))
            {
                GetComponent<Image>().sprite = normalSprite;

                //If you stop hitting a slider mid way
                if (selectableSlider != null && selectableSlider.GetComponent<SliderController>().hasBeenHit)
                {
                    noteHitParticle.Stop();
                    selectableSlider.GetComponent<SliderController>().incompleteHit = true;
                    selectableSliderBeingHit = false;
                    rhythmRunner.UpdateNotesMissed(1);
                }
            }
        }
    }

    private void FixedUpdate()
    {
        if (selectableSliderBeingHit)
        {
            rhythmRunner.UpdateScore(sliderAccuracyMultiplier);

            GameObject maskChild = selectableSlider.GetComponent<SliderController>().maskChild;
            GameObject sliderSpriteChild = selectableSlider.GetComponent<SliderController>().sliderSpriteChild;
            GameObject arrowSpriteChild = selectableSlider.GetComponent<SliderController>().arrowSpriteChild;

            //Move slider's mask parent up, counteracting scroller
            sliderSpriteChild.transform.localPosition = new Vector3(sliderSpriteChild.transform.localPosition.x, sliderSpriteChild.transform.localPosition.y - rhythmRunner.scrollSpeed, sliderSpriteChild.transform.localPosition.z);
            arrowSpriteChild.transform.position = Vector3.Lerp(arrowSpriteChild.transform.position, transform.position, 30 * Time.deltaTime);
            //maskChild.transform.localPosition = new Vector3(maskChild.transform.localPosition.x, maskChild.transform.localPosition.y + rhythmRunner.scrollSpeed, maskChild.transform.localPosition.z);
            maskChild.transform.localPosition = Vector3.Lerp(maskChild.transform.localPosition, new Vector3(arrowSpriteChild.transform.localPosition.x, arrowSpriteChild.transform.localPosition.y + (sliderSpriteChild.GetComponent<RectTransform>().sizeDelta.y / 2), arrowSpriteChild.transform.localPosition.z), 30 * Time.deltaTime);
        }
    }

    public void NoteHitAccuracy(GameObject note)
    {
        if (note != null) //Stop code error in case note has been destroyed
        {
            float hitAccuracy = ((Vector3.Distance(note.transform.position, transform.position) * 100) / transform.GetComponent<RectTransform>().sizeDelta.y) * 1000;
            if (hitAccuracy >= 40) //Hit accuracy = 0-~90
            {
                rhythmRunner.UpdateScore(0.4f); //Bad hit
                rhythmRunner.badHits++;
            }
            else if (hitAccuracy < 40 && hitAccuracy >= 15)
            {
                rhythmRunner.UpdateScore(0.6f); //Okay hit
                rhythmRunner.okayHits++;
            }
            else if (hitAccuracy < 15 && hitAccuracy >= 8)
            {
                rhythmRunner.UpdateScore(0.8f); //Good hit
                //rhythmRunner.SpawnSplashTitle("Good", Color.cyan);
                rhythmRunner.SpawnSplashImage(splashImages[0]);
                rhythmRunner.goodHits++;
            }
            else if (hitAccuracy < 8)
            {
                rhythmRunner.UpdateScore(1); //Perfect hit
                //rhythmRunner.SpawnSplashTitle("Perfect", Color.green);
                rhythmRunner.SpawnSplashImage(splashImages[1]);
                rhythmRunner.perfectHits++;
            }
            rhythmRunner.UpdateAccuracy(100 - hitAccuracy);
        }
    }

    public void SliderHitAccuracy(GameObject slider)
    {
        float hitAccuracy = ((Vector3.Distance(slider.transform.position, transform.position) * 100) / transform.GetComponent<RectTransform>().sizeDelta.y) * 1000;
        if (hitAccuracy >= 40) //Hit accuracy = 0-~90
        {
            sliderAccuracyMultiplier = 0.01f; //Bad hit
            rhythmRunner.badHits++;
        }
        else if (hitAccuracy < 40 && hitAccuracy >= 15)
        {
            sliderAccuracyMultiplier = 0.015f; //Moderate hit
            rhythmRunner.okayHits++;
        }
        else if (hitAccuracy < 15 && hitAccuracy >= 8)
        {
            sliderAccuracyMultiplier = 0.02f; //Good hit
            //rhythmRunner.SpawnSplashTitle("Good", Color.cyan);
            rhythmRunner.SpawnSplashImage(splashImages[0]);
            rhythmRunner.goodHits++;
        }
        else if (hitAccuracy < 8)
        {
            sliderAccuracyMultiplier = 0.25f; //Perfect hit
            //rhythmRunner.SpawnSplashTitle("Perfect", Color.green);
            rhythmRunner.SpawnSplashImage(splashImages[1]);
            rhythmRunner.perfectHits++;
        }
    }
}
