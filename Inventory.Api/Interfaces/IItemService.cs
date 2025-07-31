using Inventory.Api.Common;
using Inventory.Shared.DTOs.Common;
using Inventory.Shared.DTOs.Items;

namespace Inventory.Api.Interfaces;

public interface IItemService
{
    Task<ServiceResult<PagedResult<ItemSummaryDto>>> GetAllItemsAsync(
        int page = 1,
        int pageSize = 20,
        bool? isActive = null,
        string? searchTerm = null,
        string? unit = null,
        bool? hasStock = null,
        string? sortBy = "Name",
        bool sortDescending = false
    );

    Task<ServiceResult<ItemDto>> GetItemByIdAsync(int id, bool includeInactive = false);
    Task<ServiceResult<ItemDto>> GetItemByNameAsync(string name, bool includeInactive = false);
    Task<ServiceResult<ItemDto>> CreateItemAsync(CreateItemDto createItemDto);
    Task<ServiceResult<ItemDto>> UpdateItemAsync(int id, UpdateItemDto updateItemDto);
    Task<ServiceResult> DeleteItemAsync(int id);
    Task<ServiceResult<ItemDto>> RestoreItemAsync(int id);
}
