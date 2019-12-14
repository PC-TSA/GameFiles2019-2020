using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.File;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class WorkshopController : MonoBehaviour
{	
	public string connectionString;

	public string shareName = "workshop";

	public int getSubAsyncsRunning = 0; //While this is > 0 ListAll will wait for these to finish before listing

	public List<CloudFile> recordings;

	public GameObject workshopUI;
	public GameObject workshopContentObj;
	public GameObject workshopItemPrefab;

	public CloudStorageAccount StorageAccount;

	public GameObject loadingBar;
	public List<GameObject> loadingTextPeriods;

	private void Awake()
	{
		StorageAccount = CloudStorageAccount.Parse(connectionString);
	}

	private void Start()
	{
		OpenWorkshop();
	}

	public void PopulateWorkshop(string s)
	{
		GameObject item = Instantiate(workshopItemPrefab, workshopContentObj.transform);
		item.transform.GetChild(1).GetComponent<TMP_Text>().text = s;
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
				GetSubs((CloudFileDirectory) listItem); //Get subdirectories
			else
				recordings.Add((CloudFile) listItem);
		}

		StartCoroutine(WaitForSubs());

		Debug.Log("--Listing Complete--");
	}

	public async void UploadRecording(string directory, string filePath, string fileName)
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
		CloudFileDirectory dir = root.GetDirectoryReference(directory);
		await dir.CreateIfNotExistsAsync();

		// Uploading a local file to the directory created above 
		CloudFile file = dir.GetFileReference(fileName);

#if WINDOWS_UWP && ENABLE_DOTNET
		StorageFolder storageFolder = await StorageFolder.GetFolderFromPathAsync(Application.streamingAssetsPath.Replace('/', '\\'));
		StorageFile sf = await storageFolder.GetFileAsync(ImageToUpload);
		await file.UploadFromFileAsync(sf);
#else
		await file.UploadFromFileAsync(filePath);
#endif

		Debug.Log("--Upload Complete--"); 
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
					recordings.Add((CloudFile) listItem);
			}
		}
		while (token != null);
		getSubAsyncsRunning--;
	}

	IEnumerator WaitForSubs()
	{
		while (getSubAsyncsRunning > 0) { yield return new WaitForSeconds(0.1f); }

		foreach (CloudFile file in recordings)
		{
			Debug.Log(file.Name + " | " + file.Uri);
			PopulateWorkshop(file.Name.Substring(0, file.Name.Length - 4));
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
}