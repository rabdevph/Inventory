
using Inventory.Api.Common;
using Inventory.Api.Data;
using Inventory.Api.Interfaces;
using Inventory.Api.Mappers;
using Inventory.Shared.Dtos.InventoryTransactions;
using Inventory.Shared.DTOs.Common;
using Inventory.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Api.Services;



public class InventoryTransactionService(InventoryContext context, ILogger<InventoryTransactionService> logger) : IInventoryTransactionService
{
    private readonly InventoryContext _context = context;
    private readonly ILogger<InventoryTransactionService> _logger = logger;

    #region Query Operations

    public async Task<ServiceResult<PagedResult<InventoryTransactionSummaryDto>>> GetAllTransactionsAsync(
        int page = 1,
        int pageSize = 20,
        string? transactionCode = null,
        TransactionType? type = null,
        TransactionStatus? status = null,
        int? itemId = null,
        int? requestedByEmployeeId = null,
        string? receivedByUserId = null,
        string? processedByUserId = null,
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        string? search = null,
        string? sortBy = null,
        bool sortDescending = false)
    {
        // Validate pagination parameters to endsure they are within acceptable ranges
        if (page < 1)
            return ServiceResult<PagedResult<InventoryTransactionSummaryDto>>.BadRequest("Page number must be greater than 0");

        if (pageSize < 1 || pageSize > 100)
            return ServiceResult<PagedResult<InventoryTransactionSummaryDto>>.BadRequest("Page size must be between 1 and 100");

        // Add date range validation
        if (dateFrom.HasValue && dateTo.HasValue && dateFrom.Value > dateTo.Value)
            return ServiceResult<PagedResult<InventoryTransactionSummaryDto>>.BadRequest("Date from cannot be later than date to");

        // Normalize dates to UTC to avoid timezone issues with PostgreSQL
        if (dateFrom.HasValue)
            dateFrom = DateTime.SpecifyKind(dateFrom.Value.Date, DateTimeKind.Utc); // Start of day in UTC

        if (dateTo.HasValue)
            dateTo = DateTime.SpecifyKind(dateTo.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc); // End of day in UTC

        // Start with base query and include related data
        var query = _context.InventoryTransactions
            .Include(t => t.Item)
            .Include(t => t.ReceivedByUser)
            .Include(t => t.ProcessedByUser)
            .Include(t => t.CancelledByUser)
            .Include(t => t.RequestedByEmployee)
            .AsQueryable();

        // Filter by transaction code if specified
        if (!string.IsNullOrWhiteSpace(transactionCode))
        {
            query = query.Where(t => t.TransactionCode == transactionCode);
        }

        // Filter by type if specified
        if (type.HasValue)
        {
            query = query.Where(t => t.TransactionType == type.Value);
        }

        // Filter by status if specified
        if (status.HasValue)
        {
            query = query.Where(t => t.Status == status.Value);
        }

        // Filter by itemId if specified
        if (itemId.HasValue)
        {
            query = query.Where(t => t.ItemId == itemId.Value);
        }

        // Filter by requestedByEmployeeId if specified
        if (requestedByEmployeeId.HasValue)
        {
            query = query.Where(t => t.RequestedByEmployeeId == requestedByEmployeeId.Value);
        }

        // Filter by receivedByUserId if specified
        if (!string.IsNullOrWhiteSpace(receivedByUserId))
        {
            query = query.Where(t => t.ReceivedByUserId == receivedByUserId);
        }

        // Filter by processedByUserId if specified
        if (!string.IsNullOrWhiteSpace(processedByUserId))
        {
            query = query.Where(t => t.ProcessedByUserId == processedByUserId);
        }

        // Filter by date range if specified - FIXED VERSION
        if (dateFrom.HasValue)
        {
            query = query.Where(t => t.TransactionDate >= dateFrom.Value);
        }
        if (dateTo.HasValue)
        {
            query = query.Where(t => t.TransactionDate <= dateTo.Value);
        }

        // Search by item name or remarks if search is provided
        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(t =>
                (t.Item != null && t.Item.Name != null && t.Item.Name.ToLower().Contains(searchLower)) ||
                (t.Remarks != null && t.Remarks.ToLower().Contains(searchLower)) ||
                (t.TransactionCode != null && t.TransactionCode.ToLower().Contains(searchLower)));
        }

        // Apply sorting based on specified field and direction
        query = ApplySorting(query, sortBy, sortDescending);

        // Get total count for pagination metadata before applying pagination
        var totalCount = await query.CountAsync();

        // Apply pagination and project to DTOs for efficient data transfer
        var transactions = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => t.ToSummaryDto())
            .ToListAsync();

        // Build paginated result with transactions and metadata
        var result = new PagedResult<InventoryTransactionSummaryDto>
        {
            Items = transactions,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };

        return ServiceResult<PagedResult<InventoryTransactionSummaryDto>>.Ok(result);
    }

    public async Task<ServiceResult<InventoryTransactionDto>> GetTransactionByIdAsync(int id)
    {
        // Validate that the provided ID is a positive integer
        if (id <= 0)
            return ServiceResult<InventoryTransactionDto>.BadRequest("Transaction ID must be a positive integer");

        // Find item by using ID
        var transaction = await _context.InventoryTransactions.FindAsync(id);

        // Return not found if transaction doesn't exists
        if (transaction == null)
            return ServiceResult<InventoryTransactionDto>.BadRequest($"Transaction with ID {id} not found");

        // Convert entity to DTO and return success result
        return ServiceResult<InventoryTransactionDto>.Ok(transaction.ToDto());
    }

    public async Task<ServiceResult<InventoryTransactionDto>> GetTransactionByItemIdAsync(int itemId)
    {
        // Validate that the provided item ID is a positive integer
        if (itemId <= 0)
            return ServiceResult<InventoryTransactionDto>.BadRequest("Item ID must be a positive integer");

        // Find transaction by using item ID
        var transaction = await _context.InventoryTransactions
            .Include(t => t.Item)
            .Include(t => t.ReceivedByUser)
            .Include(t => t.ProcessedByUser)
            .Include(t => t.CancelledByUser)
            .Include(t => t.RequestedByEmployee)
            .FirstOrDefaultAsync(t => t.ItemId == itemId);

        // Return not found if transaction item ID doesn't exists
        if (transaction == null)
            return ServiceResult<InventoryTransactionDto>.BadRequest($"No transaction found for item ID {itemId}");

        // Convert entity to DTO and return success result
        return ServiceResult<InventoryTransactionDto>.Ok(transaction.ToDto());
    }

    public async Task<ServiceResult<InventoryTransactionDto>> GetTransactionByEmployeeIdAsync(int employeeId)
    {
        // Validate that the provided employee ID is a positive integer
        if (employeeId <= 0)
            return ServiceResult<InventoryTransactionDto>.BadRequest("Employee ID must be a positive integer");

        // Find transaction by using employee ID
        var transaction = await _context.InventoryTransactions
            .Include(t => t.Item)
            .Include(t => t.ReceivedByUser)
            .Include(t => t.ProcessedByUser)
            .Include(t => t.CancelledByUser)
            .Include(t => t.RequestedByEmployee)
            .FirstOrDefaultAsync(t => t.RequestedByEmployeeId == employeeId);

        // Return not found if transaction item ID doesn't exists
        if (transaction == null)
            return ServiceResult<InventoryTransactionDto>.BadRequest($"No transaction found for employee ID {employeeId}");

        // Convert entity to DTO and return success result
        return ServiceResult<InventoryTransactionDto>.Ok(transaction.ToDto());
    }

    public async Task<ServiceResult<InventoryTransactionDto>> GetTransactionByUserIdAsync(string userId)
    {
        // Validate that the provided user ID is not null or empty
        if (string.IsNullOrWhiteSpace(userId))
            return ServiceResult<InventoryTransactionDto>.BadRequest("User ID must be provided and cannot be empty");

        // Find transaction by using item ID
        var transaction = await _context.InventoryTransactions
            .Include(t => t.Item)
            .Include(t => t.ReceivedByUser)
            .Include(t => t.ProcessedByUser)
            .Include(t => t.CancelledByUser)
            .Include(t => t.RequestedByEmployee)
            .FirstOrDefaultAsync(t => t.ReceivedByUserId == userId ||
                                      t.ProcessedByUserId == userId ||
                                      t.CancelledByUserId == userId);

        // Return not found if transaction item ID doesn't exists
        if (transaction == null)
            return ServiceResult<InventoryTransactionDto>.BadRequest($"No transaction found for user ID {userId}");

        // Convert entity to DTO and return success result
        return ServiceResult<InventoryTransactionDto>.Ok(transaction.ToDto());
    }

    public async Task<ServiceResult<PagedResult<InventoryTransactionSummaryDto>>> GetPendingOutTransactionsAsync(
        int page = 1,
        int pageSize = 20,
        string? sortBy = null,
        bool sortDescending = false)
    {
        // Validate pagination parameters to endsure they are within acceptable ranges
        if (page < 1)
            return ServiceResult<PagedResult<InventoryTransactionSummaryDto>>.BadRequest("Page number must be greater than 0");

        if (pageSize < 1 || pageSize > 100)
            return ServiceResult<PagedResult<InventoryTransactionSummaryDto>>.BadRequest("Page size must be between 1 and 100");

        // Build the base query first
        var query = _context.InventoryTransactions
            .Include(t => t.Item)
            .Include(t => t.ReceivedByUser)
            .Include(t => t.ProcessedByUser)
            .Include(t => t.CancelledByUser)
            .Include(t => t.RequestedByEmployee)
            .Where(t => t.TransactionType == TransactionType.Out && t.Status == TransactionStatus.Pending);

        // Apply sorting if specified
        query = ApplySorting(query, sortBy, sortDescending);

        // Get total count for pagination metadata BEFORE applying pagination
        var totalCount = await query.CountAsync();

        // Apply pagination and project to DTOs for efficient data transfer
        var transactions = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => t.ToSummaryDto())
            .ToListAsync();

        // Build paginated result with transactions and metadata
        var result = new PagedResult<InventoryTransactionSummaryDto>
        {
            Items = transactions,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };

        return ServiceResult<PagedResult<InventoryTransactionSummaryDto>>.Ok(result);
    }

    #endregion

    #region Modification Operations

    public async Task<ServiceResult<InventoryTransactionDto>> CreateInTransactionAsync(
        CreateInInventoryTransactionDto inTransactiondto,
        string receivedByUserId)
    {
        // Generate transaction code
        var transactionCode = await GenerateTransactionCodeAsync(TransactionType.In, DateTime.UtcNow);

        // Clean and normalize the input data
        var cleanDto = inTransactiondto.Clean();

        // Check if item exists
        var item = await _context.Items.FirstOrDefaultAsync(i => i.Id == cleanDto.ItemId);
        if (item == null)
            return ServiceResult<InventoryTransactionDto>.NotFound($"Item with ID {cleanDto.ItemId} not found");

        // Convert DTO to entity
        var transaction = cleanDto.ToEntity();
        transaction.TransactionCode = transactionCode;
        transaction.ReceivedByUserId = receivedByUserId;

        _context.InventoryTransactions.Add(transaction);

        item.Quantity += transaction.Quantity;

        await _context.SaveChangesAsync();

        // Log successful creation for audit purpose
        _logger.LogInformation("TRANSACTION.CREATE.SUCCESS: Created IN transaction {TransactionCode} for ItemId {ItemId}",
            transaction.TransactionCode, transaction.ItemId);

        // Return success result with created transaction data and 201 status
        return ServiceResult<InventoryTransactionDto>.Ok(transaction.ToDto(), 201); // 201 created
    }

    public async Task<ServiceResult<InventoryTransactionDto>> CreateOutTransactionAsync(
        CreateOutInventoryTransactionDto outTransactionDto)
    {
        // Generate transaction code
        var transactionCode = await GenerateTransactionCodeAsync(TransactionType.Out, DateTime.UtcNow);

        // Clean and normalize the input data
        var cleanDto = outTransactionDto.Clean();

        // Find item to update stock
        var existingItem = await _context.Items.FindAsync(cleanDto.ItemId);
        if (existingItem == null)
            return ServiceResult<InventoryTransactionDto>.NotFound($"Item with ID {cleanDto.ItemId} not found");

        // Convert DTO to entity and add to database context
        var transaction = cleanDto.ToEntity();
        _context.InventoryTransactions.Add(transaction);

        transaction.TransactionCode = transactionCode;
        transaction.Status = TransactionStatus.Pending;

        await _context.SaveChangesAsync();

        // Log successful creation for audit purpose
        _logger.LogInformation("TRANSACTION.CREATE.SUCCESS: Created OUT transaction {TransactionCode} for ItemId {ItemId}",
            transaction.TransactionCode, transaction.ItemId);

        // Return success result with created transaction data and 201 status
        return ServiceResult<InventoryTransactionDto>.Ok(transaction.ToDto(), 201); // 201 created
    }

    public async Task<ServiceResult<InventoryTransactionDto>> ProcessOutTransactionAsync(int id, string processedByUserId)
    {
        // Validate that the provided transaction ID is a positive integer
        // and the user ID is not null or empty
        if (id <= 0)
            return ServiceResult<InventoryTransactionDto>.BadRequest("Transaction ID must be a positive integer");
        if (string.IsNullOrWhiteSpace(processedByUserId))
            return ServiceResult<InventoryTransactionDto>.BadRequest("UserId must be provided");

        // Find the transaction
        var transaction = await _context.InventoryTransactions.FindAsync(id);
        if (transaction == null)
            return ServiceResult<InventoryTransactionDto>.NotFound($"Transaction with ID {id} not found");

        // Check if already processed, cancelled, or not an OUT transaction
        if (transaction.Status == TransactionStatus.Completed)
            return ServiceResult<InventoryTransactionDto>.BadRequest("Transaction is already processed");
        if (transaction.Status == TransactionStatus.Cancelled)
            return ServiceResult<InventoryTransactionDto>.BadRequest("Cannot process a cancelled transaction");
        if (transaction.TransactionType != TransactionType.Out)
            return ServiceResult<InventoryTransactionDto>.BadRequest("Only OUT transactions can be processed");

        // Find existing item from created in transaction
        var item = await _context.Items.FindAsync(transaction.ItemId);
        if (item == null)
            return ServiceResult<InventoryTransactionDto>.BadRequest($"Item with ID {transaction.ItemId} not found");

        // Check current stock if sufficient
        if (item.Quantity < transaction.Quantity)
            return ServiceResult<InventoryTransactionDto>.Conflict("Insufficient stock to process this transaction.");

        // Update item quantity and transaction status
        item.Quantity -= transaction.Quantity;
        transaction.Status = TransactionStatus.Completed;
        transaction.ProcessedByUserId = processedByUserId;
        transaction.TransactionDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Log successful update for audit purpose
        _logger.LogInformation("TRANSACTION.PROCESS.SUCCESS: Processed OUT transaction {TransactionCode} by user {UserId}",
            transaction.TransactionCode, processedByUserId);

        // Return success result with updated transaction data
        return ServiceResult<InventoryTransactionDto>.Ok(transaction.ToDto());
    }

    public async Task<ServiceResult<InventoryTransactionDto>> CancelTransactionAsync(int id, string cancelledByUserId)
    {
        // Validate that the provided transaction ID is a positive integer
        // and the user ID is not null or empty
        if (id <= 0)
            return ServiceResult<InventoryTransactionDto>.BadRequest("Transaction ID must be a positive integer");
        if (string.IsNullOrWhiteSpace(cancelledByUserId))
            return ServiceResult<InventoryTransactionDto>.BadRequest("UserId must be provided");

        // Find the transaction
        var transaction = await _context.InventoryTransactions.FindAsync(id);
        if (transaction == null)
            return ServiceResult<InventoryTransactionDto>.NotFound($"Transaction with ID {id} not found");

        // Check if already processed or not an IN transaction
        if (transaction.Status == TransactionStatus.Completed)
            return ServiceResult<InventoryTransactionDto>.BadRequest("Transaction is already processed");
        if (transaction.Status == TransactionStatus.Cancelled)
            return ServiceResult<InventoryTransactionDto>.BadRequest("Transaction is already cancelled");
        if (transaction.TransactionType != TransactionType.Out)
            return ServiceResult<InventoryTransactionDto>.BadRequest("Only OUT transactions can be processed");

        // Update transaction
        transaction.Status = TransactionStatus.Cancelled;
        transaction.CancelledByUserId = cancelledByUserId;
        transaction.CancelledAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Log successful update for audit purpose
        _logger.LogInformation("TRANSACTION.CANCEL.SUCCESS: Cancelled OUT transaction {TransactionCode} by user {UserId}",
            transaction.TransactionCode, cancelledByUserId);

        // Return success result with updated transaction data
        return ServiceResult<InventoryTransactionDto>.Ok(transaction.ToDto());
    }

    #endregion

    #region Private Helper Methods

    private async Task<string> GenerateTransactionCodeAsync(TransactionType type, DateTime date)
    {
        string typeCode = type == TransactionType.In ? "IN" : "OUT";
        string datePart = date.ToString("yyyyMMdd");

        int count = await _context.InventoryTransactions
            .CountAsync(t => t.TransactionType == type && t.CreatedAt.Date == date.Date);

        string sequence = (count + 1).ToString("D4");

        return $"{typeCode}-{datePart}-{sequence}";
    }

    private static IQueryable<Models.InventoryTransaction> ApplySorting(IQueryable<Models.InventoryTransaction> query, string? sortBy, bool sortDescending)
    {
        var validSortBy = sortBy?.Trim();
        return validSortBy?.ToLower() switch
        {
            "itemname" => sortDescending ? query.OrderByDescending(t => t.Item.Name) : query.OrderBy(t => t.Item.Name),
            "quantity" => sortDescending ? query.OrderByDescending(t => t.Quantity) : query.OrderBy(t => t.Quantity),
            "transactiondate" => sortDescending ? query.OrderByDescending(t => t.TransactionDate) : query.OrderBy(t => t.TransactionDate),
            "createdat" => sortDescending ? query.OrderByDescending(t => t.CreatedAt) : query.OrderBy(t => t.CreatedAt),
            "id" => sortDescending ? query.OrderByDescending(t => t.Id) : query.OrderBy(t => t.Id),
            _ => query.OrderByDescending(t => t.Id) // Default sort by Id descending
        };
    }

    #endregion  
}
