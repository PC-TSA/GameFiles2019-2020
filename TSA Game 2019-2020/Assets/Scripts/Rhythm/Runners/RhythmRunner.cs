using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.Networking;
using UnityEngine.Video;

public class RhythmRunner : MonoBehaviour
{
    public string XMLRecordingName; //This is the note track recording that will be used in this scene; Searches for an XML file with this exact name in the 'Assets/Resources/' folder
    public TextAsset XMLRecordingAsset;
    public string XMLRecordingPath; //The path of the xml file being loaded

    public int notesHit = 0; //How many notes hit
    public int notesMissed = 0; //How many notes missed
    public int missClicks = 0; //How many clicks that didnt hit a note
    public int combo = 1; //Current combo (ex. 10 notes without missing one / misclicking
    public int comboLvl = 0; //Level of combo (ex. 10+ combo = lvl1, 20+ combo = lvl2, 30+ combo = lvl3; Different levels = different visual effects / bonuses)
    public int deathCount = 0; //How many notes have been missed in a row. (ex. if you miss 5, but then get 1 right, your deathCount is 4. Doesnt go above 0 and works independently of combo)
    public int health = 20; //How much deathCount needs to reach to lose the game
    public float score; //Current score in a run; Every note hit += current combo, every FixedUpdate call a slider is hit + current combo * 0.01
    
    public float accuracy; //Average accuracy between hits; Used to get rank ^
    public float totalAccuracy; //Each note hit's accuracy is added to this
    public int accuracyTimesAdded; //Essentially how many notes have been hit. Divide total accuracy by this
    public string ranking; //Letter ranking = accuracy in sections; D = < 30, C = 30-50, B = 50-70, A = 70-90, S = 90-95, SS = 95-100

    public int perfectHits;
    public int goodHits;
    public int okayHits;
    public int badHits;
    public int maxCombo;

    public GameObject rhythmCanvasObj;

    public GameObject notesHitTxt;
    public GameObject notesMissedTxt;
    public GameObject missClicksTxt;
    public GameObject comboTxt;
    public GameObject deathCountTxt;
    public GameObject scoreCountTxt;
    public GameObject accuracyTxt;
    public GameObject rankingTxt;

    public List<AudioClip> songs;

    public GameObject scrollerObj;

    public bool isRunning; //If the rhythm portion is currently running, meaning a song is playing and notes are scrolling
    public bool canRun; //Set to true after xml is loaded

    public List<GameObject> lanes; //The parent objs for each lane; Are disabled/enabled in SetLaneCount when loading the recording
    public int laneCount; //How many lanes to have (1-5)

    public Recording currentRecording;
    public int currentRecordingID;

    public List<GameObject> arrowPrefabs;
    public List<GameObject> sliderPrefabs;
    public GameObject sliderMaskPrefab;
    public GameObject spacePrefab;

    public GameObject notesParent;
    public GameObject slidersParent;
    public GameObject spacesParent;

    public List<Sprite> arrowSprites; //0 = right, 1 = up, 2 = left, 3 = down

    public AudioSource audioSource;
    public List<AudioSource> sfxSources;

    public float scrollSpeed;

    public Vector3 originalPos;

    public GameObject playerObj;
    public TrailRenderer[] trailRenderers;
    public Gradient[] comboTrailGradients; //0 = lvl 1, 1 = lvl 2, 2 = lvl 3

    public bool isAnimSpeedupRunning; //Made so AnimSpeedUpTimer coorutine cant be started multiple times simultaneously, as that would likely desync that animation 

    public PostProcessVolume postProcessingVolume;
    Vignette vignette;
    ColorGrading colorGrading;
    public float vignetteNewVal;
    public float vignetteLerpSpeed;

    public GameObject rhythmMakerButton;
    public GameObject levelSelectButton;

    public float customSpeed;

    public Color selectorColor;
    public Color selectorPressColor;

    public GameObject spaceSelector;
    public float backgroundWidth;
    public float dividerWidth;

    public GameObject splashTitlePrefab;
    public GameObject splashImagePrefab;

    public GameObject loadingBar;
    public List<GameObject> loadingTextPeriods;

    public GameObject endTrackScreen;

    public GameObject fireworksParent;

    public Slider deathCountSlider;

    public bool hasLost;
    public bool goToLostVals;
    public GameObject cameraTrack;

    public List<Sprite> splashImages;

    //Tutorial UI
    public AudioClip tutorialSong;
    public GameObject tutorialUI;
    public TMP_Text tutorialTitleText;
    public TMP_Text tutorialMainText;
    public bool isTutorialActive;
    public List<GameObject> tutorialPages;
    public List<float> tutorialTimeouts;
    public int currentTutorialPage;
    public float tutorialTargetAlpha;
    public float tutorialFadeSpeed;
    public bool fadeTutorial;

    public GameObject pauseMenu;
    public bool isPaused;

    public GameObject optionsMenu;
    public Slider musicVolumeSlider;
    public Slider sfxVolumeSlider;

    public RawImage rawImage;
    public VideoPlayer videoPlayer;
    public bool playVideo;

    public bool canEnd = true;

    public bool shouldUseVideo = true;

    private void Start()
    {
        if (videoPlayer != null && shouldUseVideo)
            videoPlayer.Prepare();
        else
            shouldUseVideo = false;

        originalPos = scrollerObj.transform.localPosition;

        //Get vignette from post processing profile
        postProcessingVolume.profile.TryGetSettings(out vignette);
        postProcessingVolume.profile.TryGetSettings(out colorGrading);

        //Set music/sfx volumes
        audioSource.volume *= PlayerPrefs.GetFloat("MusicVolume");
        foreach (AudioSource source in sfxSources)
            source.volume = sfxVolumeSlider.value;

        //Set options menu
        musicVolumeSlider.value = PlayerPrefs.GetFloat("MusicVolume");
        sfxVolumeSlider.value = PlayerPrefs.GetFloat("SFXVolume");

        if (SceneManager.GetActiveScene().name == "TutorialScene")
        {
            isTutorialActive = true;
            songs.Add(tutorialSong);
            StartCoroutine(DelayedStart(1));
            StartCoroutine(TutorialPageCycle());
            Cursor.visible = false;

        }
        else if (CrossSceneController.recordingToLoad.Length != 0) //If transfering song from other scene, load from given path instead of predefined file name from build Resources
        {
            currentRecordingID = CrossSceneController.recordingToLoadID;
            if (CrossSceneController.isCampaign)
            {
                XMLRecordingName = CrossSceneController.recordingToLoad;
                StartCoroutine(DelayedStart(1));
            }
            else
            {
                XMLRecordingPath = CrossSceneController.recordingToLoad;
                string name = XMLRecordingPath.Substring(XMLRecordingPath.LastIndexOf('\\') + 1);
                XMLRecordingName = name.Remove(name.Length - 4);
                songs.Add(CrossSceneController.clipToLoad);
                audioSource.clip = CrossSceneController.clipToLoad;
                StartCoroutine(DelayedStart(1, XMLRecordingPath));
            }

            if (CrossSceneController.previousScene == "RhythmMaker")
            {
                endTrackScreen.GetComponent<EndTrackScreenController>().isTestTrack = true;
                rhythmMakerButton.SetActive(true);
                Cursor.visible = true;
            }
            else if (CrossSceneController.previousScene == "LevelSelect")
                levelSelectButton.SetActive(true);
        }
        else
        {
            Object[] temp = Resources.LoadAll("Songs", typeof(AudioClip)); //Read all audioclips in the Resources/Songs folder and add them to the 'Songs' list
            foreach (Object o in temp)
                songs.Add((AudioClip)o);
            StartCoroutine(DelayedStart(1));
            Cursor.visible = false;
        }

        deathCountSlider.maxValue = health;
        deathCountSlider.value = health;
    }

    private void Update()
    {
        //Smoothly lerps vignette to deathCount / 20 value
        vignette.intensity.value = Mathf.Lerp(vignette.intensity.value, vignetteNewVal, Time.deltaTime * vignetteLerpSpeed);

        if(deathCountSlider.value != health - deathCount)
            deathCountSlider.value = health - deathCount;

        if (goToLostVals)
        {
            colorGrading.saturation.value = Mathf.Lerp(colorGrading.saturation.value, -30, Time.deltaTime * 3);
            audioSource.volume = Mathf.Lerp(audioSource.volume, 0, Time.deltaTime * 3);
            scrollSpeed = Mathf.Lerp(scrollSpeed, 0, Time.deltaTime * 3);
            if (playerObj != null)
            {
                if(playerObj.transform.parent.GetComponent<PathCreation.Examples.PathFollower>() != null)
                    playerObj.transform.parent.GetComponent<PathCreation.Examples.PathFollower>().speed = Mathf.Lerp(playerObj.transform.parent.GetComponent<PathCreation.Examples.PathFollower>().speed, 0, Time.deltaTime * 3);
                if (playerObj.GetComponent<Animator>() != null)
                    playerObj.GetComponent<Animator>().speed = Mathf.Lerp(playerObj.GetComponent<Animator>().speed, 0, Time.deltaTime * 3);
            }
        }

        if (Input.GetKeyDown(KeyCode.Escape) && !optionsMenu.activeSelf)
            TogglePause(true);

        if (isTutorialActive)
        {
            if (Input.GetKeyDown(KeyCode.Return))
            {
                if (currentTutorialPage != tutorialPages.Count)
                    tutorialUI.SetActive(false);
            }
            if (fadeTutorial)
            {
                tutorialUI.GetComponent<CanvasGroup>().alpha = Mathf.Lerp(tutorialUI.GetComponent<CanvasGroup>().alpha, tutorialTargetAlpha, tutorialFadeSpeed * Time.deltaTime);
                if(Mathf.Abs(tutorialUI.GetComponent<CanvasGroup>().alpha - tutorialTargetAlpha) < 0.01f)
                    fadeTutorial = false;
            }
        }
        
        if(isRunning)
            CheckForFinish();

        GabeCode();
    }

    private void FixedUpdate()
    {
        //Scroller
        if (isRunning)
            scrollerObj.transform.localPosition = new Vector3(scrollerObj.transform.localPosition.x, scrollerObj.transform.localPosition.y - scrollSpeed, scrollerObj.transform.localPosition.z);
    }

    void CheckForFinish()
    {
        if (scrollerObj.transform.GetChild(0).childCount + scrollerObj.transform.GetChild(1).childCount + scrollerObj.transform.GetChild(2).childCount == 0 && canEnd)
            FinishTrack();
    }

    IEnumerator TimeToFinish()
    {
        yield return new WaitForSeconds(4);
        FinishTrack();
    }

    IEnumerator TutorialPageCycle()
    {
        yield return new WaitForSeconds(tutorialTimeouts[currentTutorialPage]);
        if(!hasLost)
        {
            tutorialUI.GetComponent<CanvasGroup>().alpha = 0;
            tutorialUI.SetActive(true);
            if (currentTutorialPage != tutorialPages.Count - 1)
                PauseRhythm(false);
            else
                Cursor.visible = true;
            if (currentTutorialPage != 0)
                tutorialPages[currentTutorialPage - 1].SetActive(false);
            currentTutorialPage++;
            tutorialPages[currentTutorialPage - 1].SetActive(true);
            tutorialTargetAlpha = 1;
            fadeTutorial = true;
            while (tutorialUI.activeSelf)
                yield return new WaitForSeconds(0);
            UnPauseRhythm(false);
            if (currentTutorialPage < tutorialPages.Count)
                StartCoroutine(TutorialPageCycle());
        }
    }

    public void TogglePause(bool useMenu)
    {
        isPaused = !isPaused;
        if (isPaused)
            PauseRhythm(useMenu);
        else
            UnPauseRhythm(useMenu);
    }

    public void PauseRhythm(bool useMenu)
    {
        if (useMenu)
            pauseMenu.SetActive(true);

        if(playerObj != null)
        {
            playerObj.GetComponent<Animator>().speed = 0;
            playerObj.transform.parent.GetComponent<PathCreation.Examples.PathFollower>().enabled = false;
            if (playerObj.transform.parent.childCount > 1 && playerObj.transform.parent.GetChild(1) != null && playerObj.transform.parent.GetChild(1).name == "Bike")
                playerObj.transform.parent.GetChild(1).GetComponent<Animator>().enabled = false;
            cameraTrack.SetActive(false);
        }
        isRunning = false;
        audioSource.Pause();
        Cursor.visible = true;
    }

    public void UnPauseRhythm(bool useMenu)
    {
        if (useMenu)
            pauseMenu.SetActive(false);

        if (playerObj != null)
        {
            playerObj.GetComponent<Animator>().speed = 1;
            playerObj.transform.parent.GetComponent<PathCreation.Examples.PathFollower>().enabled = true;
            if (playerObj.transform.parent.childCount > 1 && playerObj.transform.parent.GetChild(1) != null && playerObj.transform.parent.GetChild(1).name == "Bike")
                playerObj.transform.parent.GetChild(1).GetComponent<Animator>().enabled = true;
            cameraTrack.SetActive(true);
        }
        isRunning = true;
        audioSource.UnPause();
        Cursor.visible = false;
    }

    public void ToMainMenu()
    {
        StartCoroutine(LoadAsyncScene("MainMenu"));
    }

    public void UpdateNotesHit(int i)
    {
        notesHit += i;
        //notesHitTxt.GetComponent<TextMeshProUGUI>().text = "Notes Hit: " + notesHit;
        UpdateDeathCount(-i); //-i because the lower the death count the better and hitting a note is good
        UpdateCombo(i);
        //AnimSpeedUp();
    }

    public void UpdateNotesMissed(int i)
    {
        notesMissed += i;
        //notesMissedTxt.GetComponent<TextMeshProUGUI>().text = "Notes Missed: " + notesMissed;
        UpdateDeathCount(i);
        UpdateCombo(-combo); //Break combo
        UpdateAccuracy(accuracy / 2); //Accuracy / 2 to make it less harsh; Set to 0 for harder
    }

    public void UpdateMissclicks(int i)
    {
        missClicks += i;
        //missClicksTxt.GetComponent<TextMeshProUGUI>().text = "Misclicks: " + missClicks;
        UpdateDeathCount(i);
        UpdateCombo(-combo); //Break combo
        UpdateAccuracy(accuracy / 2); //Accuracy / 2 to make it less harsh; Set to 0 for harder
    }

    public void UpdateAccuracy(float i)
    {
        totalAccuracy += i;
        accuracyTimesAdded++;
        accuracy = totalAccuracy / accuracyTimesAdded;
        accuracy = Mathf.Clamp((Mathf.Round(accuracy * 10) / 10), 0, 100);
        UpdateRanking();
        accuracyTxt.GetComponent<TMP_Text>().text = accuracy + "%";
    }

    public void UpdateRanking()
    {
        if (accuracy <= 30)
            ranking = "D";
        else if (accuracy > 30 && accuracy <= 50)
            ranking = "C";
        else if (accuracy > 50 && accuracy <= 70)
            ranking = "B";
        else if (accuracy > 70 && accuracy <= 90)
            ranking = "A";
        else if (accuracy > 90 && accuracy <= 95)
            ranking = "S";
        else if (accuracy > 95)
            ranking = "SS";
        rankingTxt.GetComponent<TMP_Text>().text = ranking;
    }

    public void UpdateScore(float multiplier)
    {
        score += (combo) * multiplier;
        score = Mathf.Round(score * 100) / 100;
        scoreCountTxt.GetComponent<TextMeshProUGUI>().text = "" + score;
    }

    public void LoadRecording() //Deserializes xml file and sets it as current recording
    {
        if (XMLRecordingName.Substring(XMLRecordingName.Length - 4) != ".xml") //If xml file to load doesnt have .xml extension, meaning it is build in Resources instead of a full path, load from resources
            XMLRecordingAsset = Resources.Load<TextAsset>("Recordings/" + XMLRecordingName);
        else
            return;

        var serializer = new XmlSerializer(typeof(Recording));
        var reader = new System.IO.StringReader(XMLRecordingAsset.text);
        currentRecording = serializer.Deserialize(reader) as Recording;
        reader.Close();

        //Load song
        if(audioSource.clip == null)
        {
            foreach (AudioClip clip in songs)
                if (clip.name == currentRecording.clipName)
                    audioSource.clip = clip;
        }
        audioSource.time = 0;

        //Update scroll speed
        scrollSpeed = currentRecording.scrollSpeed;
        FindObjectOfType<SelectorRunner>().sliderHeightChange = scrollSpeed;

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

        canRun = true;
    }

    public void LoadRecording(string path) //Deserializes xml file at path and sets it as current recording
    {
        var serializer = new XmlSerializer(typeof(Recording));
        if (path.Length != 0)
        {
            var stream = new FileStream(path, FileMode.Open);
            currentRecording = serializer.Deserialize(stream) as Recording;
            stream.Close();

            //Load song
            if (audioSource.clip == null)
            {
                foreach (AudioClip clip in songs)
                    if (clip.name == currentRecording.clipName)
                        audioSource.clip = clip;
            }
            audioSource.time = 0;

            //Update scroll speed
            scrollSpeed = currentRecording.scrollSpeed;
            FindObjectOfType<SelectorRunner>().sliderHeightChange = scrollSpeed;

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

            foreach (SelectorRunner selector in FindObjectsOfType<SelectorRunner>())
                selector.sliderHeightChange = scrollSpeed;

            canRun = true;
        }
    }

    public void DeserializeNote(Note n)
    {
        GameObject newNote = Instantiate(arrowPrefabs[n.lane], new Vector3(0, 0, 0), transform.rotation, notesParent.transform);
        newNote.transform.SetSiblingIndex(0);
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
        sliderSpriteChild.GetComponent<BoxCollider>().size = new Vector3(sliderSpriteChild.GetComponent<BoxCollider>().size.x, s.height - 70, sliderSpriteChild.GetComponent<BoxCollider>().size.z);
        sliderSpriteChild.GetComponent<BoxCollider>().center = new Vector3(sliderSpriteChild.GetComponent<BoxCollider>().center.x, sliderSpriteChild.GetComponent<BoxCollider>().center.y - 35, sliderSpriteChild.GetComponent<BoxCollider>().center.z); //-35 & -70 (above) to make slider not need to completely exit selector zone to end sliding
    }

    public void DeserializeSpace(SpaceObj s)
    {
        GameObject newSpace = Instantiate(spacePrefab, new Vector3(0, 0, 0), transform.rotation, spacesParent.transform);
        newSpace.transform.localPosition = s.pos;
        newSpace.transform.localRotation = transform.rotation;
        newSpace.GetComponent<RectTransform>().sizeDelta = new Vector2(s.width, newSpace.GetComponent<RectTransform>().sizeDelta.y);
        newSpace.GetComponent<BoxCollider>().size = new Vector3(s.width, newSpace.GetComponent<BoxCollider>().size.y, newSpace.GetComponent<BoxCollider>().size.z);
    }

    IEnumerator StartRun() //Proper start init
    {
        //Prepare video
        if (shouldUseVideo)
        {
            while (!videoPlayer.isPrepared || !canRun)
                yield return new WaitForSeconds(0.1f);
            rawImage.texture = videoPlayer.texture;
            videoPlayer.Play();
        }

        //Adjust scroller obj y + delay song play
        StartCoroutine(AdjustSongPlay(2.5f));

        if(playerObj != null)
            playerObj.transform.parent.GetComponent<PathCreation.Examples.PathFollower>().enabled = true;
        if(cameraTrack != null)
            cameraTrack.SetActive(true);
        isRunning = true;
    }

    IEnumerator AdjustSongPlay(float s)
    {
        scrollerObj.transform.localPosition = new Vector3(originalPos.x, originalPos.y + (scrollSpeed * 50 * s), originalPos.z);
        yield return new WaitForSeconds(s);
        audioSource.Play();
    }

    IEnumerator DelayedStart(int seconds)
    {
        yield return new WaitForSeconds(seconds);
        LoadRecording();
        StartCoroutine(StartRun());
    }

    IEnumerator DelayedStart(int seconds, string path)
    {
        yield return new WaitForSeconds(seconds);
        LoadRecording(path);
        StartCoroutine(StartRun());
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
           
            deathCountSlider.value = health - deathCount;
        }
    }

    void Lose()
    {
        Debug.Log("Track Failed!");
        ranking = "F";
        hasLost = true;
        foreach(SelectorRunner selector in GameObject.FindObjectsOfType<SelectorRunner>())
            selector.SetRegularSprite();
        StartCoroutine(GoToLostVals());
    }

    IEnumerator GoToLostVals()
    {
        goToLostVals = true; //When true, vals lerp in Update
        yield return new WaitForSeconds(1.5f);
        audioSource.Stop();
        //cameraTrack.GetComponent<CPC_CameraPath>().StopPath(); STOP CAMERA MOVEMENT
        if (playerObj != null)
            playerObj.transform.parent.GetComponent<PathCreation.Examples.PathFollower>().enabled = false;
        cameraTrack.SetActive(false);
        endTrackScreen.SetActive(true);
        endTrackScreen.GetComponent<EndTrackScreenController>().clearedOrFailedTxt.GetComponent<TMP_Text>().text = "Track Failed";
        yield return new WaitForSeconds(1.5f);
        goToLostVals = false;
    }

    void UpdateCombo(int i)
    {
        combo += i;
        if (combo > maxCombo)
            maxCombo = combo;
        comboTxt.GetComponent<TextMeshProUGUI>().text = "Combo: " + combo;
        if (combo < 10)
            comboLvl = 0;
        else
        {
            switch (combo)
            {
                case 10:
                    comboLvl = 1;
                    SpawnSplashImage(splashImages[0]);
                    break;
                case 20:
                    comboLvl = 2;
                    break;
                case 25:
                    SpawnSplashImage(splashImages[1]);
                    break;
                case 30:
                    comboLvl = 3;
                    break;
                case 50:
                    SpawnSplashImage(splashImages[2]);
                    break;
                case 100:
                    SpawnSplashImage(splashImages[3]);
                    break;
                case 200:
                    SpawnSplashImage(splashImages[4]);
                    break;
                case 300:
                    SpawnSplashImage(splashImages[5]);
                    break;
                case 400:
                    SpawnSplashImage(splashImages[6]);
                    break;
                case 500:
                    SpawnSplashImage(splashImages[7]);
                    break;
            }

            //Enable trails & set their color //TRAIL RENDERERS DISABLED UNTIL FURTHER NOTICE
            /*foreach (TrailRenderer tr in trailRenderers)
            {
                tr.emitting = true;
                tr.colorGradient = comboTrailGradients[comboLvl - 1];
                //tr.material.SetColor("_EmissionColor", comboTrailGradients[comboLvl - 1].colorKeys[0].color); SETTING MAT COLOR IS BROKEN DUE TO IT BEING OVERRIDEN BY MATERIAL EMISSION, POSSIBLE FIX WITH A LINE LIKE THIS ONE
            }*/
        }
    }

    void AnimSpeedUp() //Speeds up character animation briefly to increase impact when note is pressed
    {
        if(comboLvl >= 0 && !isAnimSpeedupRunning)
        {
            if(playerObj != null)
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

    public void ToRhythmMaker()
    {
        CrossSceneController.GameToMaker(XMLRecordingPath);
        StartCoroutine(LoadAsyncScene("RhythmMaker"));
    }

    public void ToLevelSelect()
    {
        StartCoroutine(LoadAsyncScene("LevelSelect"));
    }

    public void PauseMenuExit()
    {
        string scene = CrossSceneController.previousScene;
        if (scene == "RhythmMaker")
            CrossSceneController.GameToMaker(XMLRecordingPath);

        StartCoroutine(LoadAsyncScene(scene));
    }

    public void SpawnSplashTitle(string titleText, Color titleColor)
    {
        GameObject newSplashTitle = Instantiate(splashTitlePrefab, rhythmCanvasObj.transform);
        newSplashTitle.GetComponent<TMP_Text>().text = titleText;
        newSplashTitle.GetComponent<TMP_Text>().color = titleColor;
        StartCoroutine(KillSplashTitle(newSplashTitle));
    }

    public void SpawnSplashImage(Sprite image)
    {
        GameObject newSplashImage = Instantiate(splashImagePrefab, rhythmCanvasObj.transform);
        newSplashImage.GetComponent<Image>().sprite = image;
        StartCoroutine(KillSplashTitle(newSplashImage));
    }

    IEnumerator KillSplashTitle(GameObject title)
    {
        yield return new WaitForSeconds(title.GetComponent<Animation>().clip.length);
        Destroy(title);
    }

    public IEnumerator LoadAsyncScene(string scene)
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(scene);
        StartCoroutine(LoadingBar());
        Debug.Log("Loading scene " + scene);
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
        while (true)
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

    void GabeCode() //Cheat codes haha
    {
        //Kill all notes
        if (Input.GetKey(KeyCode.LeftShift) && Input.GetKey(KeyCode.LeftAlt) && Input.GetKey(KeyCode.G))
        {
            for(int i = 0; i < scrollerObj.transform.childCount; i++)
            {
                for(int k = 0; k < scrollerObj.transform.GetChild(i).childCount; k++)
                {
                    Destroy(scrollerObj.transform.GetChild(i).GetChild(k).gameObject);
                }
            }
        }

        //Force start run without track
        if (Input.GetKey(KeyCode.LeftShift) && Input.GetKey(KeyCode.LeftAlt) && Input.GetKey(KeyCode.Space))
        {
            canRun = true;
            canEnd = false;
            StartCoroutine(StartRun());
        }
    }

    void FinishTrack()
    {
        isRunning = false;
        fireworksParent.SetActive(true);
        if (SceneManager.GetActiveScene().name != "TutorialScene")
        {
            audioSource.Stop();
            //cameraTrack.GetComponent<CPC_CameraPath>().StopPath();
            if (playerObj != null)
                playerObj.transform.parent.GetComponent<PathCreation.Examples.PathFollower>().enabled = false;
            endTrackScreen.SetActive(true);
            endTrackScreen.GetComponent<EndTrackScreenController>().clearedOrFailedTxt.GetComponent<TMP_Text>().text = "Track Cleared!";
            foreach (SelectorRunner selector in GameObject.FindObjectsOfType<SelectorRunner>())
                selector.SetRegularSprite();
            if (CrossSceneController.isCampaign && CrossSceneController.currentCampaignLevel != 3 && CrossSceneController.previousScene != "LevelSelect")
                endTrackScreen.GetComponent<EndTrackScreenController>().enterToExitTxt.text = "Press ENTER to Continue";
        }
    }

    IEnumerator LoadAudioFileStart(string path)
    {
        UnityWebRequest AudioFiles = null;
        string audioFileName = path.Substring(path.LastIndexOf('\\') + 1);
        audioFileName = audioFileName.Remove(audioFileName.Length - 4);
        AudioFiles = UnityWebRequestMultimedia.GetAudioClip(path, AudioType.WAV);

        if (AudioFiles != null)
        {
            yield return AudioFiles.SendWebRequest();
            if (AudioFiles.isNetworkError)
                Debug.Log(AudioFiles.error);
            else
            {
                AudioClip clip = DownloadHandlerAudioClip.GetContent(AudioFiles);
                clip.name = audioFileName;
                songs.Add(clip);
            }
        }
    }

    public void UpdateMusicVolume()
    {
        PlayerPrefs.SetFloat("MusicVolume", musicVolumeSlider.value);
        audioSource.volume = musicVolumeSlider.value;
    }

    public void UpdateSFXVolume()
    {
        PlayerPrefs.SetFloat("SFXVolume", sfxVolumeSlider.value);
        foreach (AudioSource source in sfxSources)
            source.volume = sfxVolumeSlider.value;
    }

    public void OpenOptionsMenu()
    {
        optionsMenu.SetActive(true);
        pauseMenu.SetActive(false);
    }

    public void CloseOptionsMenu()
    {
        optionsMenu.SetActive(false);
        pauseMenu.SetActive(true);
    }
}
