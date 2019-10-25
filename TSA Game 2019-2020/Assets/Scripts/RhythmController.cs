using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

//Main variable class
public class RhythmController : MonoBehaviour
{
    public int notesHit;
    public int notesMissed;
    public int missClicks;

    public GameObject notesHitTxt;
    public GameObject notesMissedTxt;
    public GameObject missClicksTxt;

    public ScrollerController scrollerController;
    public RhythmSliderController sliderController;

    public GameObject EditModeButton;
    public GameObject StartPauseButton;

    public List<KeyCode> laneKeycodes = new List<KeyCode>(); //index 0 = left lane, 1 = middle lane, 2 == right lane; values gotten by SelectorComponent.cs
    public List<KeyCode> manualGenKeycodes = new List<KeyCode>(); //^; alternate keys used for placing notes in manual gen mode

    public bool isPlaying;
    public int editMode; //0 = auto generated notes by bpm; 1 = when developer presses arrow keys

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
        GetComponent<AudioSource>().Play();
        sliderController.SetSlider();
        StartPauseButton.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = "Pause";
    }

    void PauseLevel()
    {
        GetComponent<AudioSource>().Pause();
        StartPauseButton.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = "Start";
    }

    void StartPause()
    {
        isPlaying = !isPlaying;
        if (isPlaying)
            StartLevel();
        else
            PauseLevel();
    }

    void AutoGenEditMode()
    {
        editMode = 0;
        EditModeButton.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = "Auto Gen";
        scrollerController.DrainTimer();
    }

    void ManualEditMode()
    {
        editMode = 1;
        EditModeButton.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = "Manual Gen";
    }

    public void EditModeSelect()
    {
        if (editMode == 0)
            ManualEditMode();
        else
            AutoGenEditMode();
    }
}
