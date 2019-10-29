using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioVisualizer : MonoBehaviour
{
    public GameObject cubePrefab;
    GameObject[] sampleCube = new GameObject[256];
    public float maxScale;
    public float scaleMultiplier;
    public float lerpSpeed;

    // Start is called before the first frame update
    void Start()
    {
        for (int i = 0; i < 256; i++) //256 samples = half of the samples, which I am wrapping into a circle
        {
            GameObject instanceCube = (GameObject)Instantiate(cubePrefab);
            instanceCube.transform.position = this.transform.position;
            instanceCube.transform.parent = this.transform;
            instanceCube.name = "SampleCube" + i;
            //this.transform.eulerAngles = new Vector3(0, -0.703125f * i, 0); //Use instead if using 512 samples
            this.transform.eulerAngles = new Vector3(0, -1.40625f * i, 0);
            instanceCube.transform.position = Vector3.forward * 100;
            sampleCube[i] = instanceCube;
        }
        transform.localScale = new Vector3(0.0043f, 0.01f, 0.0043f);
    }

    private void FixedUpdate()
    {
        for (int i = 0; i < 256; i++)
        {
            if (sampleCube != null)
            {
                float temp = (AudioPeer.samples[i] * maxScale) + 2;
                if (i <= 2)
                    temp *= 0.6f;
                if (temp <= 8)
                    temp *= 7f;
                if (temp >= 400)
                    temp *= 0.7f;
                if (temp >= 600)
                    temp *= 0.8f;
                temp *= scaleMultiplier;
                sampleCube[i].transform.localScale = new Vector3(75, Mathf.Lerp(sampleCube[i].transform.localScale.y, temp, lerpSpeed), 75);
            }
        }
    }
}
