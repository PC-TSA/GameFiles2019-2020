using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.File;
using Microsoft.WindowsAzure.Storage.Table;

public class NetworkingUtilities : MonoBehaviour
{
	public string connectionString;
	public string shareName = "workshop";

	public CloudStorageAccount StorageAccount;
	private void Awake()
	{
		StorageAccount = SetStorageAccount();
	}

	public CloudStorageAccount SetStorageAccount()
	{
		return CloudStorageAccount.Parse(connectionString);
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

	public async void NewLeaderboard(string tableName)
	{
		Debug.Log("--New Table: " + tableName + "--");
		CloudTableClient tableClient = StorageAccount.CreateCloudTableClient();

		// Create a table client for interacting with the table service 
		CloudTable table = tableClient.GetTableReference(tableName.Substring(0, tableName.Length - 4));

		try
		{
			await table.CreateIfNotExistsAsync();
		}
		catch (StorageException)
		{
			throw;
		}

		Debug.Log("--New Table Created--");
	}
}
