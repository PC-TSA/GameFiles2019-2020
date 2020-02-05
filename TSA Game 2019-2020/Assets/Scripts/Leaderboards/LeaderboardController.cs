using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using UnityEngine;
using TMPro;
using System.Collections;
using Random = UnityEngine.Random;

public class LeaderboardController : MonoBehaviour
{
	public string connectionString;

	public CloudStorageAccount StorageAccount;

	public List<ScoreEntity> tempTableResult;

	private void Awake()
	{
		StorageAccount = CloudStorageAccount.Parse(connectionString);
		//PopulateTest("gabrieltm8Frame", 100);
	}

	void PopulateTest(string tableName, int count)
	{
		for(int i = 0; i < count; i++)
		{
			AddToLeaderboard(tableName, "" + Random.Range(-100, 0), Random.Range(0, 1000), (float)(Random.Range(0, 1000) / 10), "SS");
		}
	}

	public async void AddToLeaderboard(string tableName, string player, float score, float accuracy, string rank)
	{
		Debug.Log("--Adding Score Table: " + tableName + "--");
		CloudTableClient tableClient = StorageAccount.CreateCloudTableClient();

		// Create a table client for interacting with the table service 
		CloudTable table = tableClient.GetTableReference(tableName);

		try
		{
			await table.CreateIfNotExistsAsync();
		}
		catch (StorageException)
		{
			throw;
		}

		ScoreEntity scoreEntity = new ScoreEntity(player, score, accuracy, rank);
		await InsertOrMergeEntityAsync(table, scoreEntity);
		Debug.Log("Score Added: " + player + " | " + score);
	}

	public async void NewLeaderboard(string tableName)
	{
		Debug.Log("--New Table: " + tableName + "--");
		CloudTableClient tableClient = StorageAccount.CreateCloudTableClient();

		// Create a table client for interacting with the table service 
		CloudTable table = tableClient.GetTableReference(tableName);

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

	private async Task<ScoreEntity> InsertOrMergeEntityAsync(CloudTable table, ScoreEntity entity)
	{
		if (entity == null)
		{
			throw new ArgumentNullException("entity");
		}

		// Create the InsertOrReplace  TableOperation
		TableOperation insertOrMergeOperation = TableOperation.InsertOrMerge(entity);

		// Execute the operation.
		TableResult result = await table.ExecuteAsync(insertOrMergeOperation);
		ScoreEntity insertedScore = result.Result as ScoreEntity;
		return insertedScore;
	}

	public async Task<ScoreEntity> GetEntityFromTable(string tableName, string partitionKey, string rowKey)
	{
		TableOperation retrieveOperation = TableOperation.Retrieve<ScoreEntity>(partitionKey, rowKey);
		CloudTableClient tableClient = StorageAccount.CreateCloudTableClient();
		CloudTable table = tableClient.GetTableReference(tableName);

		TableResult result = await table.ExecuteAsync(retrieveOperation);
		ScoreEntity scoreEntity = result.Result as ScoreEntity;
		return scoreEntity;
	}

	public async Task DeleteEntityAsync(CloudTable table, ScoreEntity deleteEntity)
	{
		if (deleteEntity == null)
		{
			throw new ArgumentNullException("deleteEntity");
		}

		TableOperation deleteOperation = TableOperation.Delete(deleteEntity);
		await table.ExecuteAsync(deleteOperation);
	}

	public async Task<List<ScoreEntity>> GetLeaderboardTable(string tableName)
	{
		await PartitionScanAsync(tableName); //Pulls entire leaderboard table into tempTableResult list
		return tempTableResult;
	}

	public async Task PartitionScanAsync(string tableName)
	{
		tempTableResult = new List<ScoreEntity>();
		TableQuery<ScoreEntity> partitionScanQuery = new TableQuery<ScoreEntity>();
		CloudTableClient tableClient = StorageAccount.CreateCloudTableClient();

		// Create a table client for interacting with the table service 
		CloudTable table = tableClient.GetTableReference(tableName);

		TableContinuationToken token = null;
		// Page through the results
		try
		{
			do
			{
				TableQuerySegment<ScoreEntity> segment = await table.ExecuteQuerySegmentedAsync(partitionScanQuery, token);
				token = segment.ContinuationToken;
				foreach (ScoreEntity entity in segment)
				{
					tempTableResult.Add(entity);
				}
			}
			while (token != null);
		}
		catch
		{
			Debug.LogError("Couldnt pull leaderboard table '" + tableName + "'");
			throw;
		}
	}
}

public class ScoreEntity : TableEntity
{
	public string score { get; set; }
	public string accuracy { get; set; }
	public string rank { get; set; }

	public ScoreEntity() { }

	public ScoreEntity(string name, float score, float accuracy, string rank)
	{
		this.PartitionKey = "Player";
		this.RowKey = name;
		this.score = "" + score;
		this.accuracy = "" + accuracy;
		this.rank = rank;
	}
}
