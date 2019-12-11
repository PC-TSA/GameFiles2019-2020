using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class CrossSceneController 
{
    public static string recordingToLoad = "";

    public static void MakerToGame(string recordingPath) //Triggered from maker, sends current track to game
    {
        recordingToLoad = recordingPath;
        if (recordingToLoad.Substring(recordingToLoad.Length - 4) != ".xml")
            recordingToLoad += ".xml";
        SceneManager.LoadScene("Overworld");
    }

    public static void GameToMaker(string recordingName) //Triggered from game, sends current track to maker
    {
        recordingToLoad = recordingName;
        if (recordingToLoad.Substring(recordingToLoad.Length - 4) != ".xml")
            recordingToLoad += ".xml";
        SceneManager.LoadScene("RhythmMaker");
    }
}
