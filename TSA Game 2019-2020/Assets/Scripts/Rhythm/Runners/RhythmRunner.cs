using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using UnityEngine.Rendering.PostProcessing;

public class RhythmRunner : MonoBehaviour
{
    public string XMLRecordingName; //This is the note track recording that will be used in this scene; Searches for an XML file with this exact name in the 'Assets/Resources/' folder
    public TextAsset xmlRecordingAsset;

    public int notesHit = 0; //How many notes hit
    public int notesMissed = 0; //How many notes missed
    public int missClicks = 0; //How many clicks that didnt hit a note
    public int combo = 1; //Current combo (ex. 10 notes without missing one / misclicking
    public int comboLvl = 0; //Level of combo (ex. 10+ combo = lvl1, 20+ combo = lvl2, 30+ combo = lvl3; Different levels = different visual effects / bonuses)
    public int deathCount = 0; //How many notes have been missed in a row. (ex. if you miss 5, but then get 1 right, your deathCount is 4. Doesnt go above 0 and works independently of combo)
    public int health = 20; //How much deathCount needs to reach to lose the game
    public float score; //Current score in a run; Every note hit += current combo, every FixedUpdate call a slider is hit + current combo * 0.01

    public GameObject notesHitTxt;
    public GameObject notesMissedTxt;
    public GameObject missClicksTxt;
    public GameObject comboTxt;
    public GameObject deathCountTxt;
    public GameObject scoreCountTxt;

    public List<AudioClip> songs;

    public GameObject scrollerObj;

    public bool isRunning; //If the rhythm portion is currently running, meaning a song is playing and notes are scrolling

    public List<KeyCode> laneKeycodes = new List<KeyCode>(); //index 0 = left lane, 1 = middle lane, 2 == right lane; values gotten by SelectorComponent.cs

    public Recording currentRecording;

    public GameObject notePrefab;
    public GameObject sliderPrefab;
    public GameObject notesParent;
    public GameObject slidersParent;
    public GameObject sliderMaskPrefab;

    public AudioSource audioSource;

    public float scrollSpeed;

    public Vector3 originalPos;

    public GameObject playerObj;
    public TrailRenderer[] trailRenderers;
    public Gradient[] comboTrailGradients; //0 = lvl 1, 1 = lvl 2, 2 = lvl 3

    public bool isAnimSpeedupRunning; //Made so AnimSpeedUpTimer coorutine cant be started multiple times simultaneously, as that would likely desync that animation 

    public PostProcessVolume postProcessingVolume;
    Vignette vignette;
    public float vignetteNewVal;

    public float customSpeed;

    private void Start()
    {
        originalPos = scrollerObj.transform.localPosition;

        //Get vignette from post processing profile
        postProcessingVolume.profile.TryGetSettings(out vignette);

        //Load xml asset
        xmlRecordingAsset = Resources.Load<TextAsset>(XMLRecordingName);

        StartCoroutine(DelayedStart());
    }

    private void Update()
    {
        //Smoothly lerps vignette to deathCount / 20 value
        vignette.intensity.value = Mathf.Lerp(vignette.intensity.value, vignetteNewVal, Time.deltaTime);
    }

    private void FixedUpdate()
    {
        //Scroller
        if (isRunning)
            scrollerObj.transform.localPosition = new Vector3(scrollerObj.transform.localPosition.x, scrollerObj.transform.localPosition.y - scrollSpeed, scrollerObj.transform.localPosition.z);

        if (Input.GetKeyDown(KeyCode.Space))
        {
            UpdateCombo(1);
            AnimSpeedUp();
        }
    }

    public void UpdateNotesHit(int i)
    {
        notesHit += i;
        notesHitTxt.GetComponent<TextMeshProUGUI>().text = "Notes Hit: " + notesHit;
        UpdateDeathCount(-i); //-i because the lower the death count the better and hitting a note is good
        UpdateCombo(i);
        AnimSpeedUp();
    }

    public void UpdateNotesMissed(int i)
    {
        notesMissed += i;
        notesMissedTxt.GetComponent<TextMeshProUGUI>().text = "Notes Missed: " + notesMissed;
        UpdateDeathCount(i);
        BreakCombo();
    }

    public void UpdateMissclicks(int i)
    {
        missClicks += i;
        missClicksTxt.GetComponent<TextMeshProUGUI>().text = "Misclicks: " + missClicks;
        UpdateDeathCount(i);
        BreakCombo();
    }

    public void UpdateScore(float multiplier)
    {
        score += combo * multiplier;
        scoreCountTxt.GetComponent<TextMeshProUGUI>().text = "Score: " + score;
    }

    public void LoadRecording() //Deserializes chosen xml file and sets it as current recording
    {
        var serializer = new XmlSerializer(typeof(Recording));
        var reader = new System.IO.StringReader(xmlRecordingAsset.text);
        currentRecording = serializer.Deserialize(reader) as Recording;
        reader.Close();

        //Load song
        foreach (AudioClip clip in songs)
            if (clip.name == currentRecording.clipName)
                audioSource.clip = clip;
        audioSource.time = 0;

        //Reset scroller
        scrollerObj.transform.localPosition = originalPos;

        //Update scroll speed
        scrollSpeed = currentRecording.scrollSpeed;

        /*Adjusts for tracks made in low speed being off slightly (OLD, LIKELY DOESNT SERVE A PURPOSE AND MIGHT MAKE THE DELAY WORSE)
        if (scrollSpeed <= 10)
        {
            float newY = (scrollSpeed * 0.66f) * scrollSpeed;
            scrollerObj.transform.localPosition = new Vector3(scrollerObj.transform.localPosition.x, newY, scrollerObj.transform.localPosition.z);
        }*/

        //Generate notes
        if (scrollSpeed == customSpeed || customSpeed == 0)
        {
            foreach (Note n in currentRecording.notes)
                DeserializeNote(n.lane, n.pos);
            foreach (SliderObj s in currentRecording.sliders)
                DeserializeSlider(s.lane, s.pos, s.height);
        }
        else
        {
            scrollSpeed = customSpeed;
            foreach (Note n in currentRecording.notes)
                DeserializeNote(n.lane, OverrideSpeedPos(n));
            foreach (SliderObj s in currentRecording.sliders)
                DeserializeSlider(s.lane, OverrideSpeedPos(s), s.height);
        }

        foreach(SelectorRunner selector in FindObjectsOfType<SelectorRunner>())
            selector.sliderHeightChange = scrollSpeed;

        audioSource.Play();
        isRunning = true;
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
    }

    public void DeserializeSlider(int lane, Vector3 pos, float height)
    {
        GameObject newSliderMask = Instantiate(sliderMaskPrefab, new Vector3(0, 0, 0), transform.rotation, slidersParent.transform);
        newSliderMask.transform.localPosition = new Vector3(pos.x, pos.y, pos.z);
        GameObject newSlider = Instantiate(sliderPrefab, new Vector3(0, 0, 0), newSliderMask.transform.rotation, newSliderMask.transform);
        newSlider.transform.localPosition = new Vector3(0, 0, 0);
        newSlider.GetComponent<RectTransform>().sizeDelta = new Vector2(newSlider.GetComponent<RectTransform>().sizeDelta.x, height);
        newSliderMask.GetComponent<RectTransform>().sizeDelta = new Vector2(newSlider.GetComponent<RectTransform>().sizeDelta.x, height);
        newSlider.GetComponent<BoxCollider2D>().size = new Vector2(newSlider.GetComponent<BoxCollider2D>().size.x, height);
    }

    IEnumerator DelayedStart()
    {
        yield return new WaitForSeconds(1);
        LoadRecording();
        FindObjectOfType<SelectorRunner>().sliderHeightChange = scrollSpeed;
    }

    void UpdateDeathCount(int i)
    {
        if (deathCount + i >= 0 && deathCount + i <= health)
        {
            deathCount += i;
            deathCountTxt.GetComponent<TextMeshProUGUI>().text = "Death Count: " + deathCount;
            if (deathCount >= health)
                Lose();

            if (deathCount == 0)
                GetComponent<AudioLowPassFilter>().enabled = false;
            else
            {
                //Update audio low pass filter, aka 'distorted/muffled effect'
                GetComponent<AudioLowPassFilter>().enabled = true;
                int freq = 6000 - (deathCount * 6000 / health); //Proportion of the deathCount / health = new frequency / 6000 (6000 = arbitrary max frequency I chose)
                if (freq < 1000)
                    freq = 1000;
                GetComponent<AudioLowPassFilter>().cutoffFrequency = freq;

                //Update vignette
                vignetteNewVal = 0.25f + (deathCount * 0.1f / health);
            }
        }
    }

    void Lose()
    {
        //Temp code
        Debug.Log("You Lost!");
    }

    void UpdateCombo(int i)
    {
        combo += i;
        comboTxt.GetComponent<TextMeshProUGUI>().text = "Combo: " + combo;
        if (combo < 10)
            comboLvl = 0;
        else
        {
            switch (combo)
            {
                case 10:
                    comboLvl = 1;
                    break;
                case 20:
                    comboLvl = 2;
                    break;
                case 30:
                    comboLvl = 3;
                    break;
            }

            //Enable trails & set their color
            foreach (TrailRenderer tr in trailRenderers)
            {
                tr.enabled = true;
                tr.colorGradient = comboTrailGradients[comboLvl - 1];
            }
        }
    }

    void BreakCombo()
    {
        combo = 1;
        comboLvl = 0;
        foreach (TrailRenderer tr in trailRenderers)
            tr.enabled = false;
        comboTxt.GetComponent<TextMeshProUGUI>().text = "Combo: " + combo;
    }

    void AnimSpeedUp() //Speeds up character animation briefly to increase impact when note is pressed
    {
        if(comboLvl >= 0 && !isAnimSpeedupRunning)
        {
            playerObj.GetComponent<Animator>().speed = 1f;
            StartCoroutine(AnimSpeedUpTimer());
        }
    }

    IEnumerator AnimSpeedUpTimer()
    {
        isAnimSpeedupRunning = true;
        playerObj.GetComponent<Animator>().speed = 2;
        yield return new WaitForSeconds(0.3f);
        playerObj.GetComponent<Animator>().speed = 0.25f;
        yield return new WaitForSeconds(0.15f);
        playerObj.GetComponent<Animator>().speed = 1f;
        isAnimSpeedupRunning = false;
    }

    Vector3 OverrideSpeedPos(Note n)
    {
        float speedMultiplier = customSpeed / scrollSpeed;
        return new Vector3(n.pos.x, n.pos.y * speedMultiplier, n.pos.z);
    }

    Vector3 OverrideSpeedPos(SliderObj s)
    {
        float speedMultiplier = customSpeed / scrollSpeed;
        return new Vector3(s.pos.x, s.pos.y * speedMultiplier, s.pos.z);
    }
}
