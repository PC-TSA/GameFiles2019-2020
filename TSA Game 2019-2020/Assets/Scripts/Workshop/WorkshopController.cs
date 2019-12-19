using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.File;
using Microsoft.WindowsAzure.Storage.Table;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Networking;
using TMPro;
using System.Collections;
using System.Xml;
using System.Xml.Serialization;
using SFB;

public class WorkshopController : MonoBehaviour
{
	public string shareName = "workshop";

	public string username;

	public int getSubAsyncsRunning = 0; //While this is > 0 ListAll will wait for these to finish before listing

	public List<CloudFile> recordings;

	public GameObject workshopUI;
	public GameObject workshopContentObj;
	public GameObject workshopItemPrefab;

	public NetworkingUtilities networkingUtilities;
	public CloudStorageAccount StorageAccount;

	public GameObject loadingBar;
	public List<GameObject> loadingTextPeriods;

	public Sprite tempSprite;

	//---------- Upload UI ----------
	public string selectedTrackPath;
	public TMP_Text selectedTrackPathText;
	public string selectedCoverPath;
	public TMP_Text selectedCoverPathText;

	public TMP_InputField songNameInput;
	public TMP_InputField songArtistInput;
	public TMP_Dropdown difficultyDropdown;

	public List<string> builtInSongs;

	private void Start()
	{
		StorageAccount = networkingUtilities.SetStorageAccount();
		System.IO.Directory.CreateDirectory(Application.persistentDataPath + "\\" + "Workshop");

		foreach(AudioClip s in Resources.FindObjectsOfTypeAll<AudioClip>())
			builtInSongs.Add(s.name);

		OpenWorkshop();
	}

	public async void PopulateWorkshop(string songName, string songArtist, string trackArtist, string difficulty, string coverName, string xmlName)
	{
		GameObject item = Instantiate(workshopItemPrefab, workshopContentObj.transform);
		Sprite s = await GetCoverImage(username, songName, coverName);
		item.GetComponent<WorkshopItemController>().InitializeItem(s, songName, songArtist, trackArtist, difficulty, xmlName); 
	}

	public async Task<Sprite> GetCoverImage(string username, string songName, string coverName) 
	{
		// Create a file client for interacting with the file service.
		CloudFileClient fileClient = StorageAccount.CreateCloudFileClient();

		// Create a share for organizing files and directories within the storage account.
		CloudFileShare share = fileClient.GetShareReference(shareName);

		// Get a reference to the root directory of the share.        
		CloudFileDirectory root = share.GetRootDirectoryReference();
		// Get a directory under the root directory 
		CloudFileDirectory userDir = root.GetDirectoryReference(username);
		//Get a directory under the user directory 
		CloudFileDirectory dir = userDir.GetDirectoryReference(songName);
		// Get image file
		CloudFile file = dir.GetFileReference(coverName);

		byte[] byteArr = new byte[file.StreamWriteSizeInBytes];
		await file.DownloadToByteArrayAsync(byteArr, 0);
		Texture2D tex2d = new Texture2D(2, 2);           // Create new "empty" texture
		Sprite cover = null;
		if (tex2d.LoadImage(byteArr))         // Load the imagedata into the texture (size is set automatically)
			cover = Sprite.Create(tex2d, new Rect(0, 0, tex2d.width, tex2d.height), new Vector2(0, 0), 100, 0, SpriteMeshType.Tight);

		return cover;
	}
	async void OpenWorkshop()
	{
		for (int i = 0; i < workshopContentObj.transform.childCount; i++) //Clear content
			Destroy(workshopContentObj.transform.GetChild(i).gameObject);
		await ScanTracksTable();
	}

	public void ExitWorkshop()
	{
		StartCoroutine(LoadAsyncScene("MainMenu"));
	}

	public async void ListAll()
	{
		Debug.Log("--Starting Listing--");

		// Create a file client for interacting with the file service.
		CloudFileClient fileClient = StorageAccount.CreateCloudFileClient();

		// Create a share for organizing files and directories within the storage account.
		CloudFileShare share = fileClient.GetShareReference(shareName);

		try
		{
			await share.CreateIfNotExistsAsync();
		}
		catch (StorageException)
		{
			throw;
		}

		// List all files/directories under the root directory
		List<IListFileItem> results = new List<IListFileItem>();
		recordings = new List<CloudFile>();

		FileContinuationToken token = null;
		do
		{
			FileResultSegment resultSegment = await share.GetRootDirectoryReference().ListFilesAndDirectoriesSegmentedAsync(token);
			results.AddRange(resultSegment.Results);
			token = resultSegment.ContinuationToken;
		}
		while (token != null);

		// Print all files/directories listed above
		foreach (IListFileItem listItem in results)
		{
			// listItem type will be CloudFile or CloudFileDirectory
			if (listItem is CloudFileDirectory)
				GetSubs((CloudFileDirectory)listItem); //Get subdirectories
			else
				Debug.Log("File Found: " + listItem.Uri);
		}

		//StartCoroutine(WaitForSubs());

		Debug.Log("--Listing Complete--");
	}

	private async Task ScanTracksTable()
	{
		TableQuery<TrackEntity> partitionScanQuery = new TableQuery<TrackEntity>();

		CloudTableClient tableClient = StorageAccount.CreateCloudTableClient();
		CloudTable table = tableClient.GetTableReference("Tracks");

		TableContinuationToken token = null;

		// Page through the results
		do
		{
			TableQuerySegment<TrackEntity> segment = await table.ExecuteQuerySegmentedAsync(partitionScanQuery, token);
			token = segment.ContinuationToken;
			foreach (TrackEntity entity in segment)
			{
				if(entity.PartitionKey != "TrackCounter") //TrackCounter keeps track of table size
					PopulateWorkshop(entity.SongName, entity.SongArtist, entity.RowKey, entity.Difficulty, entity.CoverName, entity.RowKey);
			}
		}
		while (token != null);
	}

	public void PickRecording() 
	{
		var extensions = new[] {
			new ExtensionFilter("XML", "xml" ), };

		string[] temp = StandaloneFileBrowser.OpenFilePanel("Open File", "", extensions, false);
		if (temp.Length != 0 && temp[0].Length != 0)
			selectedTrackPath = temp[0];

		selectedTrackPathText.text = selectedTrackPath;
	}

	public void PickCover()
	{
		var extensions = new[] {
			new ExtensionFilter("Image Files", "png", "jpg", "jpeg" ), };
		string[] temp = StandaloneFileBrowser.OpenFilePanel("Open File", "", extensions, false);
		if (temp.Length != 0 && temp[0].Length != 0)
			selectedCoverPath = temp[0];

		selectedCoverPathText.text = selectedCoverPath;
	}

	public void Upload()
	{
		string songName = songNameInput.text;
		string songArtist = songArtistInput.text;
		string difficulty = difficultyDropdown.options[difficultyDropdown.value].text;
		if (selectedTrackPath.Length > 0 && songName.Length > 0 && songArtist.Length > 0) //A path has been picked, song name & song artist have been specified
		{
			//Get xml name from path
			string xmlName = selectedTrackPath;
			xmlName = selectedTrackPath.Substring(selectedTrackPath.LastIndexOf('\\') + 1);

			//Get cover name from path
			string coverName = selectedCoverPath;
			if (selectedCoverPath.Length > 0)
				coverName = selectedCoverPath.Substring(selectedCoverPath.LastIndexOf('\\') + 1);

			string wavPath = "";

			//Get XML as recording
			var serializer = new XmlSerializer(typeof(Recording));
			var stream = new FileStream(selectedTrackPath, FileMode.Open);
			Recording rec = serializer.Deserialize(stream) as Recording;
			stream.Close();

			//Update recording vals
			rec.songName = songName;
			rec.songArtist = songArtist;
			rec.trackDifficulty = difficulty;

			if (!builtInSongs.Contains(rec.clipName)) //If the recording is not for a built in song
			{
				wavPath = Application.persistentDataPath + "\\" + "Songs" + "\\" + rec.clipName + ".wav"; 
				rec.clipName = rec.songName;
			}

			//Override XML file with new vals
			stream = new FileStream(selectedTrackPath, FileMode.Create);
			serializer.Serialize(stream, rec);
			stream.Close();

			UploadRecording(rec, selectedTrackPath, xmlName, songName, songArtist, difficulty, selectedCoverPath, coverName, wavPath);
		}
	}

	async void UploadRecording(Recording recording, string xmlPath, string xmlName, string songName, string songArtist, string difficulty, string coverPath, string coverName, string wavPath)
	{
		Debug.Log("--Starting Upload--");

		// Create a file client for interacting with the file service.
		CloudFileClient fileClient = StorageAccount.CreateCloudFileClient();

		// Create a share for organizing files and directories within the storage account.
		CloudFileShare share = fileClient.GetShareReference(shareName);

		try
		{
			await share.CreateIfNotExistsAsync();
		}
		catch (StorageException)
		{
			throw;
		}

		// Get a reference to the root directory of the share.        
		CloudFileDirectory root = share.GetRootDirectoryReference();

		// Create a directory under the root directory 
		CloudFileDirectory userDir = root.GetDirectoryReference(username);
		await userDir.CreateIfNotExistsAsync();
		// Create a directory under the user directory 
		CloudFileDirectory dir = userDir.GetDirectoryReference(songName);
		await dir.CreateIfNotExistsAsync();

		// Uploading XML
		CloudFile file = dir.GetFileReference(xmlName);
		await file.UploadFromFileAsync(xmlPath);

		//Upload cover (if one has been selected)
		if(coverPath.Length > 0)
		{
			file = dir.GetFileReference(coverName);
			await file.UploadFromFileAsync(coverPath);
		}

		//Upload song (if the recording is not for a built in song)
		if(wavPath.Length > 0)
		{
			file = dir.GetFileReference(songName + ".wav");
			await file.UploadFromFileAsync(wavPath);
		}

		AddToTracksTable(xmlName, songName, songArtist, coverName, difficulty);

		Debug.Log("--Upload Complete--");
	}

	//XML name | xml artist | song name | song artist
	public async void AddToTracksTable(string xmlName, string songName, string songArtist, string coverName, string difficulty)
	{
		Debug.Log("-- Adding ' " + xmlName + "' to Tracks Table --");
		CloudTableClient tableClient = StorageAccount.CreateCloudTableClient();

		// Create a table client for interacting with the table service 
		CloudTable table = tableClient.GetTableReference("Tracks");
		try
		{
			await table.CreateIfNotExistsAsync();
		}
		catch (StorageException)
		{
			throw;
		}
		
		//Get current table size by retrieving entity with partitionKey & rowKey "TrackCounter" 
		TableOperation retrieve = TableOperation.Retrieve<TrackCounter>("TrackCounter", "TrackCounter");
		TableResult result = await table.ExecuteAsync(retrieve);
		TrackCounter trackCounter = (TrackCounter) result.Result;
		//Gets table count & adds 1 for new track being uploaded
		trackCounter.TrackCount++;
		//Inserts new track counter value
		TableOperation insert = TableOperation.InsertOrReplace(trackCounter);
		await table.ExecuteAsync(insert);

		TrackEntity trackEntity = new TrackEntity(trackCounter.TrackCount, xmlName, username, songName, songArtist, coverName, difficulty);
		await InsertOrMergeEntityAsync(table, trackEntity);
	}

	async void GetSubs(CloudFileDirectory directory)
	{
		getSubAsyncsRunning++;
		FileContinuationToken token = null;
		do
		{
			FileResultSegment resultSegment = await directory.ListFilesAndDirectoriesSegmentedAsync(token);
			List<IListFileItem> newResults = new List<IListFileItem>();
			newResults.AddRange(resultSegment.Results);
			token = resultSegment.ContinuationToken;

			foreach (IListFileItem listItem in newResults)
			{
				// listItem type will be CloudFile or CloudFileDirectory
				if (listItem is CloudFileDirectory)
					GetSubs((CloudFileDirectory)listItem); //Get subdirectories
				else
					recordings.Add((CloudFile)listItem);
			}
		}
		while (token != null);
		getSubAsyncsRunning--;
	}

	IEnumerator LoadAsyncScene(string scene)
	{
		AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(scene);
		StartCoroutine(LoadingBar());
		// Wait until the asynchronous scene fully loads
		while (!asyncLoad.isDone)
		{
			loadingBar.GetComponent<Slider>().value = asyncLoad.progress;
			yield return null;
		}
	}

	IEnumerator LoadingBar()
	{
		loadingBar.SetActive(true);
		int periodIndex = 0;
		while (true)
		{
			for (int i = 0; i < loadingTextPeriods.Count; i++)
				if (i == periodIndex)
					loadingTextPeriods[i].SetActive(true);

			if (periodIndex == loadingTextPeriods.Count)
			{
				periodIndex = 0;
				foreach (GameObject obj in loadingTextPeriods)
					obj.SetActive(false);
			}
			else
				periodIndex++;

			yield return new WaitForSeconds(0.5f);
		}
	}

	private async Task<ScoreEntity> InsertOrMergeEntityAsync(CloudTable table, TrackEntity entity)
	{
		if (entity == null)
		{
			throw new ArgumentNullException("entity");
		}

		// Create the InsertOrReplace  TableOperation
		TableOperation insertOrMergeOperation = TableOperation.InsertOrMerge(entity);

		// Execute the operation.
		TableResult result = await table.ExecuteAsync(insertOrMergeOperation);
		ScoreEntity insertedCustomer = result.Result as ScoreEntity;
		return insertedCustomer;
	}

	public Sprite LoadNewSprite(string FilePath, float PixelsPerUnit = 100.0f, SpriteMeshType spriteType = SpriteMeshType.Tight)
	{
		// Load a PNG or JPG image from disk to a Texture2D, assign this texture to a new sprite and return its reference
		Texture2D SpriteTexture = LoadTexture(FilePath);
		Sprite NewSprite = Sprite.Create(SpriteTexture, new Rect(0, 0, SpriteTexture.width, SpriteTexture.height), new Vector2(0, 0), PixelsPerUnit, 0, spriteType);

		return NewSprite;
	}

	public Texture2D LoadTexture(string FilePath)
	{

		// Load a PNG or JPG file from disk to a Texture2D
		// Returns null if load fails

		Texture2D Tex2D;
		byte[] FileData;

		if (File.Exists(FilePath))
		{
			FileData = File.ReadAllBytes(FilePath);
			Tex2D = new Texture2D(2, 2);           // Create new "empty" texture
			if (Tex2D.LoadImage(FileData))           // Load the imagedata into the texture (size is set automatically)
				return Tex2D;                 // If data = readable -> return texture
		}
		return null;                     // Return null if load failed
	}

	/*public async Task<AudioClip> DownloadWorkshopItem(WorkshopItemController item) 
	{
		// Create a file client for interacting with the file service.
		CloudFileClient fileClient = StorageAccount.CreateCloudFileClient();

		// Create a share for organizing files and directories within the storage account.
		CloudFileShare share = fileClient.GetShareReference(shareName);

		// Get a reference to the root directory of the share.        
		CloudFileDirectory root = share.GetRootDirectoryReference();
		// Get a directory under the root directory 
		CloudFileDirectory userDir = root.GetDirectoryReference(item.trackArtist);
		//Get a directory under the user directory 
		CloudFileDirectory dir = userDir.GetDirectoryReference(item.songName);
		// Get image file
		CloudFile file = dir.GetFileReference(item.xmlName);
		string path = Application.persistentDataPath + "\\" + "Workshop" + "\\" + item.trackArtist + "\\" + item.xmlName.Substring(0, item.xmlName.Length - 4) + ".wav";
		await file.DownloadToFileAsync(path, FileMode.CreateNew);

		UnityWebRequest AudioFiles = null;
		string audioFileName = path.Substring(path.LastIndexOf('\\') + 1);
		audioFileName = audioFileName.Remove(audioFileName.Length - 4);
		AudioFiles = UnityWebRequestMultimedia.GetAudioClip(path, AudioType.WAV);

		if (AudioFiles != null)
		{
			yield return AudioFiles.SendWebRequest();
			if (AudioFiles.isNetworkError)
				Debug.Log(AudioFiles.error);
			else
			{
				AudioClip clip = DownloadHandlerAudioClip.GetContent(AudioFiles);
				clip.name = audioFileName;
			}
		}
	}*/

	public void OpenItem(WorkshopItemController item)
	{
		System.IO.Directory.CreateDirectory(Application.persistentDataPath + "\\" + "Workshop" + "\\" + item.trackArtist);

	}
}

public class TrackEntity : TableEntity
{
	// Your entity type must expose a parameter-less constructor
	public TrackEntity() { }

	// Define the PK and RK
	public TrackEntity(int id, string xmlName, string xmlArtist, string songName, string songArtist, string coverName, string difficulty)
	{
		this.PartitionKey = "" + id;	
		this.RowKey = xmlName;
		XMLArtist = xmlArtist;
		SongName = songName;
		SongArtist = songArtist;
		CoverName = coverName;
		Difficulty = difficulty;
	}
	public string XMLArtist { get; set; }
	public string SongName { get; set; }
	public string SongArtist { get; set; }
	public string CoverName { get; set; }
	public string Difficulty { get; set; }
}

public class TrackCounter : TableEntity
{
	// Your entity type must expose a parameter-less constructor
	public TrackCounter() { }

	// Define the PK and RK
	public TrackCounter(int count)
	{
		this.PartitionKey = "TrackCounter";
		this.RowKey = "TrackCounter";
		TrackCount = count;
	}
	public int TrackCount { get; set; }
}