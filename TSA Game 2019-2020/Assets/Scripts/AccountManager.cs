using System;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using UnityEngine;
using System.Text;
using System.Security.Cryptography;
using TMPro;

public class AccountManager : MonoBehaviour
{
    public MainMenuController mainMenuController;

    public string shareName = "workshop";
    public NetworkingUtilities networkingUtilities;
    public CloudStorageAccount StorageAccount;

    User tempUser;

    public TMP_InputField loginUsernameInput;
    public TMP_InputField loginPasswordInput;
    public TMP_InputField signUpUsernameInput;
    public TMP_InputField signUpPasswordInput;

    // Start is called before the first frame update
    void Start()
    {
        StorageAccount = networkingUtilities.SetStorageAccount();
    }

    public void Login()
    {
        RunLogin(loginUsernameInput.text, loginPasswordInput.text);
    }

    public void AddUser()
    {
        RunAddUser(signUpUsernameInput.text, signUpPasswordInput.text);
    }

    public void ContinueOffline()
    {
        mainMenuController.SpawnSplashTitle("Online Features Disabled", Color.red);
        mainMenuController.LoadMainUI();
    }

    async void RunLogin(string username, string password)
    {
        await GetUser(username);

        if (tempUser != null)
        { 
            using (SHA256 sha256Hash = SHA256.Create())
            {
                string hash = GetHash(sha256Hash, password);
                if (hash == tempUser.RowKey)
                {
                    PlayerPrefs.SetString("username", tempUser.PartitionKey);
                    mainMenuController.SpawnSplashTitle("Login Successfull", Color.green);
                    CrossSceneController.isOnline = true;
                    mainMenuController.LoadMainUI();
                }
                else
                    mainMenuController.SpawnSplashTitle("Incorrect Password", Color.red);
            }
        }
        else
            mainMenuController.SpawnSplashTitle("User Not Found", Color.red);
    }

    private static string GetHash(HashAlgorithm hashAlgorithm, string input)
    {
        // Convert the input string to a byte array and compute the hash.
        byte[] data = hashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(input));

        // Create a new Stringbuilder to collect the bytes
        // and create a string.
        var sBuilder = new StringBuilder();

        // Loop through each byte of the hashed data 
        // and format each one as a hexadecimal string.
        for (int i = 0; i < data.Length; i++)
        {
            sBuilder.Append(data[i].ToString("x2"));
        }

        // Return the hexadecimal string.
        return sBuilder.ToString();
    }

    private async Task GetUser(string username)
    {
        tempUser = null;
        TableQuery<User> partitionScanQuery = new TableQuery<User>().Where
            (TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, username));

        CloudTableClient tableClient = StorageAccount.CreateCloudTableClient();
        CloudTable table = tableClient.GetTableReference("Users");

        TableContinuationToken token = null;

        // Page through the results
        do
        {
            TableQuerySegment<User> segment = await table.ExecuteQuerySegmentedAsync(partitionScanQuery, token);
            token = segment.ContinuationToken;
            foreach (User user in segment)
            {
                tempUser = user;
                break;
            }
        }
        while (token != null);
    }

    public async void RunAddUser(string username, string password)
    {
        //Check if username & password were filled out
        if(username.Length == 0 || password.Length == 0)
        {
            mainMenuController.SpawnSplashTitle("Missing username/password", Color.red);
            return;
        }
        //Check if user exists
        await GetUser(username);
        if(tempUser != null)
        {
            mainMenuController.SpawnSplashTitle("User Already Exists", Color.red);
            return;
        }

        string hash = "";
        using (SHA256 sha256Hash = SHA256.Create())
        {
            hash = GetHash(sha256Hash, password);
        }

        CloudTableClient tableClient = StorageAccount.CreateCloudTableClient();

        // Create a table client for interacting with the table service 
        CloudTable table = tableClient.GetTableReference("Users");
        try
        {
            await table.CreateIfNotExistsAsync();
        }
        catch (StorageException)
        {
            throw;
        }

        //Get current table size by retrieving entity with partitionKey & rowKey "TrackCounter" 
        User newUser = new User(username, hash);
        TableOperation insert = TableOperation.Insert(newUser);
        await table.ExecuteAsync(insert);

        PlayerPrefs.SetString("username", username);
        mainMenuController.SpawnSplashTitle("User Created and Logged In", Color.green);
        CrossSceneController.isOnline = true;
        mainMenuController.LoadMainUI();
    }
}

public class User : TableEntity
{
    // Your entity type must expose a parameter-less constructor
    public User() { }

    // Define the PK and RK
    public User(string username, string passwordHash)
    {
        this.PartitionKey = username;
        this.RowKey = passwordHash;
    }
}
