using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScrollerController : MonoBehaviour
{
    public GameObject notePrefab;
    public GameObject notesParent;

    public GameObject[] lanes;

    public float scrollSpeed;

    public float bpm;
    public float delay;

    public float lastTime, deltaTime, timer;

    private void FixedUpdate()
    {
        //Scroller
        transform.position = new Vector3(transform.position.x, transform.position.y - scrollSpeed, transform.position.z);

        //Prevents first few note spawns from spawning multiple at once
        if (GetComponent<AudioSource>().time > 1.5f)
        {
            deltaTime = GetComponent<AudioSource>().time - lastTime;
            timer += deltaTime;
        }
        else
            lastTime = GetComponent<AudioSource>().time;

        if (timer >= (delay / bpm))
        {
            //Chooses a random lane to create the note in
            int rand = Random.Range(0, 4);

            //Create the note
            int lane = Random.Range(0, lanes.Length);
            GameObject newNote = Instantiate(notePrefab, lanes[lane].transform.position, transform.rotation, notesParent.transform);

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

            timer -= (delay / bpm);
        }

        lastTime = GetComponent<AudioSource>().time;
    }
}
