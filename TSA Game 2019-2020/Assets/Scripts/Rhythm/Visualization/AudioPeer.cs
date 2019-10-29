using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioPeer : MonoBehaviour
{
    AudioSource audioSource;
    public static float[] samples = new float[512];
    // Start is called before the first frame update
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    // Update is called once per frame
    void Update()
    {
        GetSpectrum();
    }

    void GetSpectrum()
    {
        audioSource.GetSpectrumData(samples, 0, FFTWindow.Blackman);
    }
}
