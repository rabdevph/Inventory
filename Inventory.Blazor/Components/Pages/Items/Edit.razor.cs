using Microsoft.AspNetCore.Components;
using System.Text.Json;
using Inventory.Shared.Dtos.Items;

namespace Inventory.Blazor.Components.Pages.Items;

public partial class Edit : ComponentBase
{
    [Parameter] public int Id { get; set; }

    [Inject] public required NavigationManager Navigation { get; set; }
    [Inject] public required HttpClient Http { get; set; }

    private ItemDto? item;
    private UpdateItemDto updateDto = new();
    private ItemDto? originalItem; // Store original values for comparison
    private bool isLoading = true;
    private bool isSaving = false;
    private string? errorMessage;
    private string? saveMessage;
    private string? errorTitle;
    private bool saveSuccess = false;
    private Dictionary<string, List<string>> validationErrors = new();
    private List<string> updatedFields = new(); // Track what fields were changed
    private DateTime? updateTimestamp; // Store consistent timestamp for display

    protected override async Task OnInitializedAsync()
    {
        await LoadItem();
    }

    private async Task LoadItem()
    {
        isLoading = true;
        errorMessage = null;
        StateHasChanged();

        try
        {
            Console.WriteLine("BLAZOR.ITEM.EDIT.LOAD: Loading item details for editing ID {0}", Id);
            var response = await Http.GetAsync($"api/items/{Id}");

            if (response.IsSuccessStatusCode)
            {
                var jsonString = await response.Content.ReadAsStringAsync();
                item = JsonSerializer.Deserialize<ItemDto>(jsonString, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (item != null)
                {
                    // Store original values for comparison
                    originalItem = JsonSerializer.Deserialize<ItemDto>(JsonSerializer.Serialize(item));

                    // Populate the form with current values
                    updateDto.Name = item.Name;
                    updateDto.Description = item.Description ?? string.Empty;
                    updateDto.Unit = item.Unit;

                    Console.WriteLine("BLAZOR.ITEM.EDIT.LOAD.SUCCESS: Loaded item {0} for editing", item.Name);
                }
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                errorMessage = $"Item with ID {Id} was not found.";
                Console.WriteLine("BLAZOR.ITEM.EDIT.LOAD.NOTFOUND: Item {0} not found", Id);
            }
            else
            {
                errorMessage = $"Failed to load item details: {response.StatusCode}";
                Console.WriteLine("BLAZOR.ITEM.EDIT.LOAD.FAILED: API returned status {0} for item {1}", response.StatusCode, Id);
            }
        }
        catch (Exception ex)
        {
            errorMessage = $"Error loading item details: {ex.Message}";
            Console.WriteLine("BLAZOR.ITEM.EDIT.LOAD.ERROR: {0}", ex.Message);
        }
        finally
        {
            isLoading = false;
            StateHasChanged();
        }
    }

    private async Task HandleSubmit()
    {
        if (!IsFormValid() || isSaving)
            return;

        isSaving = true;
        saveMessage = null;
        errorTitle = null;
        validationErrors.Clear();
        updatedFields.Clear(); // Clear previous changes
        StateHasChanged();

        try
        {
            Console.WriteLine("BLAZOR.ITEM.EDIT.SAVE: Saving changes for item ID {0}", Id);

            var json = JsonSerializer.Serialize(updateDto);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var response = await Http.PutAsync($"api/items/{Id}", content);

            if (response.IsSuccessStatusCode)
            {
                var responseJson = await response.Content.ReadAsStringAsync();
                var updatedItem = JsonSerializer.Deserialize<ItemDto>(responseJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (updatedItem != null)
                {
                    // Track what fields were changed
                    TrackChangedFields(originalItem, updatedItem);

                    item = updatedItem;
                    // Update original for future comparisons
                    originalItem = JsonSerializer.Deserialize<ItemDto>(JsonSerializer.Serialize(updatedItem));

                    // Create detailed success message
                    if (updatedFields.Any())
                    {
                        saveMessage = $"Item updated successfully! Changed: {string.Join(", ", updatedFields)}";
                    }
                    else
                    {
                        saveMessage = "Item saved successfully! No changes were made.";
                    }

                    saveSuccess = true;
                    Console.WriteLine("BLAZOR.ITEM.EDIT.SAVE.SUCCESS: Item {0} updated successfully - Fields changed: {1}",
                    item.Name, string.Join(", ", updatedFields));

                    // Store the update timestamp for consistent display
                    updateTimestamp = DateTime.Now;
                }
            }
            else
            {
                var errorResponse = await response.Content.ReadAsStringAsync();
                saveMessage = ExtractErrorMessage(errorResponse, response.StatusCode);
                saveSuccess = false;
                Console.WriteLine("BLAZOR.ITEM.EDIT.SAVE.FAILED: Failed to update item ID {0} - Status: {1}", Id, response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            saveMessage = $"Error updating item: {ex.Message}";
            saveSuccess = false;
            Console.WriteLine("BLAZOR.ITEM.EDIT.SAVE.ERROR: {0}", ex.Message);
        }
        finally
        {
            isSaving = false;
            StateHasChanged();
        }
    }

    private void GoBack()
    {
        Console.WriteLine("BLAZOR.ITEM.EDIT.NAVIGATION: Navigating back to items list");
        Navigation.NavigateTo("/items");
    }

    // Validation and styling helper methods
    private bool IsFormValid()
    {
        return !string.IsNullOrWhiteSpace(updateDto.Name) &&
        !string.IsNullOrWhiteSpace(updateDto.Unit) &&
        updateDto.Name.Length <= 200 &&
        updateDto.Unit.Length <= 50 &&
        (updateDto.Description?.Length ?? 0) <= 1000;
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

    // Track what fields were changed
    private void TrackChangedFields(ItemDto? original, ItemDto updated)
    {
        if (original == null) return;

        updatedFields.Clear();

        if (original.Name != updated.Name)
            updatedFields.Add($"Name ('{original.Name}' → '{updated.Name}')");

        if (original.Unit != updated.Unit)
            updatedFields.Add($"Unit ('{original.Unit}' → '{updated.Unit}')");

        var originalDesc = original.Description ?? "";
        var updatedDesc = updated.Description ?? "";
        if (originalDesc != updatedDesc)
        {
            var shortOriginal = originalDesc.Length > 30 ? originalDesc.Substring(0, 30) + "..." : originalDesc;
            var shortUpdated = updatedDesc.Length > 30 ? updatedDesc.Substring(0, 30) + "..." : updatedDesc;

            if (string.IsNullOrEmpty(originalDesc))
                updatedFields.Add($"Description (added: '{shortUpdated}')");
            else if (string.IsNullOrEmpty(updatedDesc))
                updatedFields.Add("Description (removed)");
            else
                updatedFields.Add($"Description ('{shortOriginal}' → '{shortUpdated}')");
        }
    }

    // Stock status helper methods
    private string GetStockBadgeClass()
    {
        if (item?.Quantity == 0) return "bg-red-100 text-red-800";
        if (item?.IsLowStock == true) return "bg-yellow-100 text-yellow-800";
        return "bg-green-100 text-green-800";
    }

    private string GetStockStatusText()
    {
        if (item?.Quantity == 0) return "Out of Stock";
        if (item?.IsLowStock == true) return "Low Stock";
        return "In Stock";
    }

    // Helper method to ensure consistent timezone display
    private DateTime GetLocalTime(DateTime dateTime)
    {
        // If the datetime is UTC, convert to local time
        if (dateTime.Kind == DateTimeKind.Utc)
        {
            return dateTime.ToLocalTime();
        }
        // If unspecified, assume it's UTC and convert
        else if (dateTime.Kind == DateTimeKind.Unspecified)
        {
            return DateTime.SpecifyKind(dateTime, DateTimeKind.Utc).ToLocalTime();
        }
        // Already local time
        return dateTime;
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
                return $"Failed to update item: {statusCode}";
            }
        }
        catch
        {
            // If JSON parsing fails, return the raw response or a generic message
            errorTitle = $"Error {(int)statusCode}";
            return !string.IsNullOrEmpty(errorResponse) ? errorResponse : $"Failed to update item: {statusCode}";
        }
    }
}
