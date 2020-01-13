using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class EndTrackScreenController : MonoBehaviour
{
    public GameObject scoreTabParent;
    public GameObject detailsTabParent;
    public GameObject leaderboardTabParent;

    // ---------- Score Tab Variables ----------
    public GameObject clearedOrFailedTxt;
        
    public TMP_Text finalScoreTxt;
    public TMP_Text rankingTxt;
    public TMP_Text songNameTxt;
    public TMP_Text songArtistTxt;
    public TMP_Text trackArtistTxt;
    public TMP_Text trackDifficultyTxt;

    public GameObject tabHighlightDivider;
    public bool tabHighlightMoving;
    public float tabHighlightMoveSpeed = 2;
    public Vector3 tabHighlightGoalPos;

    public RhythmRunner rhythmRunner;

    private void OnEnable()
    {
        PopulateScoreTab();
        PopulateDetailsTab();
        PopulateLeaderboardsTab();
        GetComponent<Animator>().Play("CanvasGroupFadeIn");
    }

    public void SelectScoreTab(GameObject tabButton)
    {
        PopulateScoreTab();

        scoreTabParent.SetActive(true);
        detailsTabParent.SetActive(false);
        leaderboardTabParent.SetActive(false);

        tabHighlightGoalPos = new Vector3(tabButton.transform.localPosition.x, tabHighlightDivider.transform.localPosition.y, tabHighlightDivider.transform.localPosition.z);
        tabHighlightMoving = true;
    }

    public void SelectDetailsTab(GameObject tabButton)
    {
        PopulateDetailsTab();

        scoreTabParent.SetActive(false);
        detailsTabParent.SetActive(true);
        leaderboardTabParent.SetActive(false);

        tabHighlightGoalPos = new Vector3(tabButton.transform.localPosition.x, tabHighlightDivider.transform.localPosition.y, tabHighlightDivider.transform.localPosition.z);
        tabHighlightMoving = true;
    }

    public void SelectLeaderboardsTab(GameObject tabButton)
    {
        PopulateLeaderboardsTab();

        scoreTabParent.SetActive(false);
        detailsTabParent.SetActive(false);
        leaderboardTabParent.SetActive(true);

        tabHighlightGoalPos = new Vector3(tabButton.transform.localPosition.x, tabHighlightDivider.transform.localPosition.y, tabHighlightDivider.transform.localPosition.z);
        tabHighlightMoving = true;

        PopulateLeaderboardsTab();
    }

    public void PopulateScoreTab()
    {
        Recording recording = rhythmRunner.currentRecording;
        Debug.Log(recording.clipName);
        finalScoreTxt.text = "" + rhythmRunner.score;
        rankingTxt.text = rhythmRunner.ranking;
        songNameTxt.text = recording.songName;
        songArtistTxt.text = recording.songArtist;
        trackArtistTxt.text = recording.trackArtist;
        trackDifficultyTxt.text = recording.trackDifficulty;
    }

    public void PopulateDetailsTab()
    {

    }

    public void PopulateLeaderboardsTab()
    {

    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return)) //Enter to exit
            StartCoroutine(rhythmRunner.LoadAsyncScene("MainMenu"));

        if (Input.GetKeyDown(KeyCode.Space)) //Space to restart
            StartCoroutine(rhythmRunner.LoadAsyncScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name));

        if (tabHighlightMoving)
        {
            tabHighlightDivider.transform.localPosition = Vector3.Lerp(tabHighlightDivider.transform.localPosition, tabHighlightGoalPos, Time.deltaTime * tabHighlightMoveSpeed);
            if (Vector3.Distance(tabHighlightDivider.transform.localPosition, tabHighlightGoalPos) < 0.001)
                tabHighlightMoving = false;
        }
    }
}
