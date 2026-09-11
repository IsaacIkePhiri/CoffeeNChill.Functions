using Azure.Data.Tables;
using CoffeeNChill.Functions.Models;

namespace CoffeeNChill.Functions.Services;

public class MenuTableService
{
    private readonly TableClient _tableClient;

    public MenuTableService()
    {
        var connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "AzureWebJobsStorage is not configured.");
        }

        _tableClient = new TableClient(connectionString, "MenuItems");

        _tableClient.CreateIfNotExists();
    }

    public async Task CreateAsync(MenuItem item)
    {
        var entity = new TableEntity(item.PartitionKey, item.RowKey)
        {
            ["Name"] = item.Name,
            ["Description"] = item.Description,
            ["Price"] = item.Price,
            ["IsAvailable"] = item.IsAvailable
        };

        await _tableClient.AddEntityAsync(entity);
    }

    public async Task<List<MenuItem>> GetAllAsync()
    {
        var items = new List<MenuItem>();

        await foreach (var entity in _tableClient.QueryAsync<TableEntity>())
        {
            items.Add(ConvertToMenuItem(entity));
        }

        return items;
    }

    public async Task<List<MenuItem>> GetByCategoryAsync(string category)
    {
        var items = new List<MenuItem>();

        await foreach (var entity in _tableClient.QueryAsync<TableEntity>(
            x => x.PartitionKey == category))
        {
            items.Add(ConvertToMenuItem(entity));
        }

        return items;
    }

    public async Task<MenuItem?> GetByIdAsync(string category, string id)
    {
        try
        {
            var response = await _tableClient.GetEntityAsync<TableEntity>(
                category,
                id);

            return ConvertToMenuItem(response.Value);
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    public async Task UpdateAsync(MenuItem item)
    {
        var entity = new TableEntity(item.PartitionKey, item.RowKey)
        {
            ["Name"] = item.Name,
            ["Description"] = item.Description,
            ["Price"] = item.Price,
            ["IsAvailable"] = item.IsAvailable
        };

        await _tableClient.UpsertEntityAsync(
            entity,
            TableUpdateMode.Replace);
    }

    public async Task DeleteAsync(string category, string id)
    {
        await _tableClient.DeleteEntityAsync(category, id);
    }

    private static MenuItem ConvertToMenuItem(TableEntity entity)
    {
        return new MenuItem
        {
            PartitionKey = entity.PartitionKey,
            RowKey = entity.RowKey,
            Name = entity.GetString("Name") ?? string.Empty,
            Description = entity.GetString("Description") ?? string.Empty,
            Price = entity.GetDouble("Price") ?? 0,
            IsAvailable = entity.GetBoolean("IsAvailable") ?? false
        };
    }
}