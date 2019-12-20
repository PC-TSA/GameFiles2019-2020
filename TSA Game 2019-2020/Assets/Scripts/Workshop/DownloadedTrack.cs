using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DownloadedTrack
{
    public int id;
    public Sprite coverSprite;
    public string songName;
    public string songArtist;
    public string trackArtist;
    public string difficulty;
    public string xmlName;
    public string mp3Name;

    public string downloadedPath;

    //Parameterless constructor for serialization
    public DownloadedTrack() { }

    public DownloadedTrack(Sprite coverSprite, string songName, string songArtist, string trackArtist, string difficulty, string xmlName, string mp3Name, int id, string downloadedPath)
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
        this.downloadedPath = downloadedPath;
    }

    public DownloadedTrack(WorkshopItemController item)
    {
        if (coverSprite != null)
            this.coverSprite = item.coverSprite;
        this.songName = item.songName;
        this.songArtist = item.songArtist;
        this.trackArtist = item.trackArtist;
        this.difficulty = item.difficulty;
        this.xmlName = item.xmlName;
        this.mp3Name = item.mp3Name;
        this.id = item.id;
    }
}
