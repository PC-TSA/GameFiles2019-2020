using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WavefromVisualizer : MonoBehaviour
{
    int resolution = 60;

    float[] waveForm;
    float[] samples;

    // Use this for initialization
    void Start()
    {

        resolution = GetComponent<AudioSource>().clip.frequency / resolution;

        samples = new float[GetComponent<AudioSource>().clip.samples * GetComponent<AudioSource>().clip.channels];
        GetComponent<AudioSource>().clip.GetData(samples, 0);

        waveForm = new float[(samples.Length / resolution)];

        for (int i = 0; i < waveForm.Length; i++)
        {
            waveForm[i] = 0;

            for (int ii = 0; ii < resolution; ii++)
            {
                waveForm[i] += Mathf.Abs(samples[(i * resolution) + ii]);
            }

            waveForm[i] /= resolution;
        }
    }

    // Update is called once per frame
    void Update()
    {
        for (int i = 0; i < waveForm.Length - 1; i++)
        {
            Vector3 sv = new Vector3(i * .01f, waveForm[i] * 10, 0);
            Vector3 ev = new Vector3(i * .01f, -waveForm[i] * 10, 0);

            Debug.DrawLine(sv, ev, Color.yellow);
        }

        int current = GetComponent<AudioSource>().timeSamples / resolution;
        current *= 2;

        Vector3 c = new Vector3(current * .01f, 0, 0);

        Debug.DrawLine(c, c + Vector3.up * 10, Color.white);
    }
}