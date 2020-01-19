using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RhythmSliderController : MonoBehaviour
{
    public GameObject scroller;
    public AudioSource audioSource;
    public ScrollerController scrollerController;
    public bool sliderInUse;
    public RhythmController rhythmController;
    
    private void Update()
    {
        if (sliderInUse)
            UpdateVals();
        else if(rhythmController.isPlaying)
            UpdateSlider();
    }

    public void SetSlider()
    {
        GetComponent<Slider>().maxValue = audioSource.clip.length;
    }

    public void UpdateSlider()
    {
        GetComponent<Slider>().value = audioSource.time;
    }

    public void UpdateVals()
    {
        scroller.transform.localPosition = new Vector3(scrollerController.originalPos.x, -((GetComponent<Slider>().value) * 50 * scrollerController.scrollSpeed), scrollerController.originalPos.z); //*50 because FixedUpdate runs 50 times a second and +530 because it (for some reason) was off by around that much
        audioSource.time = GetComponent<Slider>().value;
    }

    public void OnSliderSelected()
    {
        sliderInUse = true;
        scrollerController.slideScrollOverride = true;
        //audioSource.Pause();
    }

    public void OnSliderDeSelected()
    {
        sliderInUse = false;
        scrollerController.slideScrollOverride = false;
        //audioSource.UnPause();
    }
}
