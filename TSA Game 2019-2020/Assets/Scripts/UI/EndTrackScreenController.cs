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
    public bool isTestTrack; //If the current game being played is in the Rhythm Maker Test Track function; ENTER will return to rhythm maker instead of main menu

    //---------- Details Tab Variables ----------
    public TMP_Text notesHitTxt;
    public TMP_Text perfectHitsTxt;
    public TMP_Text goodHitsTxt;
    public TMP_Text okayHitsTxt;
    public TMP_Text badHitsTxt;
    public TMP_Text notesMiseedTxt;
    public TMP_Text missClicksTxt;
    public TMP_Text accuracyTxt;
    public TMP_Text maxComboTxt;

    //---------- Leaderboard Tab Variables ----------
    public GameObject leaderboardEntriesParent;
    public GameObject leaderboardEntryPrefab;
    public GameObject currentScoreEntry;
    public GameObject leaderboardUnavailableTxt;

    //---------- Leaderboard Variables ----------
    public string leaderboardTable;
    public ScoreEntity currentScore;
    public ScoreEntity highScore;
    public List<ScoreEntity> leaderboard;

    private void OnEnable()
    {
        Cursor.visible = true;
        VarSetup();
        PopulateScoreTab();
        PopulateDetailsTab();
        PopulateLeaderboardsTab();
        GetComponent<Animator>().Play("CanvasGroupFadeIn");
    }

    private void OnDisable()
    {
        Cursor.visible = false;
    }

    void VarSetup()
    {
        leaderboard = new List<ScoreEntity>();
        username = PlayerPrefs.GetString("username");
        leaderboardTable = rhythmRunner.currentRecording.trackArtist + rhythmRunner.XMLRecordingName;
        //leaderboardTable = "gabrieltm8Frame"; HARD CODED LEADERBOARD
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
        scoreTabParent.SetActive(false);
        detailsTabParent.SetActive(true);
        leaderboardTabParent.SetActive(false);

        tabHighlightGoalPos = new Vector3(tabButton.transform.localPosition.x, tabHighlightDivider.transform.localPosition.y, tabHighlightDivider.transform.localPosition.z);
        tabHighlightMoving = true;
    }

    public void SelectLeaderboardsTab(GameObject tabButton)
    {
        scoreTabParent.SetActive(false);
        detailsTabParent.SetActive(false);
        leaderboardTabParent.SetActive(true);

        tabHighlightGoalPos = new Vector3(tabButton.transform.localPosition.x, tabHighlightDivider.transform.localPosition.y, tabHighlightDivider.transform.localPosition.z);
        tabHighlightMoving = true;
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
        notesHitTxt.text = "" + rhythmRunner.notesHit;
        perfectHitsTxt.text = "" + rhythmRunner.perfectHits;
        goodHitsTxt.text = "" + rhythmRunner.goodHits;
        okayHitsTxt.text = "" + rhythmRunner.okayHits;
        badHitsTxt.text = "" + rhythmRunner.badHits;
        notesMiseedTxt.text = "" + rhythmRunner.notesMissed;
        missClicksTxt.text = "" + rhythmRunner.missClicks;
        accuracyTxt.text = "" + rhythmRunner.accuracy + "%";
        maxComboTxt.text = "" + rhythmRunner.maxCombo;
    }

    public async void PopulateLeaderboardsTab()
    {
        try
        {
            leaderboard.AddRange(await leaderboardController.GetLeaderboardTable(leaderboardTable));
            bool hasBeenUploaded = false;
            foreach (ScoreEntity e in leaderboard)
            {
                if (e.RowKey == username)
                {
                    if (float.Parse(currentScore.score) > float.Parse(e.score)) //Only upload score if it is better than the current score saved online for this user
                    {
                        leaderboardController.AddToLeaderboard(leaderboardTable, username, rhythmRunner.score, rhythmRunner.accuracy, rhythmRunner.ranking); //Upload score
                        leaderboard.Add(currentScore);
                        highScore = currentScore;
                        leaderboard.Remove(e);
                        hasBeenUploaded = true;
                        break;
                    }
                    else
                        highScore = e;
                }
            }
            if (!hasBeenUploaded)
            {
                leaderboardController.AddToLeaderboard(leaderboardTable, username, rhythmRunner.score, rhythmRunner.accuracy, rhythmRunner.ranking); //Upload score
                leaderboard.Add(currentScore);
                highScore = currentScore;
            }

            leaderboard.Sort(delegate (ScoreEntity e1, ScoreEntity e2) { return float.Parse(e1.score).CompareTo(float.Parse(e2.score)); }); //Sorts leaderboard in ascending order
            int currentScoreRank = leaderboard.Count - leaderboard.IndexOf(highScore);
            leaderboard.RemoveRange(0, leaderboard.Count - 10);
            leaderboard.Reverse();
            for (int i = 0; i < leaderboard.Count; i++)
            {
                ScoreEntity e = leaderboard[i];
                Debug.Log(e.RowKey + " | " + e.score + " | " + e.accuracy + " | " + e.rank);
                GameObject entry = Instantiate(leaderboardEntryPrefab, leaderboardEntriesParent.transform);
                entry.transform.localPosition = new Vector3(entry.transform.localPosition.x, entry.transform.localPosition.y - (25 * i), entry.transform.localPosition.z);
                entry.transform.GetChild(0).GetComponent<TMP_Text>().text = "" + (i + 1);
                entry.transform.GetChild(1).GetComponent<TMP_Text>().text = e.RowKey;
                entry.transform.GetChild(2).GetComponent<TMP_Text>().text = e.score;
            }
            currentScoreEntry.transform.GetChild(0).GetComponent<TMP_Text>().text = "" + currentScoreRank;
            currentScoreEntry.transform.GetChild(1).GetComponent<TMP_Text>().text = highScore.RowKey;
            currentScoreEntry.transform.GetChild(2).GetComponent<TMP_Text>().text = highScore.score;
        }
        catch
        {
            if(leaderboardEntriesParent.transform.childCount > 0)
                for(int i = 0; i < leaderboardEntriesParent.transform.childCount; i++)
                    Destroy(leaderboardEntriesParent.transform.GetChild(0));

            currentScoreEntry.SetActive(false);
            leaderboardUnavailableTxt.SetActive(true);
            Debug.LogError("Leaderboard loading failed");
            throw;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return)) //Enter to continue
        {
            if (isTestTrack)
                rhythmRunner.ToRhythmMaker();
            else if(CrossSceneController.isCampaign)
            {
                CrossSceneController.currentCampaignLevel++;
                StartCoroutine(CampaignSceneTransition("Campaign" + CrossSceneController.currentCampaignLevel + CrossSceneController.campaignDifficulty));
            }
            else
                StartCoroutine(rhythmRunner.LoadAsyncScene("MainMenu"));
        }

        if (Input.GetKeyDown(KeyCode.Space)) //Space to restart
            StartCoroutine(rhythmRunner.LoadAsyncScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name));

        if (tabHighlightMoving)
        {
            tabHighlightDivider.transform.localPosition = Vector3.Lerp(tabHighlightDivider.transform.localPosition, tabHighlightGoalPos, Time.deltaTime * tabHighlightMoveSpeed);
            if (Vector3.Distance(tabHighlightDivider.transform.localPosition, tabHighlightGoalPos) < 0.001)
                tabHighlightMoving = false;
        }
    }

    IEnumerator CampaignSceneTransition(string nextScene)
    {
        //PLAY TRANSITION ANIMATION
        yield return new WaitForSeconds(0.5f);
        StartCoroutine(rhythmRunner.LoadAsyncScene(nextScene));
    }
}
