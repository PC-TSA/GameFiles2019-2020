using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using UnityEngine.Networking;
using NAudio;
using NAudio.Wave;
using UnityEngine.SceneManagement;

public class LevelSelectController : MonoBehaviour
{
    public AudioSource audioSource;

    public string workshopPath;

    public List<DownloadedTrack> downloadedTracks;

    public GameObject trackItemPrefab;

    public List<string> builtInSongs;

    public GameObject loadingBar;

    //Right tab UI
    public bool tabSelectorMoving;
    public GameObject tabSelector;
    public Vector3 tabSelectorGoalPos;
    public float tabSelectorSpeed;

    //Campaign Tab
    public GameObject campaignTab;
    public GameObject difficultyPrompt;
    public TMP_Text promptTrackTxt;
    public TMP_Text promptSegmentTxt;
    public string difficulty;

    public bool difficultySelectorMoving;
    public GameObject difficultySelector;
    public Vector3 difficultySelectorGoalPos;
    public float difficultySelectorSpeed;

    public int campaignBeingSelected;
    public List<List<TextAsset>> campaignLevels; //List of lists; Each level is a list with 3 text assets, easy, medium, hard

    //Downloaded Tracks Tab
    public GameObject downloadedTracksTab;
    public GameObject downloadedTracksContentParent;

    //Bonus Tracks Tab
    public GameObject bonusTracksTab;

    void Start()
    {
        System.IO.Directory.CreateDirectory(Application.persistentDataPath + "\\" + "DownloadedTracks");
        workshopPath = Application.persistentDataPath + "\\" + "Workshop";

        //Read all audioclips in the Resources/Songs folder and add them to the 'builtInSongs' list
        UnityEngine.Object[] temp = Resources.LoadAll("Songs", typeof(AudioClip));
        foreach (UnityEngine.Object o in temp)
            builtInSongs.Add(o.name);

        if (CrossSceneController.mainThemeTime != 0)
            audioSource.time = CrossSceneController.mainThemeTime;

        Cursor.visible = true;

        GetDownloadedTracks();
        PopulateDownloadedTracksTab();
    }

    private void Update()
    {
        if (tabSelectorMoving)
        {
            tabSelector.transform.localPosition = Vector3.Lerp(tabSelector.transform.localPosition, tabSelectorGoalPos, Time.deltaTime * tabSelectorSpeed);
            if (Vector3.Distance(tabSelector.transform.localPosition, tabSelectorGoalPos) < 0.001)
                tabSelectorMoving = false;
        }

        if (difficultySelectorMoving)
        {
            difficultySelector.transform.localPosition = Vector3.Lerp(difficultySelector.transform.localPosition, difficultySelectorGoalPos, Time.deltaTime * difficultySelectorSpeed);
            if (Vector3.Distance(difficultySelector.transform.localPosition, difficultySelectorGoalPos) < 0.001)
                difficultySelectorMoving = false;
        }

        if (Input.GetKeyDown(KeyCode.Escape) && difficultyPrompt.activeSelf)
            CloseDifficultyPrompt();
    }

    public void TabSelector(GameObject tabObj)
    {
        tabSelectorGoalPos = new Vector3(tabSelector.transform.localPosition.x, tabObj.transform.localPosition.y, tabSelector.transform.localPosition.z);
        tabSelectorMoving = true;
    }

    public void CampaignTab()
    {
        campaignTab.SetActive(true);
        downloadedTracksTab.SetActive(false);
        bonusTracksTab.SetActive(false);
    }

    public void DownloadedTracksTab()
    {
        campaignTab.SetActive(false);
        downloadedTracksTab.SetActive(true);
        bonusTracksTab.SetActive(false);
    }

    public void BonusTracksTab()
    {
        campaignTab.SetActive(false);
        downloadedTracksTab.SetActive(false);
        bonusTracksTab.SetActive(true);
    }

    void GetDownloadedTracks()
    {
        downloadedTracks = new List<DownloadedTrack>();
        string[] files = System.IO.Directory.GetFiles(Application.persistentDataPath + "\\" + "DownloadedTracks");
        var serializer = new XmlSerializer(typeof(DownloadedTrack));

        foreach (string path in files)
        {
            if (path.Substring(path.Length - 4) != ".xml")
            {
                Debug.LogWarning("Non-XML File Found in DownloadedTracks");
                return;
            }

            var stream = new FileStream(path, FileMode.Open);
            DownloadedTrack downloadedTrack = serializer.Deserialize(stream) as DownloadedTrack;
            downloadedTrack.downloadedPath = workshopPath + "\\" + downloadedTrack.trackArtist + "\\" + downloadedTrack.songName;
            stream.Close();
            downloadedTracks.Add(downloadedTrack);
        }
    }

    void PopulateDownloadedTracksTab()
    {
        foreach(DownloadedTrack track in downloadedTracks)
        {
            GameObject trackItem = Instantiate(trackItemPrefab, downloadedTracksContentParent.transform);
            Sprite cover = GetCover(track);
            trackItem.GetComponent<LevelSelectItemController>().InitializeItem(cover, track.songName, track.songArtist, track.trackArtist, track.difficulty, track.xmlName, track.mp3Name, track.id);
        }
    }

    Sprite GetCover(DownloadedTrack downloadedTrack)
    {
        Sprite cover = null;
        if (File.Exists(downloadedTrack.downloadedPath + "\\cover.jpg"))
        {
            byte[] byteArr = File.ReadAllBytes(downloadedTrack.downloadedPath + "\\cover.jpg");
            Texture2D tex2d = new Texture2D(2, 2); //Create new "empty" texture
            if (tex2d.LoadImage(byteArr)) //Load the imagedata into the texture (size is set automatically)
                cover = Sprite.Create(tex2d, new Rect(0, 0, tex2d.width, tex2d.height), new Vector2(0, 0), 100, 0, SpriteMeshType.Tight);
        }
        return cover;
    }

    public void PlayTrack(LevelSelectItemController track)
    {
        StartCoroutine(PlayTrackEnum(track));
    }

    public void DeleteTrack(LevelSelectItemController track, GameObject item)
    {
        File.Delete(Application.persistentDataPath + "\\DownloadedTracks\\" + track.xmlName);
        foreach (DownloadedTrack downloadedTrack in downloadedTracks)
        {
            if (downloadedTrack.xmlName == track.xmlName)
            {
                downloadedTracks.Remove(downloadedTrack);
                break;
            }
        }
        DirectoryInfo dir = new DirectoryInfo(workshopPath + "\\" + track.trackArtist + "\\" + track.songName);
        dir.Delete(true);
        if (Directory.GetDirectories(workshopPath + "\\" + track.trackArtist).Length == 0) //If the no other tracks remain by this track artist, delete the track artist's folder too
            Directory.Delete(workshopPath + "\\" + track.trackArtist);
        Destroy(item); 
    }

    IEnumerator PlayTrackEnum(LevelSelectItemController track)
    {
        // ---------- Get Clip ----------
        string path = Application.persistentDataPath + "\\Workshop\\" + track.trackArtist + "\\" + track.songName + "\\";
        track.songName = track.mp3Name;
        AudioClip clip = null;

        if (builtInSongs.Contains(track.mp3Name))
        {
            Debug.Log("Loading " + track.xmlName + "'s song from built in resources..."); 
            clip = Resources.Load("Songs/" + track.mp3Name) as AudioClip;
        }
        else if (File.Exists(path + track.mp3Name + ".mp3"))
        {
            Debug.Log("Loading " + track.xmlName + "'s song from " + track.mp3Name + ".mp3...");
            UnityWebRequest AudioFiles = null;

            Mp3ToWav(path + track.mp3Name + ".mp3", path + "temp.wav");

            AudioFiles = UnityWebRequestMultimedia.GetAudioClip(path + "temp.wav", AudioType.WAV);
            if (AudioFiles != null)
            {
                yield return AudioFiles.SendWebRequest();
                if (AudioFiles.isNetworkError)
                    Debug.Log(AudioFiles.error);
                else
                {
                    clip = DownloadHandlerAudioClip.GetContent(AudioFiles);
                    clip.name = track.songName;
                }
            }
            File.Delete(path + "temp.wav");
        }
        else
        {
            Debug.LogError("Couldn't load song for xml " + track.xmlName);
            StopCoroutine(PlayTrackEnum(track));
        }

        CrossSceneController.SceneToGame(path + track.xmlName, clip, track.id);
        StartCoroutine(LoadAsyncScene("PlayTrackScene"));
    }

    public void Mp3ToWav(string mp3File, string outputFile)
    {
        using (Mp3FileReader reader = new Mp3FileReader(mp3File))
        {
            WaveFileWriter.CreateWaveFile(outputFile, reader);
        }
    }

    IEnumerator LoadAsyncScene(string scene)
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(scene);
        asyncLoad.allowSceneActivation = false;
        StartCoroutine(StartBar(loadingBar));
        // Wait until the asynchronous scene fully loads
        while (!asyncLoad.isDone)
        {
            loadingBar.GetComponent<Slider>().value = asyncLoad.progress;
            if (scene == "MainMenu")
                CrossSceneController.mainThemeTime = audioSource.time;
            asyncLoad.allowSceneActivation = true;
            yield return null;
        }
    }

    IEnumerator StartBar(GameObject bar)
    {
        bar.SetActive(true);
        int periodIndex = 0;
        List<GameObject> loadingTextPeriods = new List<GameObject>();
        for (int i = 0; i < bar.transform.GetChild(0).childCount; i++) //Populate loading text periods from bar's children
            loadingTextPeriods.Add(bar.transform.GetChild(0).GetChild(i).gameObject);

        while (bar.activeSelf) //While the bar is active, animate loading text periods
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
    IEnumerator StopBar(GameObject bar, bool shouldFade)
    {
        if (shouldFade)
        {
            bar.GetComponent<Animator>().Play("CanvasGroupFadeOut");
            yield return new WaitForSeconds(1);
        }
        bar.SetActive(false);
        if (shouldFade)
            bar.GetComponent<CanvasGroup>().alpha = 1;
    }

    public void ExitLevelSelect()
    {
        StartCoroutine(LoadAsyncScene("MainMenu"));
    }

    public void CampaignDifficultyPrompt(int track)
    {
        difficultyPrompt.SetActive(true);
        switch(track)
        {
            case 1:
                promptTrackTxt.text = "Tsunami";
                promptSegmentTxt.text = "Segment 1 - Swim";
                break;
            case 2:
                promptTrackTxt.text = "Chain Drive";
                promptSegmentTxt.text = "Segment 1 - Bike";
                break;
            case 3:
                promptTrackTxt.text = "Neon Lights";
                promptSegmentTxt.text = "Segment 1 - Run";
                break;
        }
        difficulty = "Medium";
        campaignBeingSelected = track;
    }

    public void CloseDifficultyPrompt()
    {
        difficultyPrompt.SetActive(false);
    }

    public void DifficultySelector(GameObject difficultyObj)
    {
        difficultySelectorGoalPos = new Vector3(difficultyObj.transform.localPosition.x, difficultySelector.transform.localPosition.y, difficultySelector.transform.localPosition.z);
        difficultySelectorMoving = true;
        difficulty = difficultyObj.GetComponent<TMP_Text>().text;
    }

    public void PlayCampaignTrack() //Ex: Campaign2Hard = the second campaign level in hard difficulty; There will be equivalently named XML files in resources to be loaded
    {
        CrossSceneController.recordingToLoad = "Campaign" + campaignBeingSelected + difficulty;
        switch (campaignBeingSelected)
        {
            case 1:
                StartCoroutine(LoadAsyncScene("OverworldDay"));
                break;
            case 2:
                StartCoroutine(LoadAsyncScene("OverworldSunset"));
                break;
            case 3:
                StartCoroutine(LoadAsyncScene("OverworldNight"));
                break;
        }
    }
}
