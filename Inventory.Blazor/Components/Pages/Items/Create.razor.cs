using Microsoft.AspNetCore.Components;
using System.Text.Json;
using Inventory.Shared.Dtos.Items;

namespace Inventory.Blazor.Components.Pages.Items;

public partial class Create : ComponentBase
{
    [Inject] public required NavigationManager Navigation { get; set; }
    [Inject] public required HttpClient Http { get; set; }

    private CreateItemDto createDto = new();
    private bool isSaving = false;
    private string? saveMessage;
    private string? errorTitle;
    private bool saveSuccess = false;
    private Dictionary<string, List<string>> validationErrors = new();

    private async Task HandleSubmit()
    {
        if (!IsFormValid() || isSaving)
            return;

        isSaving = true;
        saveMessage = null;
        errorTitle = null;
        validationErrors.Clear();
        StateHasChanged();

        try
        {
            Console.WriteLine("BLAZOR.ITEM.CREATE.SAVE: Creating new item: {0}", createDto.Name);

            var json = JsonSerializer.Serialize(createDto);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var response = await Http.PostAsync("api/items", content);

            if (response.IsSuccessStatusCode)
            {
                var responseJson = await response.Content.ReadAsStringAsync();
                var createdItem = JsonSerializer.Deserialize<ItemDto>(responseJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (createdItem != null)
                {
                    saveMessage = $"Item '{createdItem.Name}' created successfully with ID {createdItem.Id}!";
                    saveSuccess = true;
                    Console.WriteLine("BLAZOR.ITEM.CREATE.SAVE.SUCCESS: Item {0} created with ID {1}", createdItem.Name, createdItem.Id);

                    // Reset form for next item creation
                    createDto = new CreateItemDto();

                    // Navigate to the created item after a brief delay
                    _ = Task.Delay(2000).ContinueWith(_ =>
                    {
                        InvokeAsync(() =>
                        {
                            Navigation.NavigateTo($"/items/{createdItem.Id}");
                        });
                    });
                }
            }
            else
            {
                var errorResponse = await response.Content.ReadAsStringAsync();
                saveMessage = ExtractErrorMessage(errorResponse, response.StatusCode);
                saveSuccess = false;
                Console.WriteLine("BLAZOR.ITEM.CREATE.SAVE.FAILED: Failed to create item {0} - Status: {1}", createDto.Name,
                response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            saveMessage = $"Error creating item: {ex.Message}";
            saveSuccess = false;
            Console.WriteLine("BLAZOR.ITEM.CREATE.SAVE.ERROR: {0}", ex.Message);
        }
        finally
        {
            isSaving = false;
            StateHasChanged();
        }
    }

    private void GoBack()
    {
        Console.WriteLine("BLAZOR.ITEM.CREATE.NAVIGATION: Navigating back to items list");
        Navigation.NavigateTo("/items");
    }

    // Validation and styling helper methods
    private bool IsFormValid()
    {
        return !string.IsNullOrWhiteSpace(createDto.Name) &&
        !string.IsNullOrWhiteSpace(createDto.Unit) &&
        createDto.Name.Length <= 200 &&
        createDto.Unit.Length <= 50 &&
        (createDto.Description?.Length ?? 0) <= 1000 &&
        createDto.InitialQuantity >= 0;
    }

    private string? GetValidationError(string field)
    {
        if (validationErrors.ContainsKey(field))
        {
            return string.Join(", ", validationErrors[field]);
        }
        return null;
    }

    private string GetInputClass(string field)
    {
        var hasError = GetValidationError(field) != null;
        return hasError ? "border-red-300 focus:ring-red-500 focus:border-red-500" : "border-gray-300";
    }

    private string GetSaveButtonClass()
    {
        if (isSaving || !IsFormValid())
        {
            return "bg-gray-400 text-white cursor-not-allowed";
        }
        return "bg-blue-600 text-white hover:bg-blue-700";
    }

    // Helper method to extract structured error messages from API responses
    private string ExtractErrorMessage(string errorResponse, System.Net.HttpStatusCode statusCode)
    {
        errorTitle = null; // Reset error title

        try
        {
            var errorResult = JsonSerializer.Deserialize<JsonElement>(errorResponse);

            // Check for validation errors first (400/422 responses with field-level errors)
            if ((statusCode == System.Net.HttpStatusCode.BadRequest || statusCode == System.Net.HttpStatusCode.UnprocessableEntity)
                && errorResult.TryGetProperty("errors", out var errorsElement))
            {
                validationErrors = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(errorsElement.GetRawText()) ?? new();
                errorTitle = "Validation Error";
                return "Please fix the validation errors below";
            }

            // Extract structured error message (title and detail)
            string title = "";
            string detail = "";

            if (errorResult.TryGetProperty("title", out var titleElement))
            {
                title = titleElement.GetString() ?? "";
            }

            if (errorResult.TryGetProperty("detail", out var detailElement))
            {
                detail = detailElement.GetString() ?? "";
            }

            // Set the error title if available
            if (!string.IsNullOrEmpty(title))
            {
                errorTitle = title;
            }

            // Use detail as primary message, fallback to title, then generic message
            if (!string.IsNullOrEmpty(detail))
            {
                return detail;
            }
            else if (!string.IsNullOrEmpty(title))
            {
                return title;
            }
            else
            {
                errorTitle = $"Error {(int)statusCode}";
                return $"Failed to create item: {statusCode}";
            }
        }
        catch
        {
            // If JSON parsing fails, return the raw response or a generic message
            errorTitle = $"Error {(int)statusCode}";
            return !string.IsNullOrEmpty(errorResponse) ? errorResponse : $"Failed to create item: {statusCode}";
        }
    }
}
