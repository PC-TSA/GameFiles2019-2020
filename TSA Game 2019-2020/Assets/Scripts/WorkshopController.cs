using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.File;
using UnityEngine;

public class WorkshopController : BaseStorage
{
    public string workshopShare = "workshop";
    public string dir = "";
    public string path = "";
    public string name = "";
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
}