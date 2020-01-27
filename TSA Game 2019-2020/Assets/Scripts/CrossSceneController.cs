using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class CrossSceneController 
{
    public static string previousScene;
    public static string recordingToLoad = "";
    public static AudioClip clipToLoad;

    public static bool isCampaign;
    public static int currentCampaignLevel = 1; //1-3
    public static string campaignDifficulty = "";

    public static void SceneToGame(string recordingPath, AudioClip clip) //Triggered from other scene, sends current track to game
    {
        previousScene = SceneManager.GetActiveScene().name;
        recordingToLoad = recordingPath;
        if (recordingToLoad.Substring(recordingToLoad.Length - 4) != ".xml")
            recordingToLoad += ".xml";
        clipToLoad = clip;
    }

    public static void GameToMaker(string recordingName) //Triggered from game, sends current track to maker
    {
        recordingToLoad = recordingName;
        if (recordingToLoad.Substring(recordingToLoad.Length - 4) != ".xml")
            recordingToLoad += ".xml";
    }
}
