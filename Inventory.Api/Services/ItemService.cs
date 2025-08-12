using Microsoft.EntityFrameworkCore;
using Inventory.Api.Data;
using Inventory.Api.Interfaces;
using Inventory.Api.Models;
using Inventory.Api.Mappers;
using Inventory.Api.Common;
using Inventory.Shared.Dtos.Common;
using Inventory.Shared.Dtos.Items;

namespace Inventory.Api.Services;

public class ItemService(InventoryContext context, ILogger<ItemService> logger) : IItemService
{
    private readonly InventoryContext _context = context;
    private readonly ILogger<ItemService> _logger = logger;

    #region Query Operations

    public async Task<ServiceResult<PagedResult<ItemSummaryDto>>> GetAllItemsAsync(
        int page = 1,
        int pageSize = 20,
        bool? isActive = null,
        string? searchTerm = null,
        string? unit = null,
        bool? hasStock = null,
        string? sortBy = "Name",
        bool sortDescending = false)
    {
        // Validate pagination parameters to ensure they are within acceptable ranges
        if (page < 1)
            return ServiceResult<PagedResult<ItemSummaryDto>>.BadRequest("Page number must be greater than 0");

        if (pageSize < 1 || pageSize > 100)
            return ServiceResult<PagedResult<ItemSummaryDto>>.BadRequest("Page size must be between 1 and 100");

        // Start with base query for all items
        var query = _context.Items.AsQueryable();

        // Filter by active status if specified
        if (isActive.HasValue)
        {
            query = query.Where(i => i.IsActive == isActive.Value);
        }

        // Apply search term filter across name and description fields
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var cleanSearchTerm = searchTerm.Trim().ToLower();
            query = query.Where(i => i.Name.ToLower().Contains(cleanSearchTerm) ||
                                    (i.Description != null && i.Description.ToLower().Contains(cleanSearchTerm)));
        }

        // Filter by specific unit type if provided
        if (!string.IsNullOrWhiteSpace(unit))
        {
            query = query.Where(i => i.Unit == unit);
        }

        // Filter by stock availability based on quantity
        if (hasStock.HasValue)
        {
            if (hasStock.Value)
            {
                query = query.Where(i => i.Quantity > 0);
            }
            else
            {
                query = query.Where(i => i.Quantity == 0);
            }
        }

        // Apply sorting based on specified field and direction
        query = ApplySorting(query, sortBy, sortDescending);

        // Get total count for pagination metadata before applying pagination
        var totalCount = await query.CountAsync();

        // Apply pagination and project to Dtos for efficient data transfer
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(i => i.ToSummaryDto())
            .ToListAsync();

        // Build paginated result with items and metadata
        var result = new PagedResult<ItemSummaryDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };

        return ServiceResult<PagedResult<ItemSummaryDto>>.Ok(result);
    }

    public async Task<ServiceResult<ItemDto>> GetItemByIdAsync(int id, bool includeInactive = false)
    {
        // Validate that the provided ID is a positive integer
        if (id <= 0)
            return ServiceResult<ItemDto>.BadRequest("Item ID must be a positive integer");

        // Start with base query for items
        var query = _context.Items.AsQueryable();

        // Filter out inactive items unless specifically requested
        if (!includeInactive)
        {
            query = query.Where(i => i.IsActive);
        }

        // Find the item by ID using the filtered query
        var item = await query.FirstOrDefaultAsync(i => i.Id == id);

        // Return not found if item doesn't exist or doesn't match criteria
        if (item == null)
            return ServiceResult<ItemDto>.NotFound($"Item with ID {id} not found");

        // Convert entity to DTO and return success result
        return ServiceResult<ItemDto>.Ok(item.ToDto());
    }

    public async Task<ServiceResult<ItemDto>> GetItemByNameAsync(string name, bool includeInactive = false)
    {
        // Validate that the name parameter is not null, empty, or whitespace
        if (string.IsNullOrWhiteSpace(name))
            return ServiceResult<ItemDto>.BadRequest("Item name cannot be empty");

        // Start with base query for items
        var query = _context.Items.AsQueryable();

        // Filter out inactive items unless specifically requested
        if (!includeInactive)
        {
            query = query.Where(i => i.IsActive);
        }

        // Find item by name using case-insensitive comparison with trimmed input
        var trimmedName = name.Trim();
        var item = await query.FirstOrDefaultAsync(i => i.Name.ToLower() == trimmedName.ToLower());

        // Return not found if item doesn't exist or doesn't match criteria
        if (item == null)
            return ServiceResult<ItemDto>.NotFound($"Item with name '{name}' not found");

        // Convert entity to DTO and return success result
        return ServiceResult<ItemDto>.Ok(item.ToDto());
    }

    #endregion

    #region Modification Operations

    public async Task<ServiceResult<ItemDto>> CreateItemAsync(CreateItemDto createItemDto)
    {
        // Clean and normalize the input data (trim strings, validate ranges)
        var cleanDto = createItemDto.Clean();

        // Check if an item with the same name already exists (case-insensitive)
        var existingItem = await _context.Items
            .AnyAsync(i => i.Name.ToLower() == cleanDto.Name.ToLower());

        // Return conflict error if duplicate name is found
        if (existingItem)
        {
            _logger.LogWarning("ITEM.CREATE.FAILED: Attempted to create item with duplicate name: {ItemName}", cleanDto.Name);
            return ServiceResult<ItemDto>.Conflict($"An item with the name '{cleanDto.Name}' already exists");
        }

        // Convert DTO to entity and add to database context
        var item = cleanDto.ToEntity();
        _context.Items.Add(item);
        await _context.SaveChangesAsync();

        // Log successful creation for audit purposes
        _logger.LogInformation("ITEM.CREATE.SUCCESS: Created new item: {ItemName} with ID {ItemId}", item.Name, item.Id);

        // Return success result with created item data and 201 status
        return ServiceResult<ItemDto>.Ok(item.ToDto(), 201); // 201 Created
    }

    public async Task<ServiceResult<ItemDto>> UpdateItemAsync(int id, UpdateItemDto updateItemDto)
    {
        // Validate that the provided ID is a positive integer
        if (id <= 0)
            return ServiceResult<ItemDto>.BadRequest("Item ID must be a positive integer");

        // Clean and normalize the input data (trim strings, validate ranges)
        var cleanDto = updateItemDto.Clean();

        // Find the existing item to update
        var item = await _context.Items.FirstOrDefaultAsync(i => i.Id == id);
        if (item == null)
            return ServiceResult<ItemDto>.NotFound($"Item with ID {id} not found");

        // Check if another item with the same name exists (excluding current item)
        var existingItem = await _context.Items
            .AnyAsync(i => i.Id != id && i.Name.ToLower() == cleanDto.Name.ToLower());

        // Return conflict error if duplicate name is found
        if (existingItem)
        {
            _logger.LogWarning("ITEM.UPDATE.FAILED: Attempted to update item {ItemId} with duplicate name: {ItemName}", id, cleanDto.Name);
            return ServiceResult<ItemDto>.Conflict($"An item with the name '{cleanDto.Name}' already exists");
        }

        // Apply updates from DTO to the existing entity
        item.UpdateFromDto(cleanDto);
        await _context.SaveChangesAsync();

        // Log successful update for audit purposes
        _logger.LogInformation("ITEM.UPDATE.SUCCESS: Updated item: {ItemName} with ID {ItemId}", item.Name, item.Id);

        // Return success result with updated item data
        return ServiceResult<ItemDto>.Ok(item.ToDto());
    }

    public async Task<ServiceResult> DeleteItemAsync(int id)
    {
        // Validate that the provided ID is a positive integer
        if (id <= 0)
            return ServiceResult.BadRequest("Item ID must be a positive integer");

        // Find the item to delete (includes both active and inactive items)
        var item = await _context.Items.FirstOrDefaultAsync(i => i.Id == id);
        if (item == null)
            return ServiceResult.NotFound($"Item with ID {id} not found");

        // Prevent duplicate soft deletion of already inactive items
        if (!item.IsActive)
            return ServiceResult.BadRequest("Item is already deleted");

        // Perform soft delete by marking as inactive and updating timestamp
        item.IsActive = false;
        item.UpdatedAt = DateTime.UtcNow;

        // Save changes to persist the soft deletion
        await _context.SaveChangesAsync();

        // Log successful soft deletion for audit purposes
        _logger.LogInformation("ITEM.DELETE.SUCCESS: Soft deleted item: {ItemName} with ID {ItemId}", item.Name, item.Id);

        // Return success result with 204 No Content status
        return ServiceResult.Ok(204); // 204 No Content
    }

    public async Task<ServiceResult<ItemDto>> RestoreItemAsync(int id)
    {
        // Validate that the provided ID is a positive integer
        if (id <= 0)
            return ServiceResult<ItemDto>.BadRequest("Item ID must be a positive integer");

        // Find the item to restore (includes both active and inactive items)
        var item = await _context.Items.FirstOrDefaultAsync(i => i.Id == id);
        if (item == null)
            return ServiceResult<ItemDto>.NotFound($"Item with ID {id} not found");

        // Prevent restoration of already active items
        if (item.IsActive)
            return ServiceResult<ItemDto>.BadRequest("Item is already active");

        // Restore the item by marking as active and updating timestamp
        item.IsActive = true;
        item.UpdatedAt = DateTime.UtcNow;

        // Save changes to persist the restoration
        await _context.SaveChangesAsync();

        // Log successful restoration for audit purposes
        _logger.LogInformation("ITEM.RESTORE.SUCCESS: Restored item: {ItemName} with ID {ItemId}", item.Name, item.Id);

        // Return success result with restored item data
        return ServiceResult<ItemDto>.Ok(item.ToDto());
    }

    #endregion

    #region Private Helper Methods

    private static IQueryable<Item> ApplySorting(IQueryable<Item> query, string? sortBy, bool sortDescending)
    {
        // Clean and normalize the sort field name
        var validSortBy = sortBy?.Trim();

        // Apply sorting based on the specified field and direction using pattern matching
        var orderedQuery = validSortBy?.ToLower() switch
        {
            "name" => sortDescending ? query.OrderByDescending(i => i.Name) : query.OrderBy(i => i.Name),
            "quantity" => sortDescending ? query.OrderByDescending(i => i.Quantity) : query.OrderBy(i => i.Quantity),
            "createdat" => sortDescending ? query.OrderByDescending(i => i.CreatedAt) : query.OrderBy(i => i.CreatedAt),
            "updatedat" => sortDescending ? query.OrderByDescending(i => i.UpdatedAt) : query.OrderBy(i => i.UpdatedAt),
            "id" => sortDescending ? query.OrderByDescending(i => i.Id) : query.OrderBy(i => i.Id),
            _ => query.OrderBy(i => i.Name) // Default sort by name ascending for consistency
        };

        return orderedQuery;
    }

    #endregion
}
