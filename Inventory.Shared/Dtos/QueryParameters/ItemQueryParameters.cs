using System.ComponentModel.DataAnnotations;

namespace Inventory.Shared.Dtos.QueryParameters;

// Query parameters for filtering and paginating item requests
public class ItemQueryParameters
{
    private int _page = 1;
    private int _pageSize = 20;

    [Range(1, int.MaxValue, ErrorMessage = "Page must be at least 1")]
    public int Page
    {
        get => _page;
        set => _page = Math.Max(1, value);
    }

    [Range(1, 100, ErrorMessage = "Page size must be between 1 and 100")]
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = Math.Clamp(value, 1, 100);
    }

    public bool? IsActive { get; set; }

    [StringLength(200, ErrorMessage = "Search term cannot exceed 200 characters")]
    public string? SearchTerm { get; set; }

    public string? Unit { get; set; }
    public bool? HasStock { get; set; }
    public string SortBy { get; set; } = "Name";
    public bool SortDescending { get; set; } = false;
    public bool? IsLowStock { get; set; }

    [Range(1, 1000, ErrorMessage = "Low stock threshold must be between 1 and 1000")]
    public int LowStockThreshold { get; set; } = 10;

    public bool IncludeInactive { get; set; } = false;

    // Gets a clean search term (trimmed and null if empty)
    public string? CleanSearchTerm => string.IsNullOrWhiteSpace(SearchTerm) ? null : SearchTerm.Trim();

    // Gets valid sort fields
    public static readonly string[] ValidSortFields = { "Name", "Quantity", "CreatedAt", "UpdatedAt", "Id" };

    // Gets the validated sort field (defaults to "Name" if invalid)
    public string ValidatedSortBy => ValidSortFields.Contains(SortBy, StringComparer.OrdinalIgnoreCase)
        ? SortBy
        : "Name";
}
