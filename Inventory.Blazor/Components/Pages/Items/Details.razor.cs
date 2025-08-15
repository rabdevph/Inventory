using Microsoft.AspNetCore.Components;
using System.Text.Json;
using Inventory.Shared.Dtos.Items;

namespace Inventory.Blazor.Components.Pages.Items;

public partial class Details : ComponentBase
{
    [Parameter] public int Id { get; set; }

    [Inject] public required NavigationManager Navigation { get; set; }
    [Inject] public required HttpClient Http { get; set; }

    private ItemDto? item;
    private bool isLoading = true;
    private string? errorMessage;

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
            Console.WriteLine("BLAZOR.ITEM.DETAILS.LOAD: Loading item details for ID {0}", Id);
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
                    Console.WriteLine("BLAZOR.ITEM.DETAILS.LOAD.SUCCESS: Loaded item {0}", item.Name);
                }
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                errorMessage = $"Item with ID {Id} was not found.";
                Console.WriteLine("BLAZOR.ITEM.DETAILS.LOAD.NOTFOUND: Item {0} not found", Id);
            }
            else
            {
                errorMessage = $"Failed to load item details: {response.StatusCode}";
                Console.WriteLine("BLAZOR.ITEM.DETAILS.LOAD.FAILED: API returned status {0} for item {1}", response.StatusCode, Id);
            }
        }
        catch (Exception ex)
        {
            errorMessage = $"Error loading item details: {ex.Message}";
            Console.WriteLine("BLAZOR.ITEM.DETAILS.LOAD.ERROR: {0}", ex.Message);
        }
        finally
        {
            isLoading = false;
            StateHasChanged();
        }
    }

    private void GoBack()
    {
        Console.WriteLine("BLAZOR.ITEM.DETAILS.NAVIGATION: Navigating back to items list");
        Navigation.NavigateTo("/items");
    }

    private void EditItem()
    {
        Console.WriteLine("BLAZOR.ITEM.DETAILS.NAVIGATION: Navigating to edit item {0}", item?.Id);
        Navigation.NavigateTo($"/items/{Id}/edit");
    }

    // Helper methods for styling and display
    private string GetStatusBadgeClass()
    {
        return item?.IsActive == true ? "bg-green-100 text-green-800" : "bg-red-100 text-red-800";
    }

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

    private string GetStockDisplayClass()
    {
        if (item?.Quantity == 0) return "text-red-600";
        if (item?.IsLowStock == true) return "text-yellow-600";
        return "text-green-600";
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
}
