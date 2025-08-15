using Microsoft.AspNetCore.Components;
using System.Text.Json;
using Inventory.Shared.Dtos.Items;
using Inventory.Shared.Dtos.Common;

namespace Inventory.Blazor.Components.Pages.Items;

public partial class Index : ComponentBase, IDisposable
{
    [Inject] public required NavigationManager Navigation { get; set; }
    [Inject] public required HttpClient Http { get; set; }

    // Filter variables
    private bool? statusFilter = null;
    private bool? stockFilter = null;

    // Pagination variables
    private int currentPage = 1;
    private int pageSize = 10;

    // Data variables
    private PagedResult<ItemSummaryDto>? pagedResult;
    private List<ItemSummaryDto> items = new();
    private bool isLoading = false;
    private string? errorMessage;

    // Search debouncing
    private Timer? searchTimer;
    private string searchTerm = "";

    protected override async Task OnInitializedAsync()
    {
        await LoadItems();
    }

    private async Task LoadItems()
    {
        isLoading = true;
        errorMessage = null;
        StateHasChanged();

        try
        {
            Console.WriteLine("BLAZOR.ITEM.INDEX.LOAD: Loading items - Page {0}, Size {1}, Status {2}, Stock {3}, Search '{4}'",
                currentPage, pageSize, statusFilter, stockFilter, searchTerm);

            var queryParams = new List<string>
            {
                $"page={currentPage}",
                $"pageSize={pageSize}"
            };

            if (statusFilter.HasValue)
                queryParams.Add($"isActive={statusFilter.Value}");

            if (stockFilter.HasValue)
                queryParams.Add($"hasStock={stockFilter.Value}");

            if (!string.IsNullOrWhiteSpace(searchTerm))
                queryParams.Add($"searchTerm={Uri.EscapeDataString(searchTerm.Trim())}");

            var queryString = string.Join("&", queryParams);
            var response = await Http.GetAsync($"api/items?{queryString}");

            if (response.IsSuccessStatusCode)
            {
                var jsonString = await response.Content.ReadAsStringAsync();
                pagedResult = JsonSerializer.Deserialize<PagedResult<ItemSummaryDto>>(jsonString, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (pagedResult != null)
                {
                    items = pagedResult.Items.ToList();
                    Console.WriteLine("BLAZOR.ITEM.INDEX.LOAD.SUCCESS: Loaded {0} items out of {1} total",
                        items.Count, pagedResult.TotalCount);
                }
            }
            else
            {
                errorMessage = $"Failed to load items: {response.StatusCode}";
                Console.WriteLine("BLAZOR.ITEM.INDEX.LOAD.FAILED: API returned status {0}", response.StatusCode);
                items.Clear();
                pagedResult = null;
            }
        }
        catch (Exception ex)
        {
            errorMessage = $"Error loading items: {ex.Message}";
            Console.WriteLine("BLAZOR.ITEM.INDEX.LOAD.ERROR: {0}", ex.Message);
            items.Clear();
            pagedResult = null;
        }
        finally
        {
            isLoading = false;
            StateHasChanged();
        }
    }

    // Search functionality with debouncing
    private string SearchTerm
    {
        get => searchTerm;
        set
        {
            if (searchTerm != value)
            {
                searchTerm = value;
                DebounceSearch();
            }
        }
    }

    private void DebounceSearch()
    {
        searchTimer?.Dispose();
        searchTimer = new Timer(async _ =>
        {
            currentPage = 1; // Reset to first page when searching
            await InvokeAsync(async () =>
            {
                await LoadItems();
                StateHasChanged();
            });
        }, null, 500, Timeout.Infinite); // 500ms delay
    }

    // Filter methods
    private async Task SetStatusFilter(bool? status)
    {
        if (statusFilter != status)
        {
            statusFilter = status;
            currentPage = 1; // Reset to first page when filtering
            await LoadItems();
        }
    }

    private async Task SetStockFilter(bool? stock)
    {
        if (stockFilter != stock)
        {
            stockFilter = stock;
            currentPage = 1; // Reset to first page when filtering
            await LoadItems();
        }
    }

    private async Task SetPageSize(int size)
    {
        if (pageSize != size)
        {
            pageSize = size;
            currentPage = 1; // Reset to first page when changing page size
            await LoadItems();
        }
    }

    // Pagination methods
    private async Task GoToPage(int page)
    {
        if (page != currentPage && page >= 1 && page <= TotalPages)
        {
            currentPage = page;
            await LoadItems();
        }
    }

    // Overload for LoadItems that accepts a page parameter
    private async Task LoadItems(int page)
    {
        await GoToPage(page);
    }

    private async Task NextPage()
    {
        if (currentPage < TotalPages)
        {
            await GoToPage(currentPage + 1);
        }
    }

    private async Task PreviousPage()
    {
        if (currentPage > 1)
        {
            await GoToPage(currentPage - 1);
        }
    }

    // Navigation methods
    private void ViewDetails(int itemId)
    {
        Console.WriteLine("BLAZOR.ITEM.INDEX.NAVIGATION: Navigating to item details {0}", itemId);
        Navigation.NavigateTo($"/items/{itemId}");
    }

    private void EditItem(int itemId)
    {
        Console.WriteLine("BLAZOR.ITEM.INDEX.NAVIGATION: Navigating to edit item {0}", itemId);
        Navigation.NavigateTo($"/items/{itemId}/edit");
    }

    // Item activation/deactivation
    private async Task ToggleItemStatus(ItemSummaryDto item)
    {
        try
        {
            var action = item.IsActive ? "deactivate" : "activate";
            Console.WriteLine("BLAZOR.ITEM.INDEX.TOGGLE: {0} item {1} ({2})", action, item.Id, item.Name);

            var response = await Http.PatchAsync($"api/items/{item.Id}/{action}", null);

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("BLAZOR.ITEM.INDEX.TOGGLE.SUCCESS: Item {0} {1}d successfully", item.Id, action);
                await LoadItems(); // Reload the list to reflect changes
            }
            else
            {
                Console.WriteLine("BLAZOR.ITEM.INDEX.TOGGLE.FAILED: Failed to {0} item {1} - Status: {2}",
                    action, item.Id, response.StatusCode);
                // Could show a toast notification here
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("BLAZOR.ITEM.INDEX.TOGGLE.ERROR: {0}", ex.Message);
            // Could show a toast notification here
        }
    }

    // Individual activation/deactivation methods
    private async Task ActivateItem(int itemId)
    {
        var item = items.FirstOrDefault(i => i.Id == itemId);
        if (item != null)
        {
            await ToggleItemStatus(item);
        }
    }

    private async Task DeactivateItem(int itemId)
    {
        var item = items.FirstOrDefault(i => i.Id == itemId);
        if (item != null)
        {
            await ToggleItemStatus(item);
        }
    }

    // Helper properties for pagination
    private int TotalPages => pagedResult?.TotalPages ?? 1;
    private int TotalItems => pagedResult?.TotalCount ?? 0;
    private int StartItem => pagedResult != null && pagedResult.Items.Any() ? ((currentPage - 1) * pageSize) + 1 : 0;
    private int EndItem => pagedResult != null ? Math.Min(currentPage * pageSize, TotalItems) : 0;

    // Styling helper methods
    private string GetStatusButtonClass(bool? status)
    {
        var isSelected = statusFilter == status;
        return isSelected
            ? "bg-blue-600 text-white border-blue-600"
            : "bg-white text-gray-700 border-gray-300 hover:bg-gray-50";
    }

    private string GetStockButtonClass(bool? stock)
    {
        var isSelected = stockFilter == stock;
        return isSelected
            ? "bg-blue-600 text-white border-blue-600"
            : "bg-white text-gray-700 border-gray-300 hover:bg-gray-50";
    }

    private string GetPageSizeButtonClass(int size)
    {
        var isSelected = pageSize == size;
        return isSelected
            ? "bg-blue-600 text-white border-blue-600"
            : "bg-white text-gray-700 border-gray-300 hover:bg-gray-50";
    }

    private string GetStatusBadgeClass(ItemSummaryDto item)
    {
        return item.IsActive ? "bg-green-100 text-green-800" : "bg-red-100 text-red-800";
    }

    private string GetStockBadgeClass(ItemSummaryDto item)
    {
        if (item.Quantity == 0) return "bg-red-100 text-red-800";
        if (item.IsLowStock) return "bg-yellow-100 text-yellow-800";
        return "bg-green-100 text-green-800";
    }

    private string GetStockStatusText(ItemSummaryDto item)
    {
        if (item.Quantity == 0) return "Out of Stock";
        if (item.IsLowStock) return "Low Stock";
        return "In Stock";
    }

    // Helper method to get page numbers for pagination display
    private List<int> GetPageNumbers()
    {
        var pages = new List<int>();
        var totalPages = TotalPages;

        if (totalPages <= 7)
        {
            // Show all pages if 7 or fewer
            for (int i = 1; i <= totalPages; i++)
            {
                pages.Add(i);
            }
        }
        else
        {
            // Always show first page
            pages.Add(1);

            if (currentPage <= 4)
            {
                // Show 1, 2, 3, 4, 5, ..., last
                for (int i = 2; i <= 5; i++)
                {
                    pages.Add(i);
                }
                pages.Add(-1); // Ellipsis marker
                pages.Add(totalPages);
            }
            else if (currentPage >= totalPages - 3)
            {
                // Show 1, ..., last-4, last-3, last-2, last-1, last
                pages.Add(-1); // Ellipsis marker
                for (int i = totalPages - 4; i <= totalPages; i++)
                {
                    pages.Add(i);
                }
            }
            else
            {
                // Show 1, ..., current-1, current, current+1, ..., last
                pages.Add(-1); // Ellipsis marker
                for (int i = currentPage - 1; i <= currentPage + 1; i++)
                {
                    pages.Add(i);
                }
                pages.Add(-2); // Second ellipsis marker
                pages.Add(totalPages);
            }
        }

        return pages;
    }

    // Additional styling methods
    private string GetPageButtonClass(int page)
    {
        if (page == currentPage)
        {
            return "bg-blue-600 text-white px-3 py-2 text-sm font-medium border border-blue-600";
        }
        return "bg-white text-gray-700 hover:bg-gray-50 px-3 py-2 text-sm font-medium border border-gray-300";
    }

    private string GetTailwindClass(string stockLevelClass)
    {
        return stockLevelClass switch
        {
            "high" => "text-green-600",
            "medium" => "text-yellow-600",
            "low" => "text-red-600",
            _ => "text-gray-600"
        };
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

    public void Dispose()
    {
        searchTimer?.Dispose();
    }
}
