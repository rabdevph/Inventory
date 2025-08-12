using Microsoft.AspNetCore.Mvc;
using Inventory.Api.Interfaces;
using Inventory.Api.Common;
using Inventory.Shared.Dtos.Items;
using Inventory.Shared.Dtos.Common;
using System.Net;
using Microsoft.AspNetCore.Authorization;

namespace Inventory.Api.Controllers;

/// <summary>
/// API Controller for managing inventory items.
/// Provides CRUD operations for inventory items including creation, retrieval, updating, and soft deletion.
/// Supports advanced features like filtering, pagination, sorting, and item restoration.
/// </summary>
/// <remarks>
/// This controller handles all item-related operations in the inventory management system.
/// All operations return standardized responses using the ServiceResult pattern for consistent error handling.
/// 
/// Key Features:
/// - Full CRUD operations (Create, Read, Update, Delete)
/// - Soft deletion with restoration capability
/// - Advanced filtering and search functionality
/// - Pagination support for large datasets
/// - Comprehensive error handling and logging
/// - OpenAPI/Swagger documentation
/// </remarks>
/// <remarks>
/// Initializes a new instance of the ItemsController.
/// </remarks>
/// <param name="itemService">The item service for business logic operations</param>
/// <param name="logger">Logger instance for this controller</param>
/// <exception cref="ArgumentNullException">Thrown when itemService or logger is null</exception>
[ApiController]
[Route("api/items")]
[Produces("application/json")]
[Tags("Items")]
public class ItemsController(IItemService itemService, ILogger<ItemsController> logger) : ApiBaseController
{
    private readonly IItemService _itemService = itemService;
    private readonly ILogger<ItemsController> _logger = logger;

    /// <summary>
    /// Retrieves all inventory items with optional filtering, sorting, and pagination.
    /// </summary>
    /// <param name="page">Page number for pagination (default: 1, minimum: 1)</param>
    /// <param name="pageSize">Number of items per page (default: 20, range: 1-100)</param>
    /// <param name="isActive">Filter by active status. null = all items, true = active only, false = inactive only</param>
    /// <param name="searchTerm">Search term to filter by item name or description (case-insensitive)</param>
    /// <param name="unit">Filter by unit type (integer value representing unit enum)</param>
    /// <param name="hasStock">Filter by stock availability. true = items with stock > 0, false = out of stock items</param>
    /// <param name="sortBy">Field to sort by. Valid values: "Name", "Quantity", "CreatedAt", "UpdatedAt", "Id" (default: "Name")</param>
    /// <param name="sortDescending">Sort direction. true = descending, false = ascending (default: false)</param>
    /// <returns>
    /// A paginated list of item summaries matching the specified criteria.
    /// </returns>
    /// <response code="200">Returns the paginated list of items</response>
    /// <response code="400">Invalid parameters provided (e.g., invalid page number, page size out of range)</response>
    /// <response code="500">Internal server error occurred</response>
    /// <example>
    /// GET /api/items?page=1&amp;pageSize=10&amp;isActive=true&amp;searchTerm=laptop&amp;sortBy=Name&amp;sortDescending=false
    /// </example>
    [HttpGet]
    [Authorize(Policy = "CanViewInventory")]
    [ProducesResponseType(typeof(PagedResult<ItemSummaryDto>), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
    public async Task<IActionResult> GetAllItems(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool? isActive = null,
        [FromQuery] string? searchTerm = null,
        [FromQuery] string? unit = null,
        [FromQuery] bool? hasStock = null,
        [FromQuery] string? sortBy = "Name",
        [FromQuery] bool sortDescending = false)
    {
        _logger.LogInformation("API.ITEMS.GET: Getting all items - Page: {Page}, PageSize: {PageSize}, SearchTerm: {SearchTerm}",
            page, pageSize, searchTerm);

        var result = await _itemService.GetAllItemsAsync(
            page, pageSize, isActive, searchTerm, unit, hasStock, sortBy, sortDescending);

        if (!result.Success)
        {
            _logger.LogWarning("API.ITEMS.GET.FAILED: Failed to retrieve items - Error: {ErrorMessage}", result.ErrorMessage);
        }

        return HandleServiceResult(result);
    }

    /// <summary>
    /// Retrieves a specific inventory item by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the item to retrieve (must be positive integer)</param>
    /// <param name="includeInactive">
    /// Whether to include inactive (soft-deleted) items in the search.
    /// Default is false (active items only).
    /// </param>
    /// <returns>
    /// The requested item details if found, otherwise a 404 Not Found response.
    /// </returns>
    /// <response code="200">Returns the requested item details</response>
    /// <response code="400">Invalid ID parameter (must be positive integer)</response>
    /// <response code="404">Item with the specified ID was not found</response>
    /// <response code="500">Internal server error occurred</response>
    /// <example>
    /// GET /api/items/123
    /// GET /api/items/123?includeInactive=true
    /// </example>
    [HttpGet("{id:int}")]
    [Authorize(Policy = "CanViewInventory")]
    [ProducesResponseType(typeof(ItemDto), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
    public async Task<IActionResult> GetItemById(int id, [FromQuery] bool includeInactive = false)
    {
        _logger.LogInformation("API.ITEMS.GETBYID: Getting item by ID: {ItemId}, IncludeInactive: {IncludeInactive}", id, includeInactive);

        var result = await _itemService.GetItemByIdAsync(id, includeInactive);

        if (!result.Success)
        {
            _logger.LogWarning("API.ITEMS.GETBYID.FAILED: Failed to retrieve item by ID {ItemId} - Error: {ErrorMessage}", id, result.ErrorMessage);
        }

        return HandleServiceResult(result);
    }

    /// <summary>
    /// Retrieves a specific inventory item by its name.
    /// </summary>
    /// <param name="name">
    /// The name of the item to retrieve. Search is case-insensitive and must be an exact match.
    /// Cannot be null, empty, or whitespace only.
    /// </param>
    /// <param name="includeInactive">
    /// Whether to include inactive (soft-deleted) items in the search.
    /// Default is false (active items only).
    /// </param>
    /// <returns>
    /// The requested item details if found, otherwise a 404 Not Found response.
    /// </returns>
    /// <response code="200">Returns the requested item details</response>
    /// <response code="400">Item name is null, empty, or contains only whitespace</response>
    /// <response code="404">Item with the specified name was not found</response>
    /// <response code="500">Internal server error occurred</response>
    /// <example>
    /// GET /api/items/by-name/Laptop%20Computer
    /// GET /api/items/by-name/Office%20Chair?includeInactive=true
    /// </example>
    [HttpGet("by-name/{name}")]
    [Authorize(Policy = "CanViewInventory")]
    [ProducesResponseType(typeof(ItemDto), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
    public async Task<IActionResult> GetItemByName(string name, [FromQuery] bool includeInactive = false)
    {
        _logger.LogInformation("API.ITEMS.GETBYNAME: Getting item by name: {ItemName}, IncludeInactive: {IncludeInactive}", name, includeInactive);

        var result = await _itemService.GetItemByNameAsync(name, includeInactive);

        if (!result.Success)
        {
            _logger.LogWarning("API.ITEMS.GETBYNAME.FAILED: Failed to retrieve item by name '{ItemName}' - Error: {ErrorMessage}", name, result.ErrorMessage);
        }

        return HandleServiceResult(result);
    }

    /// <summary>
    /// Creates a new inventory item in the system.
    /// </summary>
    /// <param name="createItemDto">
    /// The item data for creation. All required fields must be provided and valid.
    /// Item names must be unique across the system.
    /// </param>
    /// <returns>
    /// The newly created item with assigned ID and system-generated fields.
    /// </returns>
    /// <response code="201">Item was successfully created</response>
    /// <response code="400">Invalid request data or model validation failed</response>
    /// <response code="409">An item with the specified name already exists</response>
    /// <response code="422">Validation error occurred (business rule violation)</response>
    /// <response code="500">Internal server error occurred</response>
    /// <remarks>
    /// Sample request:
    /// 
    ///     POST /api/items
    ///     {
    ///         "name": "Laptop Computer",
    ///         "description": "High-performance business laptop",
    ///         "unit": 1,
    ///         "initialQuantity": 10
    ///     }
    /// 
    /// Business Rules:
    /// - Item names must be unique (case-insensitive)
    /// - Name is required and cannot exceed 200 characters
    /// - Description is optional but cannot exceed 1000 characters
    /// - Initial quantity must be non-negative
    /// - Unit must be a valid positive integer
    /// </remarks>
    [HttpPost]
    [Authorize(Policy = "CanManageInventory")]
    [ProducesResponseType(typeof(ItemDto), (int)HttpStatusCode.Created)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.Conflict)]
    [ProducesResponseType((int)HttpStatusCode.UnprocessableEntity)]
    [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
    public async Task<IActionResult> CreateItem([FromBody] CreateItemDto createItemDto)
    {
        _logger.LogInformation("API.ITEMS.CREATE: Creating new item: {ItemName}", createItemDto?.Name ?? "null");

        if (!ModelState.IsValid)
        {
            var validationErrors = string.Join("; ", ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage));

            _logger.LogWarning("API.ITEMS.CREATE.VALIDATION: Model validation failed for item creation - Errors: {ValidationErrors}", validationErrors);

            var validationResult = ServiceResult.ValidationError(validationErrors);
            return HandleServiceResult(validationResult);
        }

        var result = await _itemService.CreateItemAsync(createItemDto!);

        if (!result.Success)
        {
            _logger.LogWarning("API.ITEMS.CREATE.FAILED: Failed to create item '{ItemName}' - Error: {ErrorMessage}", createItemDto!.Name, result.ErrorMessage);
        }

        return HandleServiceResult(result);
    }

    /// <summary>
    /// Updates an existing inventory item with new information.
    /// </summary>
    /// <param name="id">The unique identifier of the item to update (must be positive integer)</param>
    /// <param name="updateItemDto">
    /// The updated item data. Only provided fields will be modified.
    /// Item names must remain unique across the system.
    /// </param>
    /// <returns>
    /// The updated item with all current information including modified timestamps.
    /// </returns>
    /// <response code="200">Item was successfully updated</response>
    /// <response code="400">Invalid request data or model validation failed</response>
    /// <response code="404">Item with the specified ID was not found</response>
    /// <response code="409">Another item with the specified name already exists</response>
    /// <response code="422">Validation error occurred (business rule violation)</response>
    /// <response code="500">Internal server error occurred</response>
    /// <remarks>
    /// Sample request:
    /// 
    ///     PUT /api/items/123
    ///     {
    ///         "name": "Updated Laptop Computer",
    ///         "description": "Updated high-performance business laptop",
    ///         "unit": 2
    ///     }
    /// 
    /// Business Rules:
    /// - Item must exist and be accessible
    /// - Item names must remain unique (case-insensitive) excluding the current item
    /// - Name is required and cannot exceed 200 characters
    /// - Description is optional but cannot exceed 1000 characters
    /// - Unit must be a valid positive integer
    /// - Quantity cannot be modified through this endpoint (use specific quantity management endpoints)
    /// </remarks>
    [HttpPut("{id:int}")]
    [Authorize(Policy = "CanManageInventory")]
    [ProducesResponseType(typeof(ItemDto), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [ProducesResponseType((int)HttpStatusCode.Conflict)]
    [ProducesResponseType((int)HttpStatusCode.UnprocessableEntity)]
    [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
    public async Task<IActionResult> UpdateItem(int id, [FromBody] UpdateItemDto updateItemDto)
    {
        _logger.LogInformation("API.ITEMS.UPDATE: Updating item ID: {ItemId} with name: {ItemName}", id, updateItemDto?.Name ?? "null");

        if (!ModelState.IsValid)
        {
            var validationErrors = string.Join("; ", ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage));

            _logger.LogWarning("API.ITEMS.UPDATE.VALIDATION: Model validation failed for item update ID: {ItemId} - Errors: {ValidationErrors}", id, validationErrors);

            var validationResult = ServiceResult.ValidationError(validationErrors);
            return HandleServiceResult(validationResult);
        }

        var result = await _itemService.UpdateItemAsync(id, updateItemDto!);

        if (!result.Success)
        {
            _logger.LogWarning("API.ITEMS.UPDATE.FAILED: Failed to update item ID: {ItemId} - Error: {ErrorMessage}", id, result.ErrorMessage);
        }

        return HandleServiceResult(result);
    }

    /// <summary>
    /// Deactivates an inventory item by marking it as inactive while preserving all data.
    /// </summary>
    /// <param name="id">The unique identifier of the item to deactivate (must be positive integer)</param>
    /// <returns>
    /// No content on successful deactivation. The item is marked as inactive but data is preserved.
    /// </returns>
    /// <response code="204">Item was successfully deactivated (marked as inactive)</response>
    /// <response code="400">Invalid ID parameter or item is already inactive</response>
    /// <response code="404">Item with the specified ID was not found</response>
    /// <response code="500">Internal server error occurred</response>
    /// <remarks>
    /// This operation performs a "soft deactivation" which means:
    /// - The item is marked as inactive (IsActive = false)
    /// - All item data is preserved in the database
    /// - The item will not appear in normal queries (unless specifically requested)
    /// - The item can be reactivated using the activate endpoint
    /// - Historical data and relationships are maintained
    /// 
    /// Use the activate endpoint (PATCH /api/items/{id}/activate) to reactivate the item.
    /// </remarks>
    [HttpPatch("{id:int}/deactivate")]
    [Authorize(Policy = "CanManageInventory")]
    [ProducesResponseType((int)HttpStatusCode.NoContent)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
    public async Task<IActionResult> DeactivateItem(int id)
    {
        _logger.LogInformation("API.ITEMS.DEACTIVATE: Deactivating item ID: {ItemId}", id);

        var result = await _itemService.DeleteItemAsync(id);

        if (!result.Success)
        {
            _logger.LogWarning("API.ITEMS.DEACTIVATE.FAILED: Failed to deactivate item ID: {ItemId} - Error: {ErrorMessage}", id, result.ErrorMessage);
        }

        return HandleServiceResult(result);
    }

    /// <summary>
    /// Activates a previously deactivated inventory item, making it active again.
    /// </summary>
    /// <param name="id">The unique identifier of the item to activate (must be positive integer)</param>
    /// <returns>
    /// The activated item with updated status and timestamps.
    /// </returns>
    /// <response code="200">Item was successfully activated and is now active</response>
    /// <response code="400">Invalid ID parameter or item is already active</response>
    /// <response code="404">Item with the specified ID was not found</response>
    /// <response code="500">Internal server error occurred</response>
    /// <remarks>
    /// This operation activates a deactivated item by:
    /// - Setting IsActive to true
    /// - Updating the UpdatedAt timestamp
    /// - Making the item visible in normal queries again
    /// - Preserving all original item data and history
    /// 
    /// This endpoint can activate items that were deactivated using the deactivate endpoint.
    /// If an item was never deactivated or doesn't exist, appropriate error responses will be returned.
    /// 
    /// After activation, the item will behave exactly as it did before deactivation,
    /// maintaining all its properties, relationships, and transaction history.
    /// </remarks>
    [HttpPatch("{id:int}/activate")]
    [Authorize(Policy = "CanManageInventory")]
    [ProducesResponseType(typeof(ItemDto), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
    public async Task<IActionResult> ActivateItem(int id)
    {
        _logger.LogInformation("API.ITEMS.ACTIVATE: Activating item ID: {ItemId}", id);

        var result = await _itemService.RestoreItemAsync(id);

        if (!result.Success)
        {
            _logger.LogWarning("API.ITEMS.ACTIVATE.FAILED: Failed to activate item ID: {ItemId} - Error: {ErrorMessage}", id, result.ErrorMessage);
        }

        return HandleServiceResult(result);
    }
}
