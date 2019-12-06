using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using SFB;

//Main variable class
public class RhythmController : MonoBehaviour
{
    public AudioSource audioSource;

    public List<AudioClip> songs;
    public List<int> songBPMs;

    public int laneCount;
    public int noteCount;
    public int sliderCount;
    public int spaceCount;

    public GameObject noteCountTxt;
    public GameObject sliderCountTxt;
    public GameObject spaceCountTxt;

    public List<GameObject> lanes;
    public GameObject laneCountPicker;

    public GameObject spaceSelector;
    public float backgroundWidth;
    public float dividerWidth;

    public GameObject songPickerDropdown;
    public int selectedSongID; //Index of the selected song in the songs list

    public ScrollerController scrollerController;
    public RhythmSliderController sliderController;

    public GameObject EditModeButton;
    public GameObject StartPauseButton;

    public List<KeyCode> laneKeycodes = new List<KeyCode>(); //index 0 = left lane, 1 = middle lane, 2 == right lane; values gotten by SelectorComponent.cs
    public List<KeyCode> manualGenKeycodes = new List<KeyCode>(); //^; alternate keys used for placing notes in manual gen mode
    public KeyCode placeSliderKeycode; //This key + a key from manualKeycode will place a slider of that type in manual gen mode
    public KeyCode placeSpaceKeycode; //This key will place a space note in manual gen mode

    public GameObject waveformObj;

    public bool isPlaying;
    public int editMode; //0 = auto generated notes by bpm; 1 = when developer presses arrow keys

    public Recording currentRecording; //Holds the track creations that can be saved

    public List<GameObject> noteGameObjects;
    public List<GameObject> sliderGameObjects;
    public List<GameObject> spaceGameObjects;

    private void Start()
    {
        Object[] temp = Resources.LoadAll("Songs", typeof(AudioClip)); //Read all audioclips in the Resources/Songs folder and add them to the 'Songs' list
        foreach (Object o in temp)
            songs.Add((AudioClip) o);

        for(int i = 0; i < songs.Count; i++) //Generate song BPMs, populate song picker dropdown
        {
            int bpm = UniBpmAnalyzer.AnalyzeBpm(songs[i]);
            songBPMs.Add(bpm / 2);
            songPickerDropdown.GetComponent<TMP_Dropdown>().options.Add(new TMP_Dropdown.OptionData() { text = songs[i].name + " | " + songBPMs[i] });
        }

        /* OLD SONG PICKER POPULATOR:
        //Populates song picker dropdown with songs from the songs list
        for (int i = 0; i < songs.Count; i++)
            songPickerDropdown.GetComponent<TMP_Dropdown>().options.Add(new TMP_Dropdown.OptionData() { text = songs[i].name + " | " + songBPMs[i] });*/
     
        currentRecording = new Recording();
    }

    private void Update()
    {
        if (isPlaying) //Update Waveform \/
            waveformObj.transform.GetChild(0).transform.localPosition = new Vector3(((audioSource.time * waveformObj.GetComponent<RectTransform>().sizeDelta.x) / audioSource.clip.length) - (waveformObj.GetComponent<RectTransform>().sizeDelta.x / 2), 0, 0);
    }

    public void UpdateNoteCount(int i)
    {
        noteCount += i;
        noteCountTxt.GetComponent<TextMeshProUGUI>().text = "Note Count: " + noteCount;
    }

    public void UpdateSliderCount(int i)
    {
        sliderCount += i;
        sliderCountTxt.GetComponent<TextMeshProUGUI>().text = "Slider Count: " + sliderCount;
    }

    public void UpdateSpaceCount(int i)
    {
        spaceCount += i;
        spaceCountTxt.GetComponent<TextMeshProUGUI>().text = "Space Count: " + spaceCount;
    }

    void StartLevel()
    {
        audioSource.Play();
        sliderController.SetSlider();
        StartPauseButton.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = "Pause";
    }

    void PauseLevel()
    {
        audioSource.Pause();
        StartPauseButton.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = "Start";
    }

    void StartPause()
    {
        if (audioSource.clip != null)
        {
            isPlaying = !isPlaying;
            if (isPlaying)
                StartLevel();
            else
                PauseLevel();
        }
    }

    //Auto generates notes that scroll down in random lanes at the bpm of the song
    void AutoGenEditMode()
    {
        editMode = 0;
        EditModeButton.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = "Auto Gen";
        scrollerController.DrainTimer();
    }

    //Manually generate notes by pressing the manual key codes from the manualKeyCodes list (likely 'A' = 'left arrow', 'W' = 'up arrow', 'D' = 'right arrow')
    void ManualEditMode()
    {
        editMode = 1;
        EditModeButton.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = "Manual Gen";
    }

    public void EditModeSelect()
    {
        if (editMode == 0)
            ManualEditMode();
        else if (editMode == 1)
            AutoGenEditMode();
    }

    void CreateWaveform()
    {
        Texture2D tex = GetComponent<WaveformVisualizer>().PaintWaveformSpectrum(audioSource.clip, 1, (int)waveformObj.GetComponent<RectTransform>().sizeDelta.x, (int)waveformObj.GetComponent<RectTransform>().sizeDelta.y, Color.yellow);
        waveformObj.GetComponent<Image>().sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        waveformObj.GetComponent<Image>().color = Color.white;
    }

    public void SelectSong()
    {
        selectedSongID = songPickerDropdown.GetComponent<TMP_Dropdown>().value;
        scrollerController.bpm = songBPMs[selectedSongID];
        audioSource.clip = songs[selectedSongID];

        //Update Waveform
        CreateWaveform();

        //Update recording song name
        currentRecording.clipName = songs[selectedSongID].name;
    }

    public void SaveRecording() //Serializes recording to xml file
    {
        //Making sure all vals are set
        currentRecording.clipName = songs[selectedSongID].name;
        currentRecording.scrollSpeed = scrollerController.scrollSpeed;

        var serializer = new XmlSerializer(typeof(Recording));
        string path = null;

        path = StandaloneFileBrowser.SaveFilePanel("Save File", "", "", "");

        if (path != null && path != "")
        {
            if (!path.Contains(".xml"))
                path += ".xml";
            var stream = new FileStream(path, FileMode.Create);
            serializer.Serialize(stream, currentRecording);
            stream.Close();
        }
    }

    public void LoadRecording() //Deserializes chosen xml file and sets it as current recording
    {
        var serializer = new XmlSerializer(typeof(Recording));
        string path = null;

        path = StandaloneFileBrowser.OpenFilePanel("Open File", "", "", false)[0];

        if (path != null && path != "")
        {
            var stream = new FileStream(path, FileMode.Open);
            currentRecording = serializer.Deserialize(stream) as Recording;
            stream.Close();

            ClearRhythmInScene();

            //Update currently selected song
            foreach (AudioClip clip in songs)
                if (clip.name == currentRecording.clipName)
                    selectedSongID = songs.IndexOf(clip);

            scrollerController.bpm = songBPMs[selectedSongID];
            audioSource.clip = songs[selectedSongID];
            audioSource.time = 0;

            //Update scroll speed
            scrollerController.scrollSpeed = currentRecording.scrollSpeed;

            //Reset scroller to start
            scrollerController.transform.localPosition = scrollerController.originalPos;

            //Update waveform
            CreateWaveform();

            //Load lane count
            LoadLaneCount();

            //Reset note/slider counts
            noteCount = 0;
            sliderCount = 0;

            //Generate notes
            foreach (Note n in currentRecording.notes)
            {
                scrollerController.DeserializeNote(n);
                noteCount += 1;
            }
            UpdateNoteCount(0);

            //Generate sliders
            foreach (SliderObj s in currentRecording.sliders)
            {
                scrollerController.DeserializeSlider(s);
                sliderCount += 1;
            }
            UpdateSliderCount(0);

            //Generate spaces
            foreach (SpaceObj s in currentRecording.spaces)
            {
                scrollerController.DeserializeSpace(s);
                spaceCount += 1;
            }
            UpdateSpaceCount(0);

            //Set start button to 'Start'
            isPlaying = false;
            PauseLevel();

            songPickerDropdown.GetComponent<TMP_Dropdown>().value = selectedSongID;
        }
    }

    public void ClearRhythmInScene() //Clears all sliders and notes
    {
        ClearNotes();
        ClearSliders();
    }

    void ClearNotes()
    {
        //Clear existing notes
        foreach (GameObject n in noteGameObjects)
        {
            currentRecording.notes.Remove(n.GetComponent<NoteController>().noteCodeObject);
            Destroy(n);
        }
        noteGameObjects.Clear();

        UpdateNoteCount(-noteCount);
    }

    void ClearSliders()
    {
        //Clear existing sliders
        foreach (GameObject s in sliderGameObjects)
        {
            currentRecording.sliders.Remove(s.GetComponent<SliderController>().sliderCodeObject);
            Destroy(s);
        }
        sliderGameObjects.Clear();

        UpdateSliderCount(-sliderCount);
    }

    public void SetLaneCount()
    {
        int canParse = 0;
        int.TryParse(laneCountPicker.GetComponent<TMP_InputField>().text, out canParse); //Prevents parse error
        if (canParse != 0)
        {
            int count = int.Parse(laneCountPicker.GetComponent<TMP_InputField>().text);
            if (count > 0 && count < 5)
            {
                laneCount = count;
                currentRecording.laneCount = laneCount;

                int index = 0;
                foreach (GameObject obj in lanes)
                {
                    if (index <= count - 1)
                        lanes[index].SetActive(true);
                    else
                        lanes[index].SetActive(false);
                    index++;
                }
            }

            UpdateSpaceSelector();
        }
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

        spaceSelector.transform.GetComponent<BoxCollider2D>().size = new Vector2(w, spaceSelector.transform.GetComponent<BoxCollider2D>().size.y); //Sets space selector collider width

        float x = (lanes[0].transform.localPosition.x + lanes[laneCount - 1].transform.localPosition.x) / 2;
        spaceSelector.transform.localPosition = new Vector3(x, spaceSelector.transform.localPosition.y, spaceSelector.transform.localPosition.z); //Sets space selector localPos.x
    }

    public void TestModeToggle()
    {
        foreach (SelectorController sc in FindObjectsOfType<SelectorController>())
            sc.shouldKillNotes = !sc.shouldKillNotes;
    }
    
    public void ReGenerate() //Remakes all code objects based on existing notes, sliders, and spaces; Good for transfering recordings from old versions to new versions
    {
        Debug.Log("Beginning ReGeneration...");

        //Make new recording with proper scene settings
        currentRecording = new Recording();
        currentRecording.clipName = songs[selectedSongID].name;
        currentRecording.scrollSpeed = scrollerController.scrollSpeed;
        currentRecording.laneCount = laneCount;

        Debug.Log("New Recording generated!");

        //Regenerate notes
        foreach (GameObject note in noteGameObjects)
        {
            Note n = new Note(note.GetComponent<NoteController>().noteCodeObject.lane, note.transform.localPosition);
            currentRecording.notes.Add(n);
            note.GetComponent<NoteController>().noteCodeObject = n;
        }

        Debug.Log("Notes generated!");

        //Regenerate sliders
        foreach (GameObject slider in sliderGameObjects)
        {
            slider.GetComponent<BoxCollider2D>().offset = new Vector2(slider.GetComponent<BoxCollider2D>().offset.x, slider.GetComponent<BoxCollider2D>().size.y / 2 - slider.transform.GetChild(1).GetComponent<RectTransform>().sizeDelta.y / 2);
            SliderObj s = new SliderObj(slider.GetComponent<SliderController>().sliderCodeObject.lane, slider.transform.localPosition);
            currentRecording.sliders.Add(s);
            slider.GetComponent<SliderController>().sliderCodeObject = s;
            slider.GetComponent<SliderController>().sliderCodeObject.height = slider.transform.GetChild(0).GetComponent<RectTransform>().sizeDelta.y;
            slider.GetComponent<SliderController>().sliderCodeObject.colliderSizeY = slider.GetComponent<BoxCollider2D>().size.y;
            slider.GetComponent<SliderController>().sliderCodeObject.colliderCenterY = slider.GetComponent<BoxCollider2D>().offset.y;
            slider.GetComponent<SliderController>().sliderCodeObject.childY = slider.transform.GetChild(0).localPosition.y;
        }

        Debug.Log("Sliders generated!");

        //Regenerate spaces
        foreach (GameObject space in spaceGameObjects)
        {
            SpaceObj s = new SpaceObj(space.GetComponent<RectTransform>().sizeDelta.x, space.transform.localPosition);
            currentRecording.spaces.Add(s);
            space.GetComponent<SpaceController>().spaceCodeObject = s;
        }

        Debug.Log("Spaces generated!");
        Debug.Log("ReGeneration Complete");
    }
}