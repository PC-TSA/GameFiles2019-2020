﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LevelSelectItemController : MonoBehaviour
{
    public int id;
    public Sprite coverSprite;
    public string songName;
    public string songArtist;
    public string trackArtist;
    public string difficulty;
    public string xmlName;
    public string mp3Name;

    public Image songCoverImage;
    public TMP_Text songNameText;
    public TMP_Text songArtistText;
    public TMP_Text difficultyText;
    public TMP_Text trackArtistText;

    public void InitializeItem(Sprite coverSprite, string songName, string songArtist, string trackArtist, string difficulty, string xmlName, string mp3Name, int id)
    {
        if (coverSprite != null)
            this.coverSprite = coverSprite;
        this.songName = songName;
        this.songArtist = songArtist;
        this.trackArtist = trackArtist;
        this.difficulty = difficulty;
        this.xmlName = xmlName;
        this.mp3Name = mp3Name;
        this.id = id;
        UpdateChildren();
    }

    public void UpdateChildren()
    {
        songCoverImage.sprite = coverSprite;
        songNameText.text = songName;
        songArtistText.text = songArtist;
        trackArtistText.text = trackArtist;
        difficultyText.text = difficulty;
    }

    public void PlayTrack()
    {
        GameObject.FindObjectOfType<LevelSelectController>().PlayTrack(this);
    } 
}