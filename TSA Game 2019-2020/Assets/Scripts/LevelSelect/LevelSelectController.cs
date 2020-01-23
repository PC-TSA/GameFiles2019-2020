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
    public string workshopPath;

    public List<DownloadedTrack> downloadedTracks;

    public GameObject trackItemsParent;
    public GameObject trackItemPrefab;

    public List<string> builtInSongs;

    public GameObject loadingBar;

    // Start is called before the first frame update
    void Start()
    {
        System.IO.Directory.CreateDirectory(Application.persistentDataPath + "\\" + "DownloadedTracks");
        workshopPath = Application.persistentDataPath + "\\" + "Workshop";

        //Read all audioclips in the Resources/Songs folder and add them to the 'builtInSongs' list
        UnityEngine.Object[] temp = Resources.LoadAll("Songs", typeof(AudioClip));
        foreach (UnityEngine.Object o in temp)
            builtInSongs.Add(o.name);

        GetDownloadedTracks();
        PopulateLevelSelect();
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

    void PopulateLevelSelect()
    {
        foreach(DownloadedTrack track in downloadedTracks)
        {
            GameObject trackItem = Instantiate(trackItemPrefab, trackItemsParent.transform);
            Sprite cover = GetCover(track);
            trackItem.GetComponent<LevelSelectItemController>().InitializeItem(cover, track.songName, track.songArtist, track.trackArtist, track.difficulty, track.xmlName, track.mp3Name, track.id);
        }
    }

    Sprite GetCover(DownloadedTrack downloadedTrack)
    {
        byte[] byteArr = File.ReadAllBytes(downloadedTrack.downloadedPath + "\\cover.jpg");
        Texture2D tex2d = new Texture2D(2, 2); //Create new "empty" texture
        Sprite cover = null;
        if (tex2d.LoadImage(byteArr)) //Load the imagedata into the texture (size is set automatically)
            cover = Sprite.Create(tex2d, new Rect(0, 0, tex2d.width, tex2d.height), new Vector2(0, 0), 100, 0, SpriteMeshType.Tight);

        return cover;
    }

    public void PlayTrack(LevelSelectItemController track)
    {
        StartCoroutine(PlayTrackEnum(track));
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

        CrossSceneController.SceneToGame(path + track.xmlName, clip);
        StartCoroutine(LoadAsyncScene("Overworld"));
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
        StartCoroutine(StartBar(loadingBar));
        // Wait until the asynchronous scene fully loads
        while (!asyncLoad.isDone)
        {
            loadingBar.GetComponent<Slider>().value = asyncLoad.progress;
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
}
