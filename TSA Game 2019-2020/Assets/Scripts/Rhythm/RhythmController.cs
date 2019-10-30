using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using System.Xml;
using System.Xml.Serialization;
using System.IO;

//Main variable class
public class RhythmController : MonoBehaviour
{
    public AudioSource audioSource;

    public int notesHit;
    public int notesMissed;
    public int missClicks;

    public List<AudioClip> songs;
    public List<int> songBPMs;

    public GameObject notesHitTxt;
    public GameObject notesMissedTxt;
    public GameObject missClicksTxt;

    public GameObject songPickerDropdown;
    public int selectedSongID; //Index of the selected song in the songs list

    public ScrollerController scrollerController;
    public RhythmSliderController sliderController;

    public GameObject EditModeButton;
    public GameObject StartPauseButton;

    public List<KeyCode> laneKeycodes = new List<KeyCode>(); //index 0 = left lane, 1 = middle lane, 2 == right lane; values gotten by SelectorComponent.cs
    public List<KeyCode> manualGenKeycodes = new List<KeyCode>(); //^; alternate keys used for placing notes in manual gen mode

    public GameObject waveformObj;

    public bool isPlaying;
    public int editMode; //0 = auto generated notes by bpm; 1 = when developer presses arrow keys

    public Recording currentRecording; //Holds the track creations that can be saved

    public List<GameObject> noteGameObjects;

    private void Start()
    {
        //Populates song picker dropdown with songs from the songs list
        for(int i = 0; i < songs.Count; i++)
            songPickerDropdown.GetComponent<TMP_Dropdown>().options.Add(new TMP_Dropdown.OptionData() { text = songs[i].name + " | " +  songBPMs[i]});

        currentRecording = new Recording();
    }

    private void Update()
    {
        if (isPlaying) //Update Waveform \/
            waveformObj.transform.GetChild(0).transform.localPosition = new Vector3(((audioSource.time * waveformObj.GetComponent<RectTransform>().sizeDelta.x) / audioSource.clip.length) - (waveformObj.GetComponent<RectTransform>().sizeDelta.x / 2), 0, 0);
    }

    public void UpdateNotesHit(int i)
    {
        notesHit += i;
        notesHitTxt.GetComponent<TextMeshProUGUI>().text = "Notes Hit: " + notesHit;
    }

    public void UpdateNotesMissed(int i)
    {
        notesMissed += i;
        notesMissedTxt.GetComponent<TextMeshProUGUI>().text = "Notes Missed: " + notesMissed;
    }

    public void UpdateMissclicks(int i)
    {
        missClicks += i;
        missClicksTxt.GetComponent<TextMeshProUGUI>().text = "Misclicks: " + missClicks;
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
        if(audioSource.clip != null)
        {
            isPlaying = !isPlaying;
            if (isPlaying)
                StartLevel();
            else
                PauseLevel();
        }

        //Reset scroller
        sliderController.UpdateVals();
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
        Texture2D tex = GetComponent<WaveformVisualizer>().PaintWaveformSpectrum(audioSource.clip, 1, (int) waveformObj.GetComponent<RectTransform>().sizeDelta.x, (int) waveformObj.GetComponent<RectTransform>().sizeDelta.y, Color.yellow);
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

        //Reset scroller
        sliderController.UpdateVals();
    }

    public void SaveRecording() //Serializes recording to xml file
    {
        //Making sure all vals are set
        currentRecording.clipName = songs[selectedSongID].name;
        currentRecording.scrollSpeed = scrollerController.scrollSpeed;

        var serializer = new XmlSerializer(typeof(Recording));
        var stream = new FileStream(EditorUtility.SaveFilePanel("Save Recording", "", "new_recording", "xml"), FileMode.Create);
        serializer.Serialize(stream, currentRecording);
        stream.Close();
    }

    public void LoadRecording() //Deserializes chosen xml file and sets it as current recording
    {
        var serializer = new XmlSerializer(typeof(Recording));
        var stream = new FileStream(EditorUtility.OpenFilePanel("Pick Recording", "", "xml"), FileMode.Open);
        currentRecording = serializer.Deserialize(stream) as Recording;
        stream.Close();

        //Clear existing notes
        foreach (GameObject n in noteGameObjects)
            Destroy(n);
        noteGameObjects.Clear();

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
        scrollerController.transform.position = scrollerController.originalPos;

        //Update slider
        sliderController.UpdateSlider();

        //Update waveform
        CreateWaveform();

        //Generate notes
        foreach (Note n in currentRecording.notes)
            scrollerController.DeserializeNote(n.lane, n.pos);

        //Set start button to 'Start'
        isPlaying = false;
        PauseLevel();

        songPickerDropdown.GetComponent<TMP_Dropdown>().value = selectedSongID;
    }

    public void ClearNotes()
    {
        //Clear existing notes
        foreach (GameObject n in noteGameObjects)
        {
            currentRecording.notes.Remove(n.GetComponent<NoteController>().noteCodeObject);
            Destroy(n);
        }
        noteGameObjects.Clear();
    }
}