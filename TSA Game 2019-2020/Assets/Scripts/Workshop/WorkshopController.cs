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

	//---------- Upload UI ----------
	public string selectedTrackPath;
	public TMP_Text selectedTrackPathText;
	public string selectedCoverPath;
	public TMP_Text selectedCoverPathText;

	public TMP_InputField songNameInput;
	public TMP_InputField songArtistInput;
	public TMP_Dropdown difficultyDropdown;

	private void Start()
	{
		StorageAccount = networkingUtilities.SetStorageAccount();
		OpenWorkshop();
	}

	public void PopulateWorkshop(string songName, string songArtist, string trackArtist, string difficulty, string coverName)
	{
		GameObject item = Instantiate(workshopItemPrefab, workshopContentObj.transform);
		Sprite cover = GetCoverImage(username, songName, coverName);
		item.GetComponent<WorkshopItemController>().InitializeItem(cover, songName, songArtist, trackArtist, difficulty); 
	}

	public Sprite GetCoverImage(string username, string songName, string coverName) //NOT DONE
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
		string path = Path.Combine(Application.temporaryCachePath, coverName);
		file.DownloadToFileAsync(path, FileMode.Create);

		//RETURN FILE AT Application.temporaryCachePath + coverName AS SPRITE
		return null;
	}

	void OpenWorkshop()
	{
		for (int i = 0; i < workshopContentObj.transform.childCount; i++) //Clear content
			Destroy(workshopContentObj.transform.GetChild(i).gameObject);
		ListAll();
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
				recordings.Add((CloudFile)listItem);
		}

		StartCoroutine(WaitForSubs());

		Debug.Log("--Listing Complete--");
	}

	private async Task TableScanAsync(CloudTable table)
	{
		TableQuery<TrackEntity> partitionScanQuery = new TableQuery<TrackEntity>();

		TableContinuationToken token = null;
		// Page through the results
		do
		{
			TableQuerySegment<TrackEntity> segment = await table.ExecuteQuerySegmentedAsync(partitionScanQuery, token);
			token = segment.ContinuationToken;
			foreach (TrackEntity entity in segment)
			{
				PopulateWorkshop(entity.SongName, entity.SongArtist, entity.RowKey, entity.Difficulty, entity.CoverName);
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

			//Get XML as recording
			var serializer = new XmlSerializer(typeof(Recording));
			var stream = new FileStream(selectedTrackPath, FileMode.Open);
			Recording rec = serializer.Deserialize(stream) as Recording;
			stream.Close();

			//Update recording vals
			rec.songName = songName;
			rec.songArtist = songArtist;
			rec.trackDifficulty = difficulty;

			//Override XML file with new vals
			stream = new FileStream(selectedTrackPath, FileMode.Create);
			serializer.Serialize(stream, rec);
			stream.Close();

			UploadRecording(rec, selectedTrackPath, xmlName, songName, songArtist, difficulty, selectedCoverPath, coverName);
		}
	}

	async void UploadRecording(Recording recording, string xmlPath, string xmlName, string songName, string songArtist, string difficulty, string coverPath, string coverName)
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

		//Upload song
		//FILL HERE ONCE SONG FILE HAS BEEN TIED TO XML

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

		TrackEntity trackEntity = new TrackEntity(xmlName, username, songName, songArtist, coverName, difficulty);
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

	IEnumerator WaitForSubs() //BROKEN NEEDS TO BE REMADE
	{
		while (getSubAsyncsRunning > 0) { yield return new WaitForSeconds(0.1f); }

		foreach (CloudFile file in recordings)
		{
			string fileName = file.Name;
			string songName = "";
			string artistName = "";
			string difficulty = "";
			string trackArtist = "";
			Debug.Log("Item: " + songName + " " + artistName + " " + difficulty + " " + trackArtist);

			PopulateWorkshop(null, songName, artistName, difficulty, null);
			//GET VALUES ABOVE BY READING FILE NAME AND SEARCHING FOR '|' DIVIDING SONG NAME | SONG ARITST | DIFFICULTY | TRACK ARTIST
		}
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
}

public class TrackEntity : TableEntity
{
	// Your entity type must expose a parameter-less constructor
	public TrackEntity() { }

	// Define the PK and RK
	public TrackEntity(string xmlName, string xmlArtist, string songName, string songArtist, string coverName, string difficulty)
	{
		this.PartitionKey = xmlName;
		this.RowKey = xmlArtist;
		SongName = songName;
		SongArtist = songArtist;
		CoverName = coverName;
		Difficulty = difficulty;
	}

	public string SongName { get; set; }
	public string SongArtist { get; set; }
	public string CoverName { get; set; }
	public string Difficulty { get; set; }
}