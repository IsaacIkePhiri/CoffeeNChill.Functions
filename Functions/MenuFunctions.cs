using System.Net;
using System.Text.Json;
using CoffeeNChill.Functions.Models;
using CoffeeNChill.Functions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace CoffeeNChill.Functions.Functions;

public class MenuFunctions
{
    private readonly MenuTableService _menuTableService;

    public MenuFunctions(MenuTableService menuTableService)
    {
        _menuTableService = menuTableService;
    }

    [Function("CreateMenuItem")]
    public async Task<HttpResponseData> CreateMenuItem(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "menu")]
        HttpRequestData req)
    {
        try
        {
            var item = await JsonSerializer.DeserializeAsync<MenuItem>(
                req.Body,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            if (item == null)
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync(
                    "Invalid menu item data.");
                return badResponse;
            }

            if (string.IsNullOrWhiteSpace(item.PartitionKey) ||
                string.IsNullOrWhiteSpace(item.RowKey) ||
                string.IsNullOrWhiteSpace(item.Name))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync(
                    "Category, ID and Name are required.");
                return badResponse;
            }

            await _menuTableService.CreateAsync(item);

            var response = req.CreateResponse(HttpStatusCode.Created);
            await response.WriteAsJsonAsync(item);

            return response;
        }
        catch (Exception ex)
        {
            var response = req.CreateResponse(
                HttpStatusCode.InternalServerError);

            await response.WriteStringAsync(
                $"Error creating menu item: {ex.Message}");

            return response;
        }
    }

    [Function("GetAllMenuItems")]
    public async Task<HttpResponseData> GetAllMenuItems(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "menu")]
        HttpRequestData req)
    {
        try
        {
            var items = await _menuTableService.GetAllAsync();

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(items);

            return response;
        }
        catch (Exception ex)
        {
            var response = req.CreateResponse(
                HttpStatusCode.InternalServerError);

            await response.WriteStringAsync(
                $"Error retrieving menu items: {ex.Message}");

            return response;
        }
    }

    [Function("GetMenuItemsByCategory")]
    public async Task<HttpResponseData> GetMenuItemsByCategory(
        [HttpTrigger(
            AuthorizationLevel.Anonymous,
            "get",
            Route = "menu/category/{category}")]
        HttpRequestData req,
        string category)
    {
        try
        {
            var items = await _menuTableService.GetByCategoryAsync(category);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(items);

            return response;
        }
        catch (Exception ex)
        {
            var response = req.CreateResponse(
                HttpStatusCode.InternalServerError);

            await response.WriteStringAsync(
                $"Error retrieving menu items by category: {ex.Message}");

            return response;
        }
    }

    [Function("UpdateMenuItem")]
    public async Task<HttpResponseData> UpdateMenuItem(
        [HttpTrigger(
            AuthorizationLevel.Anonymous,
            "put",
            Route = "menu/{category}/{id}")]
        HttpRequestData req,
        string category,
        string id)
    {
        try
        {
            var item = await JsonSerializer.DeserializeAsync<MenuItem>(
                req.Body,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            if (item == null)
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync(
                    "Invalid menu item data.");
                return badResponse;
            }

            item.PartitionKey = category;
            item.RowKey = id;

            if (string.IsNullOrWhiteSpace(item.Name))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync(
                    "Name is required.");
                return badResponse;
            }

            var existingItem = await _menuTableService.GetByIdAsync(
                category,
                id);

            if (existingItem == null)
            {
                var notFoundResponse = req.CreateResponse(
                    HttpStatusCode.NotFound);

                await notFoundResponse.WriteStringAsync(
                    "Menu item not found.");

                return notFoundResponse;
            }

            await _menuTableService.UpdateAsync(item);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(item);

            return response;
        }
        catch (Exception ex)
        {
            var response = req.CreateResponse(
                HttpStatusCode.InternalServerError);

            await response.WriteStringAsync(
                $"Error updating menu item: {ex.Message}");

            return response;
        }
    }

    [Function("DeleteMenuItem")]
    public async Task<HttpResponseData> DeleteMenuItem(
        [HttpTrigger(
            AuthorizationLevel.Anonymous,
            "delete",
            Route = "menu/{category}/{id}")]
        HttpRequestData req,
        string category,
        string id)
    {
        try
        {
            var existingItem = await _menuTableService.GetByIdAsync(
                category,
                id);

            if (existingItem == null)
            {
                var notFoundResponse = req.CreateResponse(
                    HttpStatusCode.NotFound);

                await notFoundResponse.WriteStringAsync(
                    "Menu item not found.");

                return notFoundResponse;
            }

            await _menuTableService.DeleteAsync(category, id);

            var response = req.CreateResponse(
                HttpStatusCode.NoContent);

            return response;
        }
        catch (Exception ex)
        {
            var response = req.CreateResponse(
                HttpStatusCode.InternalServerError);

            await response.WriteStringAsync(
                $"Error deleting menu item: {ex.Message}");

            return response;
        }
    }
}