using Inventory.Api.Models;
using Inventory.Shared.DTOs.Items;

namespace Inventory.Api.Mappers;

// Static class containing extension methods for mapping between Item entities and DTOs
public static class ItemMapper
{
    // Converts a full Item entity to ItemDto (complete item details)
    public static ItemDto ToDto(this Item item)
    {
        return new ItemDto
        {
            Id = item.Id,
            Name = item.Name,
            Description = item.Description,
            Unit = item.Unit,
            Quantity = item.Quantity,
            IsActive = item.IsActive,
            CanReceiveStock = item.CanReceiveStock,
            CanDistribute = item.CanDistribute,
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt
        };
    }

    // Converts Item entity to ItemSummaryDto (lightweight version for lists/grids)
    public static ItemSummaryDto ToSummaryDto(this Item item)
    {
        return new ItemSummaryDto
        {
            Id = item.Id,
            Name = item.Name,
            Quantity = item.Quantity,
            IsActive = item.IsActive,
            CanDistribute = item.CanDistribute,
            CreatedAt = item.CreatedAt
        };
    }

    // Converts a collection of Items to a collection of ItemDtos
    public static IEnumerable<ItemDto> ToDto(this IEnumerable<Item> items)
    {
        return items.Select(ToDto);
    }

    // Converts a collection of Items to a collection of ItemSummaryDtos
    public static IEnumerable<ItemSummaryDto> ToSummaryDto(this IEnumerable<Item> items)
    {
        return items.Select(ToSummaryDto);
    }

    // Creates a new Item entity from CreateItemDto (for POST operations)
    public static Item ToEntity(this CreateItemDto createDto)
    {
        return new Item
        {
            Name = createDto.Name.Trim(),
            Description = createDto.Description?.Trim(),
            Unit = createDto.Unit.Trim(),
            Quantity = createDto.InitialQuantity,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    // Updates an existing Item entity with data from UpdateItemDto (for PUT operations)
    public static Item UpdateFromDto(this Item item, UpdateItemDto updateDto)
    {
        item.Name = updateDto.Name.Trim();
        item.Description = updateDto.Description?.Trim();
        item.Unit = updateDto.Unit.Trim();
        item.UpdatedAt = DateTime.UtcNow;

        return item;
    }

    // Cleans and validates CreateItemDto input (trims strings, ensures non-negative quantity)
    public static CreateItemDto Clean(this CreateItemDto createDto)
    {
        return new CreateItemDto
        {
            Name = createDto.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(createDto.Description)
                ? null
                : createDto.Description.Trim(),
            Unit = createDto.Unit.Trim(),
            InitialQuantity = Math.Max(0, createDto.InitialQuantity)
        };
    }

    // Cleans and validates UpdateItemDto input (trims strings, handles null descriptions)
    public static UpdateItemDto Clean(this UpdateItemDto updateDto)
    {
        return new UpdateItemDto
        {
            Name = updateDto.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(updateDto.Description)
                ? null
                : updateDto.Description.Trim(),
            Unit = updateDto.Unit.Trim()
        };
    }
}
