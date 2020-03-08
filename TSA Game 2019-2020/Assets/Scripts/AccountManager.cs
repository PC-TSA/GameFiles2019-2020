using System;
using System.Collections.Generic;
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

    public AccountData tempAccountData;

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

    public async void AddInvItem(string username, int itemID)
    {
        Debug.Log("Adding item: " + itemID + " to username " + username);
        CloudTableClient tableClient = StorageAccount.CreateCloudTableClient();

        // Create a table client for interacting with the table service 
        CloudTable table = tableClient.GetTableReference("AccountData");

        //Update current inventory by adding this id to the end of the string
        TableOperation retrieve = TableOperation.Retrieve<AccountData>(username, username);
        TableResult result = await table.ExecuteAsync(retrieve);
        AccountData dat = (AccountData)result.Result;
        if (dat.inventory == null || dat.inventory.Length == 0)
            dat.inventory += itemID;
        else
            dat.inventory += "_" + itemID;
        Debug.Log("New inventory: " + dat.inventory);
        TableOperation replace = TableOperation.InsertOrReplace(dat);
        await table.ExecuteAsync(replace);
    }

    public async void RemoveInvItem(string username, int itemID)
    {
        Debug.Log("Removing item: " + itemID + " from username " + username);
        CloudTableClient tableClient = StorageAccount.CreateCloudTableClient();

        // Create a table client for interacting with the table service 
        CloudTable table = tableClient.GetTableReference("AccountData");

        TableOperation retrieve = TableOperation.Retrieve<AccountData>(username, username);
        TableResult result = await table.ExecuteAsync(retrieve);
        AccountData dat = (AccountData)result.Result;

        List<int> inv = InvToInt(dat.inventory);
        inv.Remove(itemID);
        dat.inventory = InvToString(inv);

        Debug.Log("New inventory: " + dat.inventory);
        TableOperation replace = TableOperation.InsertOrReplace(dat);
        await table.ExecuteAsync(replace);
    }

    public async void GetAccountData(string username)
    {
        Debug.Log("Pulling data from: " + username);
        CloudTableClient tableClient = StorageAccount.CreateCloudTableClient();

        // Create a table client for interacting with the table service 
        CloudTable table = tableClient.GetTableReference("AccountData");

        TableOperation retrieve = TableOperation.Retrieve<AccountData>(username, username);
        TableResult result = await table.ExecuteAsync(retrieve);
        AccountData dat = (AccountData)result.Result;

        tempAccountData = dat;
    }

    List<int> InvToInt(string inv)
    {
        List<int> list = new List<int>();
        string[] c = inv.Split('_');
        foreach (string s in c)
            list.Add(int.Parse(s));
        return list;
    }

    string InvToString(List<int> inv)
    {
        string newInvStr = "";
        foreach (int i in inv)
        {
            if (newInvStr.Length == 0)
                newInvStr += i;
            else
                newInvStr += "_" + i;
        }
        return newInvStr;
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

        //Get current table size by retrieving entity with partitionKey & rowKey "TrackCounter" 
        User newUser = new User(username, hash);
        TableOperation insert = TableOperation.Insert(newUser);
        await table.ExecuteAsync(insert);

        // Create a table client for interacting with the table service 
        table = tableClient.GetTableReference("AccountData");

        //Get current table size by retrieving entity with partitionKey & rowKey "TrackCounter" 
        AccountData newData = new AccountData(username);
        insert = TableOperation.Insert(newData);
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

public class AccountData : TableEntity
{
    // Your entity type must expose a parameter-less constructor
    public AccountData() { }

    public AccountData(string username)
    {
        this.PartitionKey = username;
        this.RowKey = username;
    }

    public AccountData(string username, string inventory)
    {
        this.PartitionKey = username;
        this.RowKey = username;
        this.inventory = inventory;
    }

    public string inventory { get; set; }

    public string equiped { get; set; }

    public bool isFemale { get; set; }
}
