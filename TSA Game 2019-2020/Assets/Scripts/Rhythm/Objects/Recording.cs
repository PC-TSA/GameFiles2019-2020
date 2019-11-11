using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Recording
{
    public string clipName; //The name of the audio clip that these tamp stamps pertain to; NOT the audio clip itself
    public float scrollSpeed; //The speed at which this recording should scroll; If different from the speed at which it was recorded the notes wont fit the song's beat
    public int laneCount = 3; //How many lanes should be used to play this song
    public List<Note> notes = new List<Note>(); //List of notes in this recording; Each has a localPos to spawn the note in
    public List<SliderObj> sliders = new List<SliderObj>(); //List of sliders in this recording; Each has a localPos to spawn the slider in & a height to stretch it to
    public List<SpaceObj> spaces = new List<SpaceObj>(); //List of spaces in this recording; Each has a localPos to spawn the space in & a width to stretch it to

    public Recording() { }
}
