using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using System.Xml;
using System.Xml.Serialization;
using System.IO;

public class RhythmRunner : MonoBehaviour
{
    public string XMLRecordingName; //This is the note track recording that will be used in this scene; Searches for an XML file with this exact name in the 'Assets/Resources/' folder
    public TextAsset xmlRecordingAsset;

    public int notesHit;
    public int notesMissed;
    public int missClicks;

    public GameObject notesHitTxt;
    public GameObject notesMissedTxt;
    public GameObject missClicksTxt;

    public List<AudioClip> songs;

    public GameObject scrollerObj;

    public List<KeyCode> laneKeycodes = new List<KeyCode>(); //index 0 = left lane, 1 = middle lane, 2 == right lane; values gotten by SelectorComponent.cs

    public Recording currentRecording;

    public GameObject notePrefab;
    public GameObject notesParent;

    public AudioSource audioSource;

    public float scrollSpeed;

    public Vector3 originalPos;

    private void Start()
    {
        //Load xml asset
        xmlRecordingAsset = Resources.Load<TextAsset>(XMLRecordingName);

        originalPos = scrollerObj.transform.position;

        StartCoroutine(DelayedStart());
    }

    private void FixedUpdate()
    {
        //Scroller
        scrollerObj.transform.position = new Vector3(scrollerObj.transform.position.x, scrollerObj.transform.position.y - scrollSpeed, scrollerObj.transform.position.z);
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

        //Reset scroller to start
        scrollerObj.transform.position = originalPos;

        //Generate notes
        foreach (Note n in currentRecording.notes)
            DeserializeNote(n.lane, n.pos);

        audioSource.Play();
    }

    public void DeserializeNote(int lane, Vector3 pos)
    {
        GameObject newNote = Instantiate(notePrefab, new Vector3(pos.x, 0, 0), transform.rotation, notesParent.transform);
        newNote.transform.localPosition = new Vector3(newNote.transform.localPosition.x, pos.y, pos.z);

        //Rotate Note
        switch (lane)
        {
            case 0:
                newNote.transform.eulerAngles = new Vector3(0, 0, -90);
                break;
            case 1:
                newNote.transform.eulerAngles = new Vector3(0, 0, 180);
                break;
            case 2:
                newNote.transform.eulerAngles = new Vector3(0, 0, 90);
                break;
        }
    }

    IEnumerator DelayedStart()
    {
        yield return new WaitForSeconds(1);
        LoadRecording();
    }
}
