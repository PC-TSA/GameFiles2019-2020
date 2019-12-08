using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class CrossSceneController 
{
    public static string recordingToLoad = "";

    public static void MakerToGame(string recordingName) //Triggered from maker, sends current track to game
    {
        GameObject.FindObjectOfType<RhythmController>().SaveRecording();
        recordingToLoad = recordingName;
        SceneManager.LoadScene("TestTrack");
    }

    public static void GameToMaker(string recordingName) //Triggered from game, sends current track to maker
    {
        recordingToLoad = recordingName;
        SceneManager.LoadScene("RhythmMaker");
    }
}
