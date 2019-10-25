using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScrollerController : MonoBehaviour
{
    public GameObject notePrefab;
    public GameObject notesParent;

    public RhythmController rhythmController;

    public GameObject[] lanes;
    public GameObject[] selectors;

    public AudioSource audioSource;

    public float scrollSpeed;

    public float bpm;
    public float delay;

    public float lastTime, deltaTime, timer;

    public int timeGenerated; //How far into the song has been generated; if the song is rewound (through the slider), this will stop note generation where it has already been made & keep the timer synced

    private void FixedUpdate()
    { 

        //If the level has started
        if(FindObjectOfType<RhythmController>().isPlaying)
        {
            //Scroller
            transform.position = new Vector3(transform.position.x, transform.position.y - scrollSpeed, transform.position.z);

            //Prevents first few note spawns from spawning multiple at once
            if (audioSource.time > 2f)
            {
                deltaTime = audioSource.time - lastTime;
                timer += deltaTime;
            }
            else
                lastTime = audioSource.time;

            if (rhythmController.editMode == 0) //If auto gen is on (instead of manual placement)
            {
                if (timer >= (delay / bpm))
                {
                    //Create the note in a random lane
                    SpawnNote(Random.Range(0, lanes.Length), false);
                    timer -= (delay / bpm);
                }
            }
       
            lastTime = audioSource.time;
        }
    }
     
    public void DrainTimer() //Used to bring the timer back to near 0 (but keep it's beat placement) when re-enabling auto gen mode
    {
        while(timer - (delay / bpm) > 0)
            timer -= (delay / bpm);
    }

    public void SpawnNote(int lane, bool manualDevMode)
    {
        GameObject newNote = null;

        if(manualDevMode)
            newNote = Instantiate(notePrefab, selectors[lane].transform.position, transform.rotation, notesParent.transform);
        else
            newNote = Instantiate(notePrefab, lanes[lane].transform.position, transform.rotation, notesParent.transform);

        //Rotate Arrow
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
}
