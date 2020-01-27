using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using TMPro;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using SFB;
using NAudio;
using NAudio.Wave;
using System.Threading.Tasks;

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

    public GameObject rhythmCanvasMiscObj;

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
    public List<Sprite> startPauseIcons; //0 = start, 1 = pause

    public List<KeyCode> laneKeycodes = new List<KeyCode>(); //index 0 = left lane, 1 = middle lane, 2 == right lane; values gotten by SelectorComponent.cs
    public List<KeyCode> manualGenKeycodes = new List<KeyCode>(); //^; alternate keys used for placing notes in manual gen mode
    public KeyCode placeSliderKeycode; //This key + a key from manualKeycode will place a slider of that type in manual gen mode
    public KeyCode placeSpaceKeycode; //This key will place a space note in manual gen mode

    public GameObject waveformObj;

    public bool isPlaying;
    public int editMode; //0 = auto generated notes by bpm; 1 = when player presses arrow keys; 2 = random note when player presses space

    public Recording currentRecording; //Holds the track creations that can be saved

    public List<GameObject> noteGameObjects;
    public List<GameObject> sliderGameObjects;
    public List<GameObject> spaceGameObjects;

    public string savedRecordingPath; //The path of the last saved recording
    public string savedRecordingName;

    public bool isSaved; //Set to false any time something changes, set to true when the recording is saved. Used to decide whether to prompt the player to save when exiting scene and/or autosave if implemented later

    public GameObject splashTitlePrefab;

    public NetworkingUtilities networkingUtilities;

    public GameObject loadingBar;
    public List<GameObject> loadingTextPeriods;

    public int loadAudioFilesToRun;
    public List<string> filesToAdd;
    public bool loadAudioFilesRunning;

    private void Start()
    {
        audioSource.volume *= PlayerPrefs.GetFloat("MusicVolume");

        StartCoroutine(LoadingBar());
        LoadSongs();
    }

    void StartPreparations()
    {
        for (int i = 0; i < songs.Count; i++) //Generate song BPMs, populate song picker dropdown
        {
            //int bpm = UniBpmAnalyzer.AnalyzeBpm(songs[i]);
            //songBPMs.Add(bpm / 2);
            songPickerDropdown.GetComponent<TMP_Dropdown>().options.Add(new TMP_Dropdown.OptionData() { text = songs[i].name });
        }

        if (CrossSceneController.recordingToLoad != "")
        {
            LoadRecording(CrossSceneController.recordingToLoad);
            CrossSceneController.recordingToLoad = "";
            CrossSceneController.clipToLoad = null;
        }
        else
        {
            currentRecording = new Recording();
            currentRecording.trackArtist = PlayerPrefs.GetString("username");
        }

        loadingBar.SetActive(false);
    }

    void LoadSongs()
    {
        Object[] temp = Resources.LoadAll("Songs", typeof(AudioClip)); //Read all audioclips in the Resources/Songs folder and add them to the 'Songs' list
        foreach (Object o in temp)
            songs.Add((AudioClip)o);

        System.IO.Directory.CreateDirectory(Application.persistentDataPath + "\\" + "Songs");
        string[] files = System.IO.Directory.GetFiles(Application.persistentDataPath + "\\" + "Songs");
        loadingBar.GetComponent<Slider>().maxValue = files.Length;
        loadAudioFilesToRun = files.Length;

        foreach (string filePath in files)
            filesToAdd.Add(filePath);
        StartCoroutine(RunLoadAudioFiles());
    }

    IEnumerator RunLoadAudioFiles()
    {
        loadingBar.transform.GetChild(3).GetComponent<TMP_Text>().text = 0 + "/" + filesToAdd.Count;
        for (int i = 0; i < loadAudioFilesToRun; i++)
        {
            loadAudioFilesRunning = true;
            StartCoroutine(LoadAudioFileStart(filesToAdd[i]));
            while (loadAudioFilesRunning)
            {
                yield return new WaitForSeconds(0.2f);
            }
            loadingBar.GetComponent<Slider>().value = i + 1;
            loadingBar.transform.GetChild(3).GetComponent<TMP_Text>().text = i + 1 + "/" + filesToAdd.Count;
        }
        StartPreparations();
    }

    private void Update()
    {
        if (isPlaying) //Update Waveform \/
            UpdateWaveform();       
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
        StartPauseButton.transform.GetChild(0).GetComponent<Image>().sprite = startPauseIcons[0];
    }

    void PauseLevel()
    {
        audioSource.Pause();
        StartPauseButton.transform.GetChild(0).GetComponent<Image>().sprite = startPauseIcons[1];
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

    //Manually generate notes by pressing the manual key codes from the manualKeyCodes list (likely 'A' = 'left arrow', 'W' = 'up arrow', 'D' = 'right arrow')
    void BeatGenEditMode()
    {
        editMode = 2;
        EditModeButton.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = "Beat Gen";
    }

    public void EditModeSelect()
    {
        if (editMode == 0)
            ManualEditMode();
        else if (editMode == 1)
            BeatGenEditMode();
        else if (editMode == 2)
            ManualEditMode(); //Replace with AutoGenEditMode to enable Auto Gen mode
    }

    void CreateWaveform()
    {
        Texture2D tex = GetComponent<WaveformVisualizer>().PaintWaveformSpectrum(audioSource.clip, 1, (int) waveformObj.GetComponent<RectTransform>().sizeDelta.x, (int) waveformObj.GetComponent<RectTransform>().sizeDelta.y, Color.white);
        waveformObj.GetComponent<Image>().sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        waveformObj.GetComponent<Image>().color = Color.white;
        waveformObj.SetActive(true);
    }

    public void UpdateWaveform()
    {
        if(audioSource.clip != null)
            waveformObj.transform.GetChild(0).localPosition = new Vector3(((sliderController.GetComponent<Slider>().value * waveformObj.GetComponent<RectTransform>().sizeDelta.x) / audioSource.clip.length) - (waveformObj.GetComponent<RectTransform>().sizeDelta.x / 2), 0, 0);
    }

    public void SelectSong()
    {
        selectedSongID = songPickerDropdown.GetComponent<TMP_Dropdown>().value;
        audioSource.clip = songs[selectedSongID];

        //Update Waveform
        CreateWaveform();
        UpdateWaveform();

        sliderController.SetSlider();
        sliderController.GetComponent<Slider>().value = 0;
        sliderController.UpdateVals();

        //Update recording song name
        currentRecording.clipName = songs[selectedSongID].name;

        if (isPlaying)
            StartPause();
    }

    public void SaveRecording() //Serializes recording to xml file
    {
        //Making sure all vals are set
        currentRecording.clipName = songs[selectedSongID].name;
        currentRecording.scrollSpeed = scrollerController.scrollSpeed;

        var serializer = new XmlSerializer(typeof(Recording));

        var extensions = new[] {
            new ExtensionFilter("XML", "xml" ), };
        string path = StandaloneFileBrowser.SaveFilePanel("Save File", "", "", extensions);

        if (path.Length != 0)
        {
            isSaved = true;

            if (path.Substring(path.Length - 4) != ".xml")
                path += ".xml";

            savedRecordingPath = path;

            savedRecordingName = savedRecordingPath.Substring(savedRecordingPath.LastIndexOf('\\') + 1);
            savedRecordingName = savedRecordingName.Remove(savedRecordingName.Length - 4);

            var stream = new FileStream(path, FileMode.Create);
            serializer.Serialize(stream, currentRecording);
            stream.Close();

            SpawnSplashTitle("Saved Successfully", Color.green);
        }
        else
            SpawnSplashTitle("Save failed, no path", Color.red);
    }

    public void LoadRecording() //Deserializes chosen xml file and sets it as current recording
    {
        var serializer = new XmlSerializer(typeof(Recording));
        string path = "";

        var extensions = new[] {
            new ExtensionFilter("XML", "xml" ), };

        string[] temp = StandaloneFileBrowser.OpenFilePanel("Open File", "", extensions, false);
        if (temp.Length != 0)
            path = temp[0];

        if (path.Length != 0)
        {
            savedRecordingPath = path;

            if (path.Substring(path.Length - 4) != ".xml")
                path += ".xml";

            savedRecordingName = savedRecordingPath.Substring(savedRecordingPath.LastIndexOf('\\') + 1);
            savedRecordingName = savedRecordingName.Remove(savedRecordingName.Length - 4);

            var stream = new FileStream(path, FileMode.Open);
            currentRecording = serializer.Deserialize(stream) as Recording;
            stream.Close();

            ClearRhythmInScene();

            //Update currently selected song
            foreach (AudioClip clip in songs)
                if (clip.name == currentRecording.clipName)
                    selectedSongID = songs.IndexOf(clip);

            audioSource.clip = songs[selectedSongID];
            audioSource.time = 0;

            //Update scroll speed
            scrollerController.scrollSpeed = currentRecording.scrollSpeed;

            //Reset scroller to start
            scrollerController.transform.localPosition = scrollerController.originalPos;

            //Update waveform
            CreateWaveform();
            UpdateWaveform();

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

            isSaved = true;
            isPlaying = false;

            //Update UI vals
            sliderController.SetSlider();
            sliderController.GetComponent<Slider>().value = 0;
            sliderController.UpdateVals();

            laneCountPicker.GetComponent<TMP_InputField>().text = "" + currentRecording.laneCount;
            scrollerController.speedPickerInputField.GetComponent<TMP_InputField>().text = "" + currentRecording.scrollSpeed;

            SpawnSplashTitle("Loaded Successfully", Color.green);
        }
        else
            SpawnSplashTitle("Load failed, no path", Color.red);
    }

    public void LoadRecording(string path) //Deserializes xml file from resources folder
    {
        var serializer = new XmlSerializer(typeof(Recording));

        if (path.Length != 0)
        {
            savedRecordingPath = path;

            if (path.Substring(path.Length - 4) != ".xml")
                path += ".xml";

            savedRecordingName = savedRecordingPath.Substring(savedRecordingPath.LastIndexOf('\\') + 1);
            savedRecordingName = savedRecordingName.Remove(savedRecordingName.Length - 4);

            var stream = new FileStream(path, FileMode.Open);
            currentRecording = serializer.Deserialize(stream) as Recording;
            stream.Close();

            ClearRhythmInScene();

            //Update currently selected song
            foreach (AudioClip clip in songs)
                if (clip.name == currentRecording.clipName)
                    selectedSongID = songs.IndexOf(clip);

            audioSource.clip = songs[selectedSongID];
            audioSource.time = 0;

            //Update scroll speed
            scrollerController.scrollSpeed = currentRecording.scrollSpeed;

            //Reset scroller to start
            scrollerController.transform.localPosition = scrollerController.originalPos;

            //Update waveform
            CreateWaveform();
            UpdateWaveform();

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

            isSaved = true;
            isPlaying = false;

            sliderController.SetSlider();
            sliderController.GetComponent<Slider>().value = 0;
            sliderController.UpdateVals();

            SpawnSplashTitle("Loaded Successfully", Color.green);
        }
        else
            SpawnSplashTitle("Load failed, no path", Color.red);
    }

    public void StartImportSong()
    {
        string path = "";
        var extensions = new[] {
            new ExtensionFilter("MPEG File", "mp3"), };

        string[] temp = StandaloneFileBrowser.OpenFilePanel("Open File", "", extensions, false);
        if (temp.Length != 0)
            path = temp[0];

        if (path.Length != 0)
        {
            StartCoroutine(LoadAudioFile(path));
        }
    }

    public void AddClipToSongs(AudioClip song)
    {
        songs.Add(song);
    }

    IEnumerator LoadAudioFile(string path)
    {
        UnityWebRequest AudioFiles = null;
        string audioFileName = path.Substring(path.LastIndexOf('\\') + 1);
        audioFileName = audioFileName.Remove(audioFileName.Length - 4);
        
        //Copy mp3 to application persistent data path \ Songs
        File.Copy(path, Application.persistentDataPath + "\\" + "Songs" + "\\" + audioFileName + ".mp3");
        path = Application.persistentDataPath + "\\" + "Songs" + "\\" + audioFileName + ".mp3";

        //Convert selected .mp3 to .wav temporarily for importing
        string tempWavPath = Application.persistentDataPath + "\\" + "Songs" + "\\" + audioFileName + ".wav";
        Mp3ToWav(path, tempWavPath);

        //Convert temporary wav file to audio clip, delete wav
        AudioFiles = UnityWebRequestMultimedia.GetAudioClip(tempWavPath, AudioType.WAV);
        AudioClip clip = null;
        if (AudioFiles != null)
        {
            yield return AudioFiles.SendWebRequest();
            if (AudioFiles.isNetworkError)
                Debug.Log(AudioFiles.error);
            else
            {
                clip = DownloadHandlerAudioClip.GetContent(AudioFiles);
                clip.name = audioFileName;
                AddClipToSongs(clip);
            }
        }
        File.Delete(tempWavPath);

        FinishImportSong(clip);
    }

    void FinishImportSong(AudioClip clip)
    {
        songPickerDropdown.GetComponent<TMP_Dropdown>().options.Add(new TMP_Dropdown.OptionData() { text = clip.name });
    }

    IEnumerator LoadAudioFileStart(string path)
    {
        UnityWebRequest AudioFiles = null;

        string audioFileName = path.Substring(path.LastIndexOf('\\') + 1);
        audioFileName = audioFileName.Remove(audioFileName.Length - 4);

        string tempWavPath = Application.persistentDataPath + "\\" + "Songs" + "\\" + audioFileName + ".wav";
        Mp3ToWav(path, tempWavPath);

        AudioFiles = UnityWebRequestMultimedia.GetAudioClip(tempWavPath, AudioType.WAV);

        if (AudioFiles != null)
        {
            yield return AudioFiles.SendWebRequest();
            if (AudioFiles.isNetworkError)
                Debug.Log(AudioFiles.error);
            else
            {
                AudioClip clip = DownloadHandlerAudioClip.GetContent(AudioFiles);
                clip.name = audioFileName;
                AddClipToSongs(clip);
            }
        }
        loadAudioFilesRunning = false;
        File.Delete(tempWavPath);
    }

    public void Mp3ToWav(string mp3File, string outputFile)
    {
        using (Mp3FileReader reader = new Mp3FileReader(mp3File))
        {
            WaveFileWriter.CreateWaveFile(outputFile, reader);
        }
    }

    public void ClearRhythmInScene() //Clears all notes, sliders, and spaces
    {
        ClearNotes();
        ClearSliders();
        ClearSpaces();
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

    void ClearSpaces()
    {
        //Clear existing spaces
        foreach (GameObject s in spaceGameObjects)
        {
            currentRecording.spaces.Remove(s.GetComponent<SpaceController>().spaceCodeObject);
            Destroy(s);
        }
        spaceGameObjects.Clear();

        UpdateSpaceCount(-spaceCount);
    }

    public void SetLaneCount()
    {
        int canParse = 0;
        int.TryParse(laneCountPicker.GetComponent<TMP_InputField>().text, out canParse); //Prevents parse error
        if (canParse != 0)
        {
            int count = int.Parse(laneCountPicker.GetComponent<TMP_InputField>().text);
            if (count > 0 && count <= lanes.Count)
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
                laneCountPicker.transform.GetChild(0).GetChild(1    ).GetComponent<TMP_Text>().text = "" + count;
            }
            else
            {
                if (count < 0) //If they inputted something less than 0
                    count = 0;
                else //If they inputted something greater than the max lane count
                    count = lanes.Count;
                laneCountPicker.GetComponent<TMP_InputField>().text = "" + count;
                laneCountPicker.transform.GetChild(0).GetChild(1).GetComponent<TMP_Text>().text = "" + count;
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
        string temp = "";
        if (currentRecording.trackArtist != "")
            temp = currentRecording.trackArtist;
        currentRecording = new Recording();
        currentRecording.clipName = songs[selectedSongID].name;
        currentRecording.scrollSpeed = scrollerController.scrollSpeed;
        currentRecording.laneCount = laneCount;
        if (temp != "")
            currentRecording.trackArtist = temp;
        else
            currentRecording.trackArtist = PlayerPrefs.GetString("username");

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

    public void TestTrack()
    {
        if (!isSaved || savedRecordingPath.Length == 0)
            SaveRecording();

        if (savedRecordingPath.Length != 0)
        {
            CrossSceneController.SceneToGame(savedRecordingPath, audioSource.clip);
            StartCoroutine(LoadAsyncScene("Overworld"));
        }
    }

    public void ExitRhythmMaker()
    {
        StartCoroutine(LoadAsyncScene("MainMenu"));
    }

    public void SpawnSplashTitle(string titleText, Color titleColor)
    {
        GameObject newSplashTitle = Instantiate(splashTitlePrefab, rhythmCanvasMiscObj.transform);
        newSplashTitle.GetComponent<TMP_Text>().text = titleText;
        newSplashTitle.GetComponent<TMP_Text>().color = titleColor;
        StartCoroutine(KillSplashTitle(newSplashTitle));
    }

    IEnumerator KillSplashTitle(GameObject title)
    {
        yield return new WaitForSeconds(title.GetComponent<Animation>().clip.length);
        Destroy(title);
    }

    public void UploadRecording()
    {
        //Upload to workshop
        networkingUtilities.UploadRecording("gabrieltm9", savedRecordingPath, savedRecordingName + ".xml");

        //Create leaderboard table1
        networkingUtilities.NewLeaderboard(savedRecordingName);
    }

    IEnumerator LoadAsyncScene(string scene)
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(scene);
        StartCoroutine(LoadingBar());
        // Wait until the asynchronous scene fully loads
        while (!asyncLoad.isDone)
        {
            loadingBar.GetComponent<Slider>().value = asyncLoad.progress;
            yield return null;
        }
    }

    IEnumerator LoadingBar()
    {
        loadingBar.SetActive(true);
        int periodIndex = 0;
        while (loadingBar.activeSelf)
        {
            for (int i = 0; i < loadingTextPeriods.Count; i++)
                if (i == periodIndex)
                    loadingTextPeriods[i].SetActive(true);

            if (periodIndex == loadingTextPeriods.Count)
            {
                periodIndex = 0;
                foreach (GameObject obj in loadingTextPeriods)
                    obj.SetActive(false);
            }
            else
                periodIndex++;

            yield return new WaitForSeconds(0.5f);
        }
    }
}