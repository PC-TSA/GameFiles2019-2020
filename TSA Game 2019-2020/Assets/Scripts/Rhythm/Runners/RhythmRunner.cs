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

    public List<GameObject> lanes; //The parent objs for each lane; Are disabled/enabled in SetLaneCount when loading the recording
    public int laneCount; //How many lanes to have (1-5)

    public Recording currentRecording;

    public List<GameObject> arrowPrefabs;
    public List<GameObject> sliderPrefabs;
    public GameObject sliderMaskPrefab;
    public GameObject spacePrefab;

    public GameObject notesParent;
    public GameObject slidersParent;
    public GameObject spacesParent;

    public List<Sprite> arrowSprites; //0 = right, 1 = up, 2 = left, 3 = down

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

    public Color selectorColor;
    public Color selectorPressColor;

    public GameObject spaceSelector;
    public float backgroundWidth;
    public float dividerWidth;

    private void Start()
    {
        Object[] temp = Resources.LoadAll("Songs", typeof(AudioClip)); //Read all audioclips in the Resources/Songs folder and add them to the 'Songs' list
        foreach (Object o in temp)
            songs.Add((AudioClip)o);

        originalPos = scrollerObj.transform.localPosition;

        //Get vignette from post processing profile
        postProcessingVolume.profile.TryGetSettings(out vignette);

        if (CrossSceneController.recordingToLoad != "")
        {
            XMLRecordingName = CrossSceneController.recordingToLoad;
            CrossSceneController.recordingToLoad = "";
        }

        SetRecording(XMLRecordingName);
        StartCoroutine(DelayedStart(1));
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

        //Enable lanes used in this recording
        LoadLaneCount();

        //Generate track
        if (scrollSpeed == customSpeed || customSpeed == 0)
        {
            foreach (Note n in currentRecording.notes) //Deserialize notes
                DeserializeNote(n);
            foreach (SliderObj s in currentRecording.sliders) //Deserialize sliders
                DeserializeSlider(s);
            foreach (SpaceObj s in currentRecording.spaces) //Deserialize spaces
                DeserializeSpace(s);
        }
        else
        {
            scrollSpeed = customSpeed;
            foreach (Note n in currentRecording.notes)
                DeserializeNote(new Note(n.lane, OverrideSpeedPos(n)));
            foreach (SliderObj s in currentRecording.sliders)
                DeserializeSlider(new SliderObj(s.lane, OverrideSpeedPos(s), s.childY, s.height, s.colliderSizeY, s.colliderCenterY));
            foreach (SpaceObj s in currentRecording.spaces) //Deserialize spaces
                DeserializeSpace(new SpaceObj(s.width, OverrideSpeedPos(s)));
        }

        foreach(SelectorRunner selector in FindObjectsOfType<SelectorRunner>())
            selector.sliderHeightChange = scrollSpeed;

        audioSource.Play();
        isRunning = true;
    }

    public void DeserializeNote(Note n)
    {
        GameObject newNote = Instantiate(arrowPrefabs[n.lane], new Vector3(0, 0, 0), transform.rotation, notesParent.transform);    
        newNote.transform.localPosition = n.pos;
        newNote.transform.localRotation = transform.rotation;
    }

    public void DeserializeSlider(SliderObj s)
    {
        GameObject newSlider = Instantiate(sliderPrefabs[s.lane], new Vector3(0, 0, 0), transform.rotation, slidersParent.transform);
        Transform sliderMaskChild = newSlider.GetComponent<SliderController>().maskChild.transform;
        Transform sliderSpriteChild = newSlider.GetComponent<SliderController>().sliderSpriteChild.transform;
        Transform arrowSpriteChild = newSlider.GetComponent<SliderController>().arrowSpriteChild.transform;

        newSlider.transform.localPosition = s.pos;
        newSlider.transform.localRotation = transform.rotation;

        sliderMaskChild.GetComponent<RectTransform>().sizeDelta = new Vector2(arrowSpriteChild.GetComponent<RectTransform>().sizeDelta.x, s.height);
        sliderSpriteChild.GetComponent<RectTransform>().sizeDelta = new Vector2(sliderSpriteChild.GetComponent<RectTransform>().sizeDelta.x, s.height);

        sliderMaskChild.transform.localPosition = new Vector3(sliderMaskChild.transform.localPosition.x, s.height / 2, newSlider.transform.localPosition.z);
        sliderSpriteChild.GetComponent<BoxCollider>().size = new Vector3(sliderSpriteChild.GetComponent<BoxCollider>().size.x, s.height, sliderSpriteChild.GetComponent<BoxCollider>().size.z);
    }

    public void DeserializeSpace(SpaceObj s)
    {
        GameObject newSpace = Instantiate(spacePrefab, new Vector3(0, 0, 0), transform.rotation, spacesParent.transform);
        newSpace.transform.localPosition = s.pos;
        newSpace.transform.localRotation = transform.rotation;
        newSpace.GetComponent<RectTransform>().sizeDelta = new Vector2(s.width, newSpace.GetComponent<RectTransform>().sizeDelta.y);
        newSpace.GetComponent<BoxCollider>().size = new Vector3(s.width, newSpace.GetComponent<BoxCollider>().size.y, newSpace.GetComponent<BoxCollider>().size.z);
    }

    IEnumerator DelayedStart(int seconds)
    {
        yield return new WaitForSeconds(seconds);
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
                int freq = 9000 - (deathCount * 9000 / health); //Proportion of the deathCount / health = new frequency / 6000 (6000 = arbitrary max frequency I chose)
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
                tr.emitting = true;
                tr.colorGradient = comboTrailGradients[comboLvl - 1];
                //tr.material.SetColor("_EmissionColor", comboTrailGradients[comboLvl - 1].colorKeys[0].color); SETTING MAT COLOR IS BROKEN DUE TO IT BEING OVERRIDEN BY MATERIAL EMISSION, POSSIBLE FIX WITH A LINE LIKE THIS ONE
            }
        }
    }

    void BreakCombo()
    {
        combo = 1;
        comboLvl = 0;
        foreach (TrailRenderer tr in trailRenderers)
            tr.emitting = false;
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

    Vector3 OverrideSpeedPos(SpaceObj s)
    {
        float speedMultiplier = customSpeed / scrollSpeed;
        return new Vector3(s.pos.x, s.pos.y * speedMultiplier, s.pos.z);
    }

    void LoadLaneCount()
    {
        laneCount = currentRecording.laneCount;
        int index = 0;
        foreach (GameObject obj in lanes)
        {
            if (index <= laneCount - 1)
                lanes[index].SetActive(true);
            else
                lanes[index].SetActive(false);
            index++;
        }

        UpdateSpaceSelector();
    }

    void UpdateSpaceSelector()
    {
        float w = (laneCount * backgroundWidth) + ((laneCount - 1) * dividerWidth);
        spaceSelector.GetComponent<RectTransform>().sizeDelta = new Vector2(w, spaceSelector.GetComponent<RectTransform>().sizeDelta.y); //Sets space selector width

        spaceSelector.transform.GetComponent<BoxCollider>().size = new Vector3(w, spaceSelector.transform.GetComponent<BoxCollider>().size.y, spaceSelector.transform.GetComponent<BoxCollider>().size.z); //Sets space selector collider width

        float x = (lanes[0].transform.localPosition.x + lanes[laneCount - 1].transform.localPosition.x) / 2;
        spaceSelector.transform.localPosition = new Vector3(x, spaceSelector.transform.localPosition.y, spaceSelector.transform.localPosition.z); //Sets space selector localPos.x
    }

    public void SetRecording(string fileName)
    {
        XMLRecordingName = fileName;
        xmlRecordingAsset = Resources.Load<TextAsset>("Recordings/" + XMLRecordingName);
    }

    public void ToRhythmMaker()
    {
        CrossSceneController.GameToMaker(XMLRecordingName);
    }
}
