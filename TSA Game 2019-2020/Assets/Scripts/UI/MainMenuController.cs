using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Playables;
using TMPro;

public class MainMenuController : MonoBehaviour
{ 
    public GameObject loadingBar;
    public List<GameObject> loadingTextPeriods;

    public GameObject mainMenuCanvas;
    public GameObject usernameInput;

    public PlayableAsset mainMenuClose; //The timeline animation to slide the main menu back into the bar
    public PlayableAsset campaignDifficultiesOpen;
    public PlayableAsset campaignDifficultiesClose;
    public PlayableAsset campaignDifficultiesCloseToMain;

    public GameObject buttonMenu;
    public GameObject campaignDifficultyMenu;

    public GameObject optionsMenu;
    public bool optionsMenuActive;

    public GameObject howToPlayMenu;
    public bool howToPlayActive;

    private void Awake()
    {
        CrossSceneController.isCampaign = false;
        CrossSceneController.recordingToLoad = "";

        Cursor.visible = true;
        if (PlayerPrefs.GetInt("FirstRun") == 0)
        {
            Debug.Log("First run player prefs setup");
            PlayerPrefs.SetInt("FirstRun", 1);
            PlayerPrefs.SetFloat("MusicVolume", 1);
            PlayerPrefs.SetFloat("SFXVolume", 1);
            PlayerPrefs.SetString("username", "Player");
        }

        usernameInput.GetComponent<TMP_InputField>().text = PlayerPrefs.GetString("username");
    }

    public void GoToCampaign()
    {
        mainMenuCanvas.GetComponent<PlayableDirector>().Play(campaignDifficultiesOpen);
        StartCoroutine(CampaignDifficultiesMenuOpen());
    }

    public void GoToLevelSelect()
    {
        StartCoroutine(LoadAsyncScene("LevelSelect"));
    }

    public void GoToMaker()
    {
        StartCoroutine(LoadAsyncScene("RhythmMaker"));
    }

    public void GoToWorkshop()
    {
        StartCoroutine(LoadAsyncScene("Workshop"));
    }

    public void HowToPlay()
    {
        howToPlayActive = !howToPlayActive;
        if (howToPlayActive)
            howToPlayMenu.SetActive(true);
        else
            howToPlayMenu.SetActive(false);
    }

    public void PlayTutorial()
    {
        StartCoroutine(LoadAsyncScene("TutorialScene"));
    }

    public void ToggleOptionsMenu()
    {
        optionsMenuActive = !optionsMenuActive;
        if(optionsMenuActive)
            optionsMenu.SetActive(true);
        else
            optionsMenu.SetActive(false);
    }

    public void Quit()
    {
        Application.Quit();
    }

    IEnumerator LoadAsyncScene(string scene)
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(scene);
        asyncLoad.allowSceneActivation = false;
        StartCoroutine(LoadingBar());
        // Wait until the asynchronous scene fully loads
            bool isPlayingAnimOut = false;
        while (!asyncLoad.isDone)
        {
            loadingBar.GetComponent<Slider>().value = asyncLoad.progress;

            if(asyncLoad.progress >= 0.9f && !isPlayingAnimOut)
            {
                isPlayingAnimOut = true;
                if(campaignDifficultyMenu.activeSelf)
                    mainMenuCanvas.GetComponent<PlayableDirector>().Play(campaignDifficultiesClose);
                else
                    mainMenuCanvas.GetComponent<PlayableDirector>().Play(mainMenuClose);
                StartCoroutine(MainMenuAnimWait(asyncLoad));
            }
            yield return null;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (howToPlayActive)
            {
                howToPlayMenu.SetActive(false);
                howToPlayActive = false;
            }
        }
    }

    IEnumerator MainMenuAnimWait(AsyncOperation op)
    {
        yield return new WaitForSeconds(1.5f);
        op.allowSceneActivation = true;
    }

    IEnumerator CampaignDifficultiesMenuOpen()
    {
        yield return new WaitForSeconds(1.2f);
        campaignDifficultyMenu.SetActive(true);
        buttonMenu.SetActive(false);
    }

    IEnumerator CampaignDifficultiesMenuClose()
    {
        yield return new WaitForSeconds(1f);
        campaignDifficultyMenu.SetActive(false);
        buttonMenu.SetActive(true);
    }

    IEnumerator LoadingBar()
    {
        loadingBar.SetActive(true);
        int periodIndex = 0;
        while (true)
        {
            for (int i = 0; i < loadingTextPeriods.Count; i++)
                if (i == periodIndex)
                    loadingTextPeriods[i].SetActive(true);

           if (periodIndex == loadingTextPeriods.Count)
            {
                periodIndex = 0;
                foreach (GameObject obj in loadingTextPeriods)
                    obj.SetActive(false);
            }
            else
                periodIndex++;

            yield return new WaitForSeconds(0.5f);
        }
    }

    public void SetUsername()
    {
        string temp = usernameInput.GetComponent<TMP_InputField>().text;
        if (temp != "")
        {
            PlayerPrefs.SetString("username", usernameInput.GetComponent<TMP_InputField>().text);
            Debug.Log("Player prefs username updated: " + PlayerPrefs.GetString("username"));
        }
    }

    public void CampaignLoader(string difficulty)
    {
        CrossSceneController.recordingToLoad = "Campaign1" + difficulty;
        CrossSceneController.isCampaign = true;
        CrossSceneController.currentCampaignLevel = 1;
        CrossSceneController.campaignDifficulty = difficulty;
        StartCoroutine(LoadAsyncScene("OverworldDay"));
    }

    public void BackFromCampaignDifficulties()
    {
        mainMenuCanvas.GetComponent<PlayableDirector>().Play(campaignDifficultiesCloseToMain);
        StartCoroutine(CampaignDifficultiesMenuClose());
    }
}
