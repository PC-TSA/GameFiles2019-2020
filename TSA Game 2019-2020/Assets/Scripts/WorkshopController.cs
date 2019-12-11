using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.File;
using UnityEngine;
using TMPro;

public class WorkshopController : BaseStorage
{
    public string workshopShare = "workshop";
    public string dir = "";
    public string path = "";
    public string name = "";

    public GameObject workshopUI;
    public GameObject workshopContent;

    public GameObject workshopItemPrefab;

    private void Start()
    {
        ListAll();
    }

    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Equals))
            TestPopulate("test");    
    }

    public async void UploadRecording(string directory, string filePath, string fileName)
    {
        Debug.Log("--Starting Upload--");

        // Create a file client for interacting with the file service.
        CloudFileClient fileClient = StorageAccount.CreateCloudFileClient();

        // Create a share for organizing files and directories within the storage account.
        CloudFileShare share = fileClient.GetShareReference("workshop");
        try
        {
            await share.CreateIfNotExistsAsync();
        }
        catch (StorageException)
        {
            Debug.LogError("Storage Exception: Make sure storage account has storage file endpoint enabled and specified correctly in the app.config");
            throw;
        }

        // Get a reference to the root directory of the share.        
        CloudFileDirectory root = share.GetRootDirectoryReference();

        // Create a directory under the root directory 
        CloudFileDirectory dir = root.GetDirectoryReference(directory);
        await dir.CreateIfNotExistsAsync();

        // Uploading a local file to the directory created above 
        CloudFile file = dir.GetFileReference(fileName);

        await file.UploadFromFileAsync(filePath);

        Debug.Log("--Upload Complete--");
    }

    public void TestPopulate(string name)
    {
        GameObject obj = Instantiate(workshopItemPrefab, workshopContent.transform);
        obj.transform.GetChild(1).GetComponent<TMP_Text>().text = name;
    }

    public async void ListAll()
    {
        Debug.Log("--Starting Listing--");

        // Create a file client for interacting with the file service.
        CloudFileClient fileClient = StorageAccount.CreateCloudFileClient();

        // Create a share for organizing files and directories within the storage account.
        CloudFileShare share = fileClient.GetShareReference("workshop");

        // List all files/directories under the root directory
        List<IListFileItem> results = new List<IListFileItem>();
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
            Debug.Log(listItem.Uri.Segments[listItem.Uri.Segments.Length - 1]);
            Debug.Log(listItem.Uri.);
        }
    }
}