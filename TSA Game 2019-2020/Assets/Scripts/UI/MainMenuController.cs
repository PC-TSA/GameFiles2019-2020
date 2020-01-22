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

    public GameObject optionsMenu;
    public bool optionsMenuActive;

    private void Start()
    {
        Cursor.visible = true;
        if (PlayerPrefs.GetInt("FirstRun") == 0)
        {
            Debug.Log("First run player prefs setup");
            PlayerPrefs.SetInt("FirstRun", 1);
            PlayerPrefs.SetFloat("MusicVolume", 1);
            PlayerPrefs.SetFloat("SFXVolume", 1);
        }

        usernameInput.GetComponent<TMP_InputField>().text = PlayerPrefs.GetString("username");
    }

    public void GoToMainGame()
    {
        StartCoroutine(LoadAsyncScene("Overworld"));
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
                mainMenuCanvas.GetComponent<PlayableDirector>().Play(mainMenuClose);
                StartCoroutine(MainMenuAnimWait(asyncLoad));
            }

            yield return null;
        }
    }

    IEnumerator MainMenuAnimWait(AsyncOperation op)
    {
        yield return new WaitForSeconds(1.5f);
        op.allowSceneActivation = true;
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
}
