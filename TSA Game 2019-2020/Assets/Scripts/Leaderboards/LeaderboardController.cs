using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using UnityEngine;
using TMPro;
using System.Collections;

public class LeaderboardController : MonoBehaviour
{
	public string connectionString;

	public CloudStorageAccount StorageAccount;

	private void Awake()
	{
		StorageAccount = CloudStorageAccount.Parse(connectionString);
	}

	public async void AddToLeaderboard(string tableName, string player, float score)
	{
		Debug.Log("--Adding Score Table: " + tableName + "--");
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

		ScoreEntity scoreEntity = new ScoreEntity(player, score);
		await InsertOrMergeEntityAsync(table, scoreEntity);
		Debug.Log("Score Added: " + player + " | " + score);
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
		ScoreEntity insertedCustomer = result.Result as ScoreEntity;
		return insertedCustomer;
	}

	private async Task<ScoreEntity> RetrieveEntityUsingPointQueryAsync(CloudTable table, string partitionKey, string rowKey)
	{
		TableOperation retrieveOperation = TableOperation.Retrieve<ScoreEntity>(partitionKey, rowKey);
		TableResult result = await table.ExecuteAsync(retrieveOperation);
		ScoreEntity score = result.Result as ScoreEntity;
		if (score != null)
		{
			Debug.Log(table.Name + ": " + score.PartitionKey + " | " + score.RowKey);
		}

		return score;
	}

	private async Task DeleteEntityAsync(CloudTable table, ScoreEntity deleteEntity)
	{
		if (deleteEntity == null)
		{
			throw new ArgumentNullException("deleteEntity");
		}

		TableOperation deleteOperation = TableOperation.Delete(deleteEntity);
		await table.ExecuteAsync(deleteOperation);
	}

	private async Task PartitionScanAsync(CloudTable table, string partitionKey)
	{
		TableQuery<ScoreEntity> partitionScanQuery = new TableQuery<ScoreEntity>().Where
			(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionKey));

		TableContinuationToken token = null;
		// Page through the results
		do
		{
			TableQuerySegment<ScoreEntity> segment = await table.ExecuteQuerySegmentedAsync(partitionScanQuery, token);
			token = segment.ContinuationToken;
			foreach (ScoreEntity entity in segment)
			{
				Debug.Log(table.Name + ": " + entity.PartitionKey + " | " + entity.RowKey);
			}
		}
		while (token != null);
	}
}

public class ScoreEntity : TableEntity
{
	// Your entity type must expose a parameter-less constructor
	public ScoreEntity() { }

	// Define the PK and RK
	public ScoreEntity(string name, float score)
	{
		this.PartitionKey = name;
		this.RowKey = "" + score;
	}
}
