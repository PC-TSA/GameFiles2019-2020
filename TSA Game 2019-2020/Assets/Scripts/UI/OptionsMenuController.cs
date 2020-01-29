using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OptionsMenuController : MonoBehaviour
{
    public Slider musicVolumeSlider;
    public Slider sfxVolumeSlider;
    public MainMenuController mainMenuController;

    private void Start()
    {
        musicVolumeSlider.value = PlayerPrefs.GetFloat("MusicVolume");
        sfxVolumeSlider.value = PlayerPrefs.GetFloat("SFXVolume");
    }

    public void UpdateMusicVolume()
    {
        PlayerPrefs.SetFloat("MusicVolume", musicVolumeSlider.value);
        mainMenuController.audioSource.volume = musicVolumeSlider.value;
    }

    public void UpdateSFXVolume()
    {
        PlayerPrefs.SetFloat("SFXVolume", sfxVolumeSlider.value);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            Disable();
    }

    void Disable()
    {
        GameObject.FindObjectOfType<MainMenuController>().optionsMenuActive = !GameObject.FindObjectOfType<MainMenuController>().optionsMenuActive;
        gameObject.SetActive(false);
    }
}
