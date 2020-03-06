using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

public class CustomizeController : MonoBehaviour
{
    //Right tab UI
    public bool tabSelectorMoving;
    public GameObject tabSelector;
    public Vector3 tabSelectorGoalPos;
    public float tabSelectorSpeed;

    public GameObject splashTitlePrefab;
    public GameObject miscCanvasObj;

    public GameObject loadingBar;

    public AudioSource audioSource;

    public GameObject customizeTab;
    public GameObject storeTab;
    public GameObject tradeTab;

    // Update is called once per frame
    void Update()
    {
        if (tabSelectorMoving)
        {
            tabSelector.transform.localPosition = Vector3.Lerp(tabSelector.transform.localPosition, tabSelectorGoalPos, Time.deltaTime * tabSelectorSpeed);
            if (Vector3.Distance(tabSelector.transform.localPosition, tabSelectorGoalPos) < 0.001)
                tabSelectorMoving = false;
        }
    }

    public void SpawnSplashTitle(string titleText, Color titleColor)
    {
        GameObject newSplashTitle = Instantiate(splashTitlePrefab, miscCanvasObj.transform);
        newSplashTitle.GetComponent<TMP_Text>().text = titleText;
        newSplashTitle.GetComponent<TMP_Text>().color = titleColor;
        StartCoroutine(KillSplashTitle(newSplashTitle));
    }

    IEnumerator KillSplashTitle(GameObject title)
    {
        yield return new WaitForSeconds(title.GetComponent<Animation>().clip.length);
        Destroy(title);
    }

    IEnumerator LoadAsyncScene(string scene)
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(scene);
        asyncLoad.allowSceneActivation = false;
        StartCoroutine(StartBar(loadingBar));
        // Wait until the asynchronous scene fully loads
        while (!asyncLoad.isDone)
        {
            loadingBar.GetComponent<Slider>().value = asyncLoad.progress;
            if (scene == "MainMenu")
                CrossSceneController.mainThemeTime = audioSource.time;
            asyncLoad.allowSceneActivation = true;
            yield return null;
        }
    }

    IEnumerator StartBar(GameObject bar)
    {
        bar.SetActive(true);
        int periodIndex = 0;
        List<GameObject> loadingTextPeriods = new List<GameObject>();
        for (int i = 0; i < bar.transform.GetChild(0).childCount; i++) //Populate loading text periods from bar's children
            loadingTextPeriods.Add(bar.transform.GetChild(0).GetChild(i).gameObject);

        while (bar.activeSelf) //While the bar is active, animate loading text periods
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
    IEnumerator StopBar(GameObject bar, bool shouldFade)
    {
        if (shouldFade)
        {
            bar.GetComponent<Animator>().Play("CanvasGroupFadeOut");
            yield return new WaitForSeconds(1);
        }
        bar.SetActive(false);
        if (shouldFade)
            bar.GetComponent<CanvasGroup>().alpha = 1;
    }

    public void ExitCustomizeMenu()
    {
        StartCoroutine(LoadAsyncScene("MainMenu"));
    }

    public void TabSelector(GameObject tabObj)
    {
        tabSelectorGoalPos = new Vector3(tabObj.transform.localPosition.x, tabSelector.transform.localPosition.y, tabSelector.transform.localPosition.z);
        tabSelectorMoving = true;
    }

    public void CustomizeTab()
    {
        customizeTab.SetActive(true);
        storeTab.SetActive(false);
        tradeTab.SetActive(false);
    }

    public void StoreTab()
    {
        customizeTab.SetActive(false);
        storeTab.SetActive(true);
        tradeTab.SetActive(false);
    }

    public void TradeTab()
    {
        customizeTab.SetActive(false);
        storeTab.SetActive(false);
        tradeTab.SetActive(true);
    }
}
