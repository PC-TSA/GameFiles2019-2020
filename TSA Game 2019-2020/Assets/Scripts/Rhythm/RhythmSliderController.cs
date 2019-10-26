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

    private void Update()
    {
        if (sliderInUse)
            UpdateVals();
        else
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
        audioSource.time = GetComponent<Slider>().value;
        scroller.transform.position = new Vector3(scroller.transform.position.x, -((audioSource.time) * 50 * scrollerController.scrollSpeed) + 530, 0); //*50 because FixedUpdate runs 50 times a second and +530 because it (for some reason) was off by around that much
    }

    public void OnSliderSelected()
    {
        sliderInUse = true;
        //audioSource.Pause();
    }

    public void OnSliderDeSelected()
    {
        sliderInUse = false;
        //audioSource.UnPause();
    }
}
