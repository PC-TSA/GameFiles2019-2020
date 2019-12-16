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
        GetComponent<Animator>().Play("CanvasGroupFadeIn");
    }

    public void SelectScoreTab(GameObject tabButton)
    {
        PopulateScoreTab();

        scoreTabParent.SetActive(true);
        detailsTabParent.SetActive(false);
        leaderboardTabParent.SetActive(false);

        tabHighlightGoalPos = new Vector3(tabButton.transform.position.x, tabHighlightDivider.transform.position.y, tabButton.transform.position.z);
        tabHighlightMoving = true;
    }

    public void SelectDetailsTab(GameObject tabButton)
    {
        PopulateDetailsTab();

        scoreTabParent.SetActive(false);
        detailsTabParent.SetActive(true);
        leaderboardTabParent.SetActive(false);

        tabHighlightGoalPos = new Vector3(tabButton.transform.position.x, tabHighlightDivider.transform.position.y, tabButton.transform.position.z);
        tabHighlightMoving = true;
    }

    public void SelectLeaderboardsTab(GameObject tabButton)
    {
        PopulateLeaderboardsTab();

        scoreTabParent.SetActive(false);
        detailsTabParent.SetActive(false);
        leaderboardTabParent.SetActive(true);

        tabHighlightGoalPos = new Vector3(tabButton.transform.position.x, tabHighlightDivider.transform.position.y, tabButton.transform.position.z);
        tabHighlightMoving = true;

        PopulateLeaderboardsTab();
    }

    public void PopulateScoreTab()
    {
        Recording recording = rhythmRunner.currentRecording;
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
        if (Input.GetKeyDown(KeyCode.Return)) //Enter to continue
            StartCoroutine(rhythmRunner.LoadAsyncScene("MainMenu"));

        if (tabHighlightMoving)
        {
            tabHighlightDivider.transform.position = Vector3.Lerp(tabHighlightDivider.transform.position, tabHighlightGoalPos, Time.deltaTime * tabHighlightMoveSpeed);
            if (Vector3.Distance(tabHighlightDivider.transform.position, tabHighlightGoalPos) < 0.001)
                tabHighlightMoving = false;
        }
    }
}
