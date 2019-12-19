using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WorkshopItemController : MonoBehaviour
{
    public int id;
    public Sprite coverSprite;
    public string songName;
    public string songArtist;
    public string trackArtist;
    public string difficulty;
    public string xmlName;

    public Image songCoverImage;
    public TMP_Text songNameText;
    public TMP_Text songArtistText;
    public TMP_Text difficultyText;
    public TMP_Text trackArtistText;

    public void InitializeItem(Sprite coverSprite, string songName, string songArtist, string trackArtist, string difficulty, string xmlName)
    {
        if(coverSprite != null)
            this.coverSprite = coverSprite;
        this.songName = songName;
        this.songArtist = songArtist;
        this.trackArtist = trackArtist;
        this.difficulty = difficulty;
        this.xmlName = xmlName;
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

    void OpenItem()
    {
        GameObject.FindObjectOfType<WorkshopController>().OpenItem(this);
    }
}
