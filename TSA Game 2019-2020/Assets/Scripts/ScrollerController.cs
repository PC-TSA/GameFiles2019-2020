using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScrollerController : MonoBehaviour
{
    public GameObject notePrefab;

    public GameObject[] lanes;

    public float scrollSpeed;

    public float bpm;
    public float delay;

    private float lastTime, deltaTime, timer;

    /*void Start()
    {
        //Select the instance of AudioProcessor and pass a reference
        //to this object
        AudioProcessor processor = FindObjectOfType<AudioProcessor>();
        processor.onBeat.AddListener(onOnbeatDetected);
        processor.onSpectrum.AddListener(onSpectrum);
    }

    //this event will be called every time a beat is detected.
    //Change the threshold parameter in the inspector
    //to adjust the sensitivity
    void onOnbeatDetected()
    {
        Debug.Log("Beat!!!");
        Instantiate(notePrefab, noteSpawnPos.transform.position, transform.rotation, transform);
    }

    //This event will be called every frame while music is playing
    void onSpectrum(float[] spectrum)
    {
        //The spectrum is logarithmically averaged
        //to 12 bands

        for (int i = 0; i < spectrum.Length; ++i)
        {
            Vector3 start = new Vector3(i, 0, 0);
            Vector3 end = new Vector3(i, spectrum[i], 0);
            Debug.DrawLine(start, end);
        }
    }*/

    private void Update()
    {
        //Chooses a random lane to create the note in
        int rand = Random.Range(0, 4);
        deltaTime = GetComponent<AudioSource>().time - lastTime;
        timer += deltaTime;

        if (timer >= (delay / bpm))
        {
            //Create the note
            Instantiate(notePrefab, lanes[Random.Range(0, lanes.Length)].transform.position, transform.rotation, transform);
            timer = 0;
        }

        lastTime = GetComponent<AudioSource>().time;
    }

    private void FixedUpdate()
    {
        //Scroller
        transform.position = new Vector3(transform.position.x, transform.position.y - scrollSpeed, transform.position.z);

    }
}
