using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Recording
{
    public string clipName; //The name of the audio clip that these tamp stamps pertain to; NOT the audio clip itself
    public float scrollSpeed; //The speed at which this recording should scroll; If different from the speed at which it was recorded the notes wont fit the song's beat
    public List<Note> notes = new List<Note>(); //List of notes in this recording; Each has a time stamp of when to spawn and a lane to spawn in
    public List<SliderObj> sliders = new List<SliderObj>();

    public Recording()
    {
            
    }
}
