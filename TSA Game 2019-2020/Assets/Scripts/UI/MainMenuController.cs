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

    public PlayableAsset mainMenuClose; //The timeline animation to slide the main menu back into the bar

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

    public void GoToOptions()
    {
        StartCoroutine(LoadAsyncScene("Options"));
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

    public void SetUsername(GameObject obj)
    {
        CrossSceneController.username = obj.GetComponent<TMP_Text>().text;
    }
}
