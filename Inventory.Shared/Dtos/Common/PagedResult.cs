namespace Inventory.Shared.Dtos.Common;

// Generic container for paginated results
public class PagedResult<T>
{
    public IEnumerable<T> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }

    // Computed properties for pagination logic
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
    public bool IsFirstPage => Page == 1;
    public bool IsLastPage => Page == TotalPages;
    public int StartItem => TotalCount == 0 ? 0 : ((Page - 1) * PageSize) + 1;
    public int EndItem => Math.Min(Page * PageSize, TotalCount);
    public string DisplayText => TotalCount == 0
        ? "No items found"
        : $"Showing {StartItem}-{EndItem} of {TotalCount} items";
}
