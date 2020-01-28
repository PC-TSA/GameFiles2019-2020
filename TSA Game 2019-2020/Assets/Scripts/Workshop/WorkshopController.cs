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
	public GameObject myTracksItemPrefab;
	public GameObject workshopUIMisc;

	public NetworkingUtilities networkingUtilities;
	public CloudStorageAccount StorageAccount;

	public GameObject loadingBar;
	public GameObject uploadBar;
	public GameObject downloadBar;

	public TMP_Text uploadBarProgress;
	public TMP_Text downloadBarProgress;
	public bool isDownloading;

	public GameObject splashTitlePrefab;

	Sprite tempSprite;

	public List<TrackEntity> pulledTracks;

	//---------- Upload UI ----------
	public string selectedTrackPath;
	public TMP_Text selectedTrackPathText;
	public string selectedCoverPath;
	public TMP_Text selectedCoverPathText;

	public TMP_InputField songNameInput;
	public string songNameTemp;
	public TMP_InputField songArtistInput;
	public string songArtistTemp;
	public TMP_Dropdown difficultyDropdown;
	public string difficultyTemp;

	public Image previewCover;
	public TMP_Text previewSongName;
	public TMP_Text previewSongArtist;
	public TMP_Text previewUsername;
	public TMP_Text previewDifficulty;

	//-------------------------------

	public List<string> builtInSongs;
	public List<DownloadedTrack> downloadedTracks;

	//Top UI
	public TMP_InputField search;
	public TMP_Dropdown difficultyFilterDropdown;

	//Right Tab UI
	public bool tabSelectorMoving;
	public GameObject tabSelector;
	public Vector3 tabSelectorGoalPos;
	public float tabSelectorSpeed;

	//Tabs
	public GameObject browseTracksTab;
	public GameObject myTracksTab;
	public GameObject uploadTab;

	//My Tracks
	public GameObject myTracksContentObj;

	private void Start()
	{
		StorageAccount = networkingUtilities.SetStorageAccount();
		System.IO.Directory.CreateDirectory(Application.persistentDataPath + "\\" + "Workshop");
		username = PlayerPrefs.GetString("username");

		//Read all audioclips in the Resources/Songs folder and add them to the 'builtInSongs' list
		UnityEngine.Object[] temp = Resources.LoadAll("Songs", typeof(AudioClip));
		foreach (UnityEngine.Object o in temp)
			builtInSongs.Add(o.name);

		PopulateDownloadedTracks();

		OpenWorkshop();

	}

	private void Update()
	{
		if (tabSelectorMoving)
		{
			tabSelector.transform.localPosition = Vector3.Lerp(tabSelector.transform.localPosition, tabSelectorGoalPos, Time.deltaTime * tabSelectorSpeed);
			if (Vector3.Distance(tabSelector.transform.localPosition, tabSelectorGoalPos) < 0.001)
				tabSelectorMoving = false;
		}
	}

	//Populate downloadedTracks list by reading Application.persistentDataPath / DownloadedTracks / DownloadedTracks
	void PopulateDownloadedTracks()
	{
		downloadedTracks = new List<DownloadedTrack>();

		foreach (string file in System.IO.Directory.GetFiles(Application.persistentDataPath + "\\" + "DownloadedTracks"))
		{
			if (file.Substring(file.Length - 3) == "xml")
			{
				var serializer = new XmlSerializer(typeof(DownloadedTrack));
				var stream = new FileStream(file, FileMode.Open);
				DownloadedTrack track = serializer.Deserialize(stream) as DownloadedTrack;
				stream.Close();
				downloadedTracks.Add(track);
			}
			else
				return;
		}
	}

	async void PopulateWorkshop(TrackEntity entity)
	{
		GameObject item = Instantiate(workshopItemPrefab, workshopContentObj.transform);
		Sprite sprite = await GetCoverImage(entity.XMLArtist, entity.SongName, entity.CoverName);
		item.GetComponent<WorkshopItemController>().InitializeItem(sprite, entity.SongName, entity.SongArtist, entity.XMLArtist, entity.Difficulty, entity.RowKey, entity.Mp3Name, int.Parse(entity.PartitionKey));
		item.GetComponent<WorkshopItemController>().entity = entity;
	}

	public async Task<Sprite> GetCoverImage(string username, string songName, string coverName) 
	{
		if(coverName == "")
			return null;

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

		if (await file.ExistsAsync())
		{
			byte[] byteArr = new byte[file.StreamWriteSizeInBytes];
			await file.DownloadToByteArrayAsync(byteArr, 0);
			Texture2D tex2d = new Texture2D(2, 2);           // Create new "empty" texture
			Sprite cover = null;
			if (tex2d.LoadImage(byteArr))         // Load the imagedata into the texture (size is set automatically)
				cover = Sprite.Create(tex2d, new Rect(0, 0, tex2d.width, tex2d.height), new Vector2(0, 0), 100, 0, SpriteMeshType.Tight);

			return cover;
		}
		else
			return null;
	}
	void OpenWorkshop()
	{
		PopulateWorkshop();
		PopulateMyTracksTab();
		PopulateUploadTab();
	}

	async void PopulateWorkshop()
	{
		pulledTracks = new List<TrackEntity>();
		Debug.Log("Clearing and populating workshop normally...");

		for (int i = 0; i < workshopContentObj.transform.childCount; i++) //Clear content
			Destroy(workshopContentObj.transform.GetChild(i).gameObject);

		if (FiltersActive())
			PopulateWithFilters();
		else
			await ScanTracksTable();
	}

	void PopulateUploadTab()
	{
		previewUsername.text = PlayerPrefs.GetString("username");
	}

	public void ExitWorkshop()
	{
		StartCoroutine(LoadAsyncScene("MainMenu"));
	}

	bool FiltersActive()
	{
		if (search.text.Trim().Length > 0)
			return true;
		else if (difficultyFilterDropdown.options[difficultyFilterDropdown.value].text != "Any Difficulty")
			return true;
		else
			return false;
	}

	public void UpdateWithFilter()
	{
		Debug.Log("Clearing and populating workshop with filters...");
		for (int i = 0; i < workshopContentObj.transform.childCount; i++) //Clear content
			Destroy(workshopContentObj.transform.GetChild(i).gameObject);
		PopulateWithFilters();
	}

	async void PopulateWithFilters()
	{
		string searchFilter = search.text.ToLower().Trim();
		string difficultyFilter = difficultyFilterDropdown.options[difficultyFilterDropdown.value].text;

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
				bool shouldInstantiate = true;

				if (entity.PartitionKey == "TrackCounter") //TrackCounter keeps track of table 
					shouldInstantiate = false;
				if (searchFilter.Length > 0)
					if (!entity.SongName.ToLower().Contains(searchFilter) && !entity.SongArtist.ToLower().Contains(searchFilter) && !entity.XMLArtist.ToLower().Contains(searchFilter)) //Search filter
						shouldInstantiate = false;
				if (difficultyFilter != "Any Difficulty") //Difficulty filter
					if (entity.Difficulty != difficultyFilter)
						shouldInstantiate = false;

				if(shouldInstantiate)
					PopulateWorkshop(entity);
			}
		}
		while (token != null);
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
				if (entity.PartitionKey != "TrackCounter") //TrackCounter keeps track of table 
					PopulateWorkshop(entity);
			}
		}
		while (token != null);
	}

	private async Task ScanTracksTable(string user)
	{
		TableQuery<TrackEntity> partitionScanQuery = new TableQuery<TrackEntity>().Where
			(TableQuery.GenerateFilterCondition("XMLArtist", QueryComparisons.Equal, user));

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
				if (entity.PartitionKey != "TrackCounter") //TrackCounter keeps track of table 
					PopulateMyTracks(entity);
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
		{
			selectedCoverPath = temp[0];
			previewCover.sprite = LoadNewSprite(selectedCoverPath);
		}

		selectedCoverPathText.text = selectedCoverPath;
	}

	public void UpdateSongName()
	{
		songNameTemp = songNameInput.text;
		previewSongName.text = songNameTemp;
	}

	public void UpdateSongArtist()
	{
		songArtistTemp = songArtistInput.text;
		previewSongArtist.text = songArtistTemp;
	}

	public void UpdateSongDifficulty()
	{
		difficultyTemp = difficultyDropdown.options[difficultyDropdown.value].text;
		previewDifficulty.text = difficultyTemp;
	}

	public void Upload()
	{
		uploadBar.SetActive(true);
		uploadBarProgress.text = "Reading xml...";
		StartCoroutine(StartBar(uploadBar));

		string songName = songNameTemp;
		string songArtist = songArtistTemp;
		string difficulty = difficultyTemp;
		if (selectedTrackPath.Length > 0 && songName.Length > 0 && songArtist.Length > 0) //A path has been picked, song name & song artist have been specified
		{
			//Get xml name from path
			string xmlName = selectedTrackPath.Substring(selectedTrackPath.LastIndexOf('\\') + 1);

			//Get cover name from path
			string coverName = "";
			if (selectedCoverPath.Length > 0)
				coverName = selectedCoverPath.Substring(selectedCoverPath.LastIndexOf('\\') + 1);

			string mp3Path = "";

			//Get XML as recording
			var serializer = new XmlSerializer(typeof(Recording));
			var stream = new FileStream(selectedTrackPath, FileMode.Open);
			Recording rec = serializer.Deserialize(stream) as Recording;
			stream.Close();

			//Update recording vals
			rec.songName = songName;
			rec.songArtist = songArtist;
			rec.trackDifficulty = difficulty;
			rec.trackArtist = PlayerPrefs.GetString("username");

			if (!builtInSongs.Contains(rec.clipName)) //If the recording is not for a built in song
				mp3Path = Application.persistentDataPath + "\\" + "Songs" + "\\" + rec.clipName + ".mp3";

			//Override XML file with new vals
			stream = new FileStream(selectedTrackPath, FileMode.Create);
			serializer.Serialize(stream, rec);
			stream.Close();

			UploadRecording(rec, selectedTrackPath, xmlName, songName, songArtist, difficulty, selectedCoverPath, coverName, mp3Path, rec.clipName);
		}
	}

	async void UploadRecording(Recording recording, string xmlPath, string xmlName, string songName, string songArtist, string difficulty, string coverPath, string coverName, string mp3Path, string mp3Name)
	{
		Debug.Log("--Starting Upload--");

		uploadBarProgress.text = "Creating database directories...";
		uploadBar.GetComponent<Slider>().value++;

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

		uploadBarProgress.text = "Uploading recording...";
		uploadBar.GetComponent<Slider>().value++;

		// Uploading XML
		CloudFile file = dir.GetFileReference(xmlName);
		await file.UploadFromFileAsync(xmlPath);

		uploadBarProgress.text = "Uploading cover...";
		uploadBar.GetComponent<Slider>().value++;

		//Upload cover (if one has been selected)
		if (coverPath.Length > 0)
		{
			file = dir.GetFileReference(coverName);
			await file.UploadFromFileAsync(coverPath);
		}

		uploadBarProgress.text = "Uploading audio...";
		uploadBar.GetComponent<Slider>().value++;

		//Upload song (if the recording is not for a built in song)
		if (mp3Path.Length > 0)
		{
			file = dir.GetFileReference(mp3Name + ".mp3");
			await file.UploadFromFileAsync(mp3Path);
		}

		uploadBarProgress.text = "Adding to tracks table...";
		uploadBar.GetComponent<Slider>().value++;

		//Add to tracks table
		AddToTracksTable(xmlName, songName, songArtist, coverName, difficulty, mp3Name);

		//Make leaderboard table
		networkingUtilities.NewLeaderboard(username + xmlName);

		uploadBarProgress.text = "--Upload Complete--";
		uploadBar.GetComponent<Slider>().value++;
		StartCoroutine(StopBar(uploadBar, true));

		Debug.Log("--Upload Complete--");

		PopulateWorkshop();
	}

	//XML name | xml artist | song name | song artist
	public async void AddToTracksTable(string xmlName, string songName, string songArtist, string coverName, string difficulty, string mp3Name)
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

		TrackEntity trackEntity = new TrackEntity(trackCounter.TrackCount, xmlName, username, songName, songArtist, coverName, difficulty, mp3Name);
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
		StartCoroutine(StartBar(loadingBar));
		// Wait until the asynchronous scene fully loads
		while (!asyncLoad.isDone)
		{
			loadingBar.GetComponent<Slider>().value = asyncLoad.progress;
			yield return null;
		}
	}

	IEnumerator StartBar(GameObject bar)
	{
		bar.SetActive(true);
		int periodIndex = 0;
		List<GameObject> loadingTextPeriods = new List<GameObject>();
		for(int i = 0; i < bar.transform.GetChild(0).childCount; i ++) //Populate loading text periods from bar's children
			loadingTextPeriods.Add(bar.transform.GetChild(0).GetChild(i).gameObject);

		while (bar.activeSelf) //While the bar is active, animate loading text periods
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
	IEnumerator StopBar(GameObject bar, bool shouldFade)
	{
		if (shouldFade)
		{
			bar.GetComponent<Animator>().Play("CanvasGroupFadeOut");
			yield return new WaitForSeconds(1);
		}
		bar.SetActive(false);
		if (shouldFade)
			bar.GetComponent<CanvasGroup>().alpha = 1;
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

	public void OpenItem(WorkshopItemController item)
	{
		DownloadWorkshopItem(item);
	}

	public async void DeleteItem(TrackEntity entity, GameObject item)
	{
		CloudTableClient tableClient = StorageAccount.CreateCloudTableClient();
		CloudTable table = tableClient.GetTableReference("Tracks");

		//Delete from "Tracks" table
		if (entity == null)
			throw new ArgumentNullException("deleteItem");
		TableOperation deleteOperation = TableOperation.Delete(entity);
		await table.ExecuteAsync(deleteOperation);

		//Delete leaderboard table
		table = tableClient.GetTableReference(entity.XMLArtist + entity.SongName);
		await table.DeleteIfExistsAsync(); //Doesnt currently work due to unkown format exception 

		//Delete track files in workshop file share + track folder
		CloudFileClient fileClient = StorageAccount.CreateCloudFileClient();
		CloudFileShare share = fileClient.GetShareReference(shareName);
		CloudFileDirectory dir = share.GetRootDirectoryReference().GetDirectoryReference(entity.XMLArtist).GetDirectoryReference(entity.SongName);
		await dir.GetFileReference(entity.RowKey).DeleteIfExistsAsync();
		await dir.GetFileReference(entity.CoverName).DeleteIfExistsAsync();
		await dir.GetFileReference(entity.Mp3Name + ".mp3").DeleteIfExistsAsync();
		await dir.DeleteIfExistsAsync();

		//Destroy workshop UI item
		for(int i = 0; i < workshopContentObj.transform.childCount; i++)
		{
			if(workshopContentObj.transform.GetChild(i).GetComponent<WorkshopItemController>().id == item.GetComponent<WorkshopItemController>().id)
			{
				Destroy(workshopContentObj.transform.GetChild(i));
				break;
			}
		}
		Destroy(item);
	}

	async void DownloadWorkshopItem(WorkshopItemController item)
	{
		if (isDownloading)
			return;

		//If this track has already been downloaded, exit with warning
		DownloadedTrack track = new DownloadedTrack(item);
		foreach (DownloadedTrack t in downloadedTracks)
		{
			if (t.id == track.id)
			{
				SpawnSplashTitle("Song Already Downloaded", Color.red);
				return;
			}
		}

		//Start download bar
		downloadBar.SetActive(true);
		downloadBarProgress.text = "Starting download...";
		downloadBar.GetComponent<Slider>().maxValue = 4;
		StartCoroutine(StartBar(downloadBar));
		isDownloading = true;

		//Save and serialize downloadedTrack xml
		downloadedTracks.Add(track);
		var serializer = new XmlSerializer(typeof(DownloadedTrack));
		var stream = new FileStream(Application.persistentDataPath + "\\DownloadedTracks\\" + track.xmlName, FileMode.Create);
		serializer.Serialize(stream, track);
		stream.Close();

		//Create directories
		System.IO.Directory.CreateDirectory(Application.persistentDataPath + "\\Workshop\\" + item.trackArtist);
		System.IO.Directory.CreateDirectory(Application.persistentDataPath + "\\Workshop\\" + item.trackArtist + "\\" + item.songName);

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

		downloadBarProgress.text = "Downloading XML...";
		downloadBar.GetComponent<Slider>().value = 1;
		//Download XML file
		CloudFile file = dir.GetFileReference(item.xmlName);
		string path = Application.persistentDataPath + "\\" + "Workshop" + "\\" + item.trackArtist + "\\" + item.songName + "\\" + item.xmlName;
		await file.DownloadToFileAsync(path, FileMode.CreateNew);

		downloadBarProgress.text = "Saving cover...";
		downloadBar.GetComponent<Slider>().value = 2;
		//Save already downloaded cover file
		byte[] coverBytes = item.coverSprite.texture.EncodeToJPG();
		path = Application.persistentDataPath + "\\" + "Workshop" + "\\" + item.trackArtist + "\\" + item.songName + "\\" + "cover.jpg";
		var coverFile = File.Open(path, FileMode.Create);
		var writer = new BinaryWriter(coverFile);
		writer.Write(coverBytes);
		coverFile.Close();

		//Download music file (if not a built in song)
		if (!builtInSongs.Contains(item.mp3Name))
		{
			downloadBarProgress.text = "Downloading song...";
			downloadBar.GetComponent<Slider>().value = 3;
			file = dir.GetFileReference(item.mp3Name + ".mp3");
			path = Application.persistentDataPath + "\\" + "Workshop" + "\\" + item.trackArtist + "\\" + item.songName + "\\" + item.mp3Name + ".mp3";
			await file.DownloadToFileAsync(path, FileMode.CreateNew);
		}

		downloadBarProgress.text = "--Download Complete--";
		downloadBar.GetComponent<Slider>().value = 4;
		StartCoroutine(StopBar(downloadBar, true));
		isDownloading = false;
	}

	public void SpawnSplashTitle(string titleText, Color titleColor)
	{
		GameObject newSplashTitle = Instantiate(splashTitlePrefab, workshopUIMisc.transform);
		newSplashTitle.GetComponent<TMP_Text>().text = titleText;
		newSplashTitle.GetComponent<TMP_Text>().color = titleColor;
		StartCoroutine(KillSplashTitle(newSplashTitle));
	}

	IEnumerator KillSplashTitle(GameObject title)
	{
		yield return new WaitForSeconds(title.GetComponent<Animation>().clip.length);
		Destroy(title);
	}

	public void TabSelector(GameObject tabObj)
	{
		tabSelectorGoalPos = new Vector3(tabSelector.transform.localPosition.x, tabObj.transform.localPosition.y, tabSelector.transform.localPosition.z);
		tabSelectorMoving = true;
	}

	public void BrowseTracksTab()
	{
		browseTracksTab.SetActive(true);
		myTracksTab.SetActive(false);
		uploadTab.SetActive(false);
	}

	public void MyTracksTab()
	{
		browseTracksTab.SetActive(false);
		myTracksTab.SetActive(true);
		uploadTab.SetActive(false);
	}

	public void UploadTab()
	{
		browseTracksTab.SetActive(false);
		myTracksTab.SetActive(false);
		uploadTab.SetActive(true);
	}

	async void PopulateMyTracksTab()
	{
		await ScanTracksTable(PlayerPrefs.GetString("username"));
	}

	async void PopulateMyTracks(TrackEntity entity)
	{
		GameObject item = Instantiate(myTracksItemPrefab, myTracksContentObj.transform);
		Sprite sprite = await GetCoverImage(entity.XMLArtist, entity.SongName, entity.CoverName);
		item.GetComponent<WorkshopItemController>().InitializeItem(sprite, entity.SongName, entity.SongArtist, entity.XMLArtist, entity.Difficulty, entity.RowKey, entity.Mp3Name, int.Parse(entity.PartitionKey));
		item.GetComponent<WorkshopItemController>().entity = entity;
	}
}

public class TrackEntity : TableEntity
{
	// Your entity type must expose a parameter-less constructor
	public TrackEntity() { }

	// Define the PK and RK
	public TrackEntity(int id, string xmlName, string xmlArtist, string songName, string songArtist, string coverName, string difficulty, string mp3Name)
	{
		this.PartitionKey = "" + id;
		this.RowKey = xmlName;
		XMLArtist = xmlArtist;
		SongName = songName;
		SongArtist = songArtist;
		CoverName = coverName;
		Difficulty = difficulty;
		Mp3Name = mp3Name;
	}
	public string XMLArtist { get; set; }
	public string SongName { get; set; }
	public string SongArtist { get; set; }
	public string CoverName { get; set; }
	public string Difficulty { get; set; }
	public string Mp3Name { get; set; }
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