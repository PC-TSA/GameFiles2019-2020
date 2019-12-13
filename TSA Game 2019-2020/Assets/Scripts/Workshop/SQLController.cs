using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Data;
using System.Data.Sql;
using System.Data.SqlClient;
using UnityEngine;
using TMPro;
using System.Collections;

public class SQLController : MonoBehaviour
{
    private string dbPath;

    public bool isWorkshopOpen;

    string connectionString = "Data Source=tritonal.database.windows.net,1433; Database=TriTonalSQL; User ID=client; Password=z+0+8ZtCm6vPNXnPnixbslEpoSk1Sj8gzPm8wq3idH8";

    public string shareName = "workshop";

    public int getSubAsyncsRunning = 0; //While this is > 0 ListAll will wait for these to finish before listing

    public GameObject workshopUI;
    public GameObject workshopContentObj;
    public GameObject workshopItemPrefab;

    private void Start()
    {
        //Open connection
        SqlConnection conn = new SqlConnection(connectionString);
        conn.Open();
        Debug.Log("Connection Open");

        SqlCommand command;
        SqlDataReader reader;
        SqlDataAdapter adapter = new SqlDataAdapter();

        string commandString = "CREATE TABLE Test (id int, name text)";

        //Create table
        command = new SqlCommand(commandString, conn);
        //reader = command.ExecuteReader();
        adapter.InsertCommand = new SqlCommand(commandString, conn);
        adapter.InsertCommand.ExecuteNonQuery();

        conn.Close();
    }

    public void CreateSchema()
    {
        using (var conn = new SqlConnection(dbPath))
        {
            conn.Open();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = "CREATE TABLE IF NOT EXISTS 'high_score' ( " +
                                  "  'id' INTEGER PRIMARY KEY, " +
                                  "  'name' TEXT NOT NULL, " +
                                  "  'score' INTEGER NOT NULL" +
                                  ");";

                var result = cmd.ExecuteNonQuery();
                Debug.Log("create schema: " + result);
            }
        }
    }

    public void InsertScore(string highScoreName, int score)
    {
        using (var conn = new SqlConnection(dbPath))
        {
            conn.Open();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = "INSERT INTO high_score (name, score) " +
                                  "VALUES (@Name, @Score);";

                cmd.Parameters.Add(new SqlParameter
                {
                    ParameterName = "Name",
                    Value = highScoreName
                });

                cmd.Parameters.Add(new SqlParameter
                {
                    ParameterName = "Score",
                    Value = score
                });

                var result = cmd.ExecuteNonQuery();
                Debug.Log("insert score: " + result);
            }
        }
    }

    public void GetHighScores(int limit)
    {
        using (var conn = new SqlConnection(dbPath))
        {
            conn.Open();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = "SELECT * FROM high_score ORDER BY score DESC LIMIT @Count;";

                cmd.Parameters.Add(new SqlParameter
                {
                    ParameterName = "Count",
                    Value = limit
                });

                Debug.Log("scores (begin)");
                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var id = reader.GetInt32(0);
                    var highScoreName = reader.GetString(1);
                    var score = reader.GetInt32(2);
                    var text = string.Format("{0}: {1} [#{2}]", highScoreName, score, id);
                    Debug.Log(text);
                }
                Debug.Log("scores (end)");
            }
        }
    }

	private void Awake()
	{

	}

	public void PopulateWorkshop(string s)
	{
		GameObject item = Instantiate(workshopItemPrefab, workshopContentObj.transform);
		item.transform.GetChild(1).GetComponent<TMP_Text>().text = s;
	}

	public void ToggleWorkshop()
	{
		isWorkshopOpen = !isWorkshopOpen;
		if (isWorkshopOpen)
			OpenWorkshop();
		else
			workshopUI.SetActive(false);
	}

	void OpenWorkshop()
	{
		workshopUI.SetActive(true);
		for (int i = 0; i < workshopContentObj.transform.childCount; i++) //Clear content
			Destroy(workshopContentObj.transform.GetChild(i).gameObject);
		ListAll();
	}

	public void ListAll()
	{
		Debug.Log("--Starting Listing--");

		Debug.Log("--Listing Complete--");
	}

	public void UploadRecording(string directory, string filePath, string fileName)
	{
		Debug.Log("--Starting Upload--");

		Debug.Log("--Upload Complete--"); 
	}
}