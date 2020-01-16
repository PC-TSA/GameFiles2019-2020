using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class EndTrackScreenController : MonoBehaviour
{
    public GameObject scoreTabParent;
    public GameObject detailsTabParent;
    public GameObject leaderboardTabParent;

    public string username;

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
    public LeaderboardController leaderboardController;

    //---------- Leaderboard Variables ----------
    public string leaderboardTable;
    public ScoreEntity currentScore;
    public ScoreEntity highScore;
    public List<ScoreEntity> leaderboard;

    private void OnEnable()
    {
        VarSetup();
        PopulateScoreTab();
        PopulateDetailsTab();
        PopulateLeaderboardsTab();
        GetComponent<Animator>().Play("CanvasGroupFadeIn");
    }

    void VarSetup()
    {
        leaderboard = new List<ScoreEntity>();
        username = PlayerPrefs.GetString("username");
        //leaderboardTable = rhythmRunner.currentRecording.songArtist + rhythmRunner.XMLRecordingName;
        leaderboardTable = "gabrieltm8Frame";
        currentScore = new ScoreEntity(username, rhythmRunner.score, rhythmRunner.accuracy, rhythmRunner.ranking);
    }

    public void SelectScoreTab(GameObject tabButton)
    {
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

    public async void PopulateLeaderboardsTab()
    {
        leaderboard.AddRange(await leaderboardController.GetLeaderboardTable(leaderboardTable));
        foreach(ScoreEntity e in leaderboard)
        {
            if(e.RowKey == username)
            {
                if (float.Parse(currentScore.score) > float.Parse(e.score)) //Only upload score if it is better than the current score saved online for this user
                {
                    leaderboardController.AddToLeaderboard(leaderboardTable, username, rhythmRunner.score, rhythmRunner.accuracy, rhythmRunner.ranking); //Upload score
                    leaderboard.Add(currentScore);
                    highScore = currentScore;
                    leaderboard.Remove(e);
                    break;
                }
                else
                    highScore = e;
            }
        }

        leaderboard.Sort(delegate (ScoreEntity e1, ScoreEntity e2) { return float.Parse(e1.score).CompareTo(float.Parse(e2.score)); }); //Sorts leaderboard in ascending order
        leaderboard.RemoveRange(0, leaderboard.Count - 10);
        foreach (ScoreEntity e in leaderboard)
            Debug.Log(e.RowKey + " | " + e.score + " | " + e.accuracy + " | " + e.rank);
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
