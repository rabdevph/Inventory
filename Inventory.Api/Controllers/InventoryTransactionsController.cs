using Microsoft.AspNetCore.Mvc;
using Inventory.Api.Interfaces;
using Inventory.Api.Common;
using Inventory.Shared.Dtos.InventoryTransactions;
using Inventory.Shared.DTOs.Common;
using Inventory.Shared.Enums;
using System.Net;

namespace Inventory.Api.Controllers;

/// <summary>
/// API Controller for managing inventory transactions.
/// Provides operations for creating, retrieving, processing, and cancelling inventory transactions.
/// Supports advanced features like filtering, pagination, sorting, and transaction status management.
/// </summary>
/// <remarks>
/// This controller handles all transaction-related operations in the inventory management system.
/// All operations return standardized responses using the ServiceResult pattern for consistent error handling.
/// 
/// Key Features:
/// - Create IN (receive) and OUT (issue) transactions
/// - Retrieve transactions with advanced filtering and pagination
/// - Process pending OUT transactions
/// - Cancel transactions with audit trail
/// - Transaction code generation and management
/// - Comprehensive error handling and logging
/// - OpenAPI/Swagger documentation
/// </remarks>
/// <remarks>
/// Initializes a new instance of the InventoryTransactionsController.
/// </remarks>
/// <param name="transactionService">The inventory transaction service for business logic operations</param>
/// <param name="logger">Logger instance for this controller</param>
/// <exception cref="ArgumentNullException">Thrown when transactionService or logger is null</exception>
[ApiController]
[Route("api/inventory-transactions")]
[Produces("application/json")]
[Tags("Inventory Transactions")]
public class InventoryTransactionsController(IInventoryTransactionService transactionService, ILogger<InventoryTransactionsController> logger) : ApiBaseController
{
    private readonly IInventoryTransactionService _transactionService = transactionService;
    private readonly ILogger<InventoryTransactionsController> _logger = logger;

    /// <summary>
    /// Retrieves all inventory transactions with optional filtering, sorting, and pagination.
    /// </summary>
    /// <param name="page">Page number for pagination (default: 1, minimum: 1)</param>
    /// <param name="pageSize">Number of transactions per page (default: 20, range: 1-100)</param>
    /// <param name="transactionCode">Filter by specific transaction code (exact match)</param>
    /// <param name="type">Filter by transaction type (In = 0, Out = 1)</param>
    /// <param name="status">Filter by transaction status (Pending = 0, Completed = 1, Cancelled = 2)</param>
    /// <param name="itemId">Filter by specific item ID</param>
    /// <param name="requestedByEmployeeId">Filter by employee who requested the transaction</param>
    /// <param name="receivedByUserId">Filter by user who received the transaction</param>
    /// <param name="processedByUserId">Filter by user who processed the transaction</param>
    /// <param name="dateFrom">Filter transactions from this date (inclusive)</param>
    /// <param name="dateTo">Filter transactions to this date (inclusive)</param>
    /// <param name="search">Search term to filter by item name or remarks (case-insensitive)</param>
    /// <param name="sortBy">Field to sort by. Valid values: "ItemName", "Quantity", "TransactionDate", "CreatedAt", "Id" (default: "Id")</param>
    /// <param name="sortDescending">Sort direction. true = descending, false = ascending (default: true)</param>
    /// <returns>
    /// A paginated list of transaction summaries matching the specified criteria.
    /// </returns>
    /// <response code="200">Returns the paginated list of transactions</response>
    /// <response code="400">Invalid parameters provided (e.g., invalid page number, page size out of range)</response>
    /// <response code="500">Internal server error occurred</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<InventoryTransactionSummaryDto>), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
    public async Task<IActionResult> GetAllTransactions(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? transactionCode = null,
        [FromQuery] TransactionType? type = null,
        [FromQuery] TransactionStatus? status = null,
        [FromQuery] int? itemId = null,
        [FromQuery] int? requestedByEmployeeId = null,
        [FromQuery] string? receivedByUserId = null,
        [FromQuery] string? processedByUserId = null,
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null,
        [FromQuery] string? search = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool sortDescending = true)
    {
        _logger.LogInformation("API.TRANSACTIONS.GET: Getting all transactions - Page: {Page}, PageSize: {PageSize}, Type: {Type}, Status: {Status}",
            page, pageSize, type, status);

        var result = await _transactionService.GetAllTransactionsAsync(
            page, pageSize, transactionCode, type, status, itemId, requestedByEmployeeId,
            receivedByUserId, processedByUserId, dateFrom, dateTo, search, sortBy, sortDescending);

        if (!result.Success)
        {
            _logger.LogWarning("API.TRANSACTIONS.GET.FAILED: Failed to retrieve transactions - Error: {ErrorMessage}", result.ErrorMessage);
        }

        return HandleServiceResult(result);
    }

    /// <summary>
    /// Retrieves a specific inventory transaction by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the transaction to retrieve (must be positive integer)</param>
    /// <returns>
    /// The requested transaction details if found, otherwise a 404 Not Found response.
    /// </returns>
    /// <response code="200">Returns the requested transaction details</response>
    /// <response code="400">Invalid ID parameter (must be positive integer)</response>
    /// <response code="404">Transaction with the specified ID was not found</response>
    /// <response code="500">Internal server error occurred</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(InventoryTransactionDto), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
    public async Task<IActionResult> GetTransactionById(int id)
    {
        _logger.LogInformation("API.TRANSACTIONS.GETBYID: Getting transaction by ID: {TransactionId}", id);

        var result = await _transactionService.GetTransactionByIdAsync(id);

        if (!result.Success)
        {
            _logger.LogWarning("API.TRANSACTIONS.GETBYID.FAILED: Failed to retrieve transaction by ID {TransactionId} - Error: {ErrorMessage}", id, result.ErrorMessage);
        }

        return HandleServiceResult(result);
    }

    /// <summary>
    /// Retrieves pending OUT transactions with optional pagination and sorting.
    /// </summary>
    /// <param name="page">Page number for pagination (default: 1, minimum: 1)</param>
    /// <param name="pageSize">Number of transactions per page (default: 20, range: 1-100)</param>
    /// <param name="sortBy">Field to sort by. Valid values: "ItemName", "Quantity", "TransactionDate", "CreatedAt", "Id" (default: "Id")</param>
    /// <param name="sortDescending">Sort direction. true = descending, false = ascending (default: true)</param>
    /// <returns>
    /// A paginated list of pending OUT transactions.
    /// </returns>
    /// <response code="200">Returns the paginated list of pending OUT transactions</response>
    /// <response code="400">Invalid parameters provided</response>
    /// <response code="500">Internal server error occurred</response>
    [HttpGet("pending")]
    [ProducesResponseType(typeof(PagedResult<InventoryTransactionSummaryDto>), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
    public async Task<IActionResult> GetPendingOutTransactions(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool sortDescending = true)
    {
        _logger.LogInformation("API.TRANSACTIONS.GETPENDING: Getting pending OUT transactions - Page: {Page}, PageSize: {PageSize}", page, pageSize);

        var result = await _transactionService.GetPendingOutTransactionsAsync(page, pageSize, sortBy, sortDescending);

        if (!result.Success)
        {
            _logger.LogWarning("API.TRANSACTIONS.GETPENDING.FAILED: Failed to retrieve pending transactions - Error: {ErrorMessage}", result.ErrorMessage);
        }

        return HandleServiceResult(result);
    }

    /// <summary>
    /// Creates a new IN (receive) inventory transaction.
    /// </summary>
    /// <param name="createInTransactionDto">
    /// The transaction data for creation. All required fields must be provided and valid.
    /// Transaction codes are automatically generated by the system.
    /// </param>
    /// <param name="receivedByUserId">The user ID of the person receiving the inventory.</param>
    /// <returns>
    /// The newly created transaction with assigned ID and system-generated fields.
    /// </returns>
    /// <response code="201">Transaction was successfully created</response>
    /// <response code="400">Invalid request data or model validation failed</response>
    /// <response code="422">Validation error occurred (business rule violation)</response>
    /// <response code="500">Internal server error occurred</response>
    /// <remarks>
    /// Sample request:
    /// 
    ///     POST /api/inventory-transactions/in
    ///     {
    ///         "itemId": 1,
    ///         "quantity": 10,
    ///         "receivedDate": "2024-08-03T10:00:00Z",
    ///         "remarks": "Monthly inventory replenishment"
    ///     }
    /// 
    ///     Query parameter:
    ///         receivedByUserId=user-001
    /// 
    /// Business Rules:
    /// - ItemId must reference an existing active item
    /// - Quantity must be positive
    /// - ReceivedDate is optional (defaults to current UTC time)
    /// - Remarks are optional but recommended for audit purposes
    /// - receivedByUserId is required
    /// - Transaction code is automatically generated (format: IN-YYYYMMDD-NNNN)
    /// </remarks>
    [HttpPost("in")]
    [ProducesResponseType(typeof(InventoryTransactionDto), (int)HttpStatusCode.Created)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.UnprocessableEntity)]
    [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
    public async Task<IActionResult> CreateInTransaction(
        [FromBody] CreateInInventoryTransactionDto createInTransactionDto,
        [FromQuery] string receivedByUserId)
    {
        _logger.LogInformation("API.TRANSACTIONS.CREATE.IN: Creating new IN transaction for ItemId: {ItemId}, ReceivedByUserId: {UserId}", createInTransactionDto?.ItemId ?? 0, receivedByUserId);

        if (!ModelState.IsValid || string.IsNullOrWhiteSpace(receivedByUserId))
        {
            var validationErrors = string.Join("; ", ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage));
            if (string.IsNullOrWhiteSpace(receivedByUserId))
            {
                validationErrors += "; receivedByUserId is required";
            }
            _logger.LogWarning("API.TRANSACTIONS.CREATE.IN.VALIDATION: Model validation failed - Errors: {ValidationErrors}", validationErrors);

            var validationResult = ServiceResult.ValidationError(validationErrors);
            return HandleServiceResult(validationResult);
        }

        var result = await _transactionService.CreateInTransactionAsync(createInTransactionDto!, receivedByUserId);

        if (!result.Success)
        {
            _logger.LogWarning("API.TRANSACTIONS.CREATE.IN.FAILED: Failed to create IN transaction for ItemId {ItemId} - Error: {ErrorMessage}",
                createInTransactionDto!.ItemId, result.ErrorMessage);
        }

        return HandleServiceResult(result);
    }

    /// <summary>
    /// Creates a new OUT (issue) inventory transaction.
    /// </summary>
    /// <param name="createOutTransactionDto">
    /// The transaction data for creation. All required fields must be provided and valid.
    /// Transaction codes are automatically generated by the system.
    /// </param>
    /// <returns>
    /// The newly created transaction with assigned ID and system-generated fields.
    /// </returns>
    /// <response code="201">Transaction was successfully created</response>
    /// <response code="400">Invalid request data or model validation failed</response>
    /// <response code="422">Validation error occurred (business rule violation)</response>
    /// <response code="500">Internal server error occurred</response>
    /// <remarks>
    /// Sample request:
    /// 
    ///     POST /api/inventory-transactions/out
    ///     {
    ///         "itemId": 1,
    ///         "quantity": 5,
    ///         "requestedByEmployeeId": 123,
    ///         "remarks": "Equipment request for new hire"
    ///     }
    /// 
    /// Business Rules:
    /// - ItemId must reference an existing active item
    /// - Quantity must be positive
    /// - RequestedByEmployeeId must reference an existing employee
    /// - Remarks are optional but recommended for audit purposes
    /// - Transaction code is automatically generated (format: OUT-YYYYMMDD-NNNN)
    /// - Transaction is created in Pending status and requires processing
    /// </remarks>
    [HttpPost("out")]
    [ProducesResponseType(typeof(InventoryTransactionDto), (int)HttpStatusCode.Created)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.UnprocessableEntity)]
    [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
    public async Task<IActionResult> CreateOutTransaction([FromBody] CreateOutInventoryTransactionDto createOutTransactionDto)
    {
        _logger.LogInformation("API.TRANSACTIONS.CREATE.OUT: Creating new OUT transaction for ItemId: {ItemId}, EmployeeId: {EmployeeId}",
            createOutTransactionDto?.ItemId ?? 0, createOutTransactionDto?.RequestedByEmployeeId ?? 0);

        if (!ModelState.IsValid)
        {
            var validationErrors = string.Join("; ", ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage));

            _logger.LogWarning("API.TRANSACTIONS.CREATE.OUT.VALIDATION: Model validation failed - Errors: {ValidationErrors}", validationErrors);

            var validationResult = ServiceResult.ValidationError(validationErrors);
            return HandleServiceResult(validationResult);
        }

        var result = await _transactionService.CreateOutTransactionAsync(createOutTransactionDto!);

        if (!result.Success)
        {
            _logger.LogWarning("API.TRANSACTIONS.CREATE.OUT.FAILED: Failed to create OUT transaction for ItemId {ItemId} - Error: {ErrorMessage}",
                createOutTransactionDto!.ItemId, result.ErrorMessage);
        }

        return HandleServiceResult(result);
    }

    /// <summary>
    /// Processes a pending OUT transaction, marking it as completed.
    /// </summary>
    /// <param name="id">The unique identifier of the transaction to process (must be positive integer)</param>
    /// <param name="request">The request containing the ID of the user processing the transaction</param>
    /// <returns>
    /// The processed transaction with updated status and processing information.
    /// </returns>
    /// <response code="200">Transaction was successfully processed</response>
    /// <response code="400">Invalid parameters or transaction cannot be processed</response>
    /// <response code="404">Transaction with the specified ID was not found</response>
    /// <response code="500">Internal server error occurred</response>
    /// <remarks>
    /// This operation:
    /// - Changes transaction status from Pending to Completed
    /// - Records the processing user and timestamp
    /// - Sets the transaction date to current time
    /// - Can only be performed on OUT transactions in Pending status
    /// 
    /// Sample request:
    /// 
    ///     PATCH /api/inventory-transactions/123/process
    ///     {
    ///         "processedByUserId": "user-123"
    ///     }
    /// </remarks>
    [HttpPatch("{id:int}/process")]
    [ProducesResponseType(typeof(InventoryTransactionDto), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
    public async Task<IActionResult> ProcessOutTransaction(int id, [FromBody] ProcessTransactionRequest request)
    {
        _logger.LogInformation("API.TRANSACTIONS.PROCESS: Processing OUT transaction ID: {TransactionId} by user: {UserId}",
            id, request?.ProcessedByUserId ?? "null");

        if (!ModelState.IsValid || string.IsNullOrWhiteSpace(request?.ProcessedByUserId))
        {
            _logger.LogWarning("API.TRANSACTIONS.PROCESS.VALIDATION: Invalid request for transaction ID: {TransactionId}", id);
            return BadRequest("ProcessedByUserId is required.");
        }

        var result = await _transactionService.ProcessOutTransactionAsync(id, request!.ProcessedByUserId);

        if (!result.Success)
        {
            _logger.LogWarning("API.TRANSACTIONS.PROCESS.FAILED: Failed to process transaction ID: {TransactionId} - Error: {ErrorMessage}",
                id, result.ErrorMessage);
        }

        return HandleServiceResult(result);
    }

    /// <summary>
    /// Cancels a transaction, marking it as cancelled with audit information.
    /// </summary>
    /// <param name="id">The unique identifier of the transaction to cancel (must be positive integer)</param>
    /// <param name="request">The request containing the ID of the user cancelling the transaction</param>
    /// <returns>
    /// The cancelled transaction with updated status and cancellation information.
    /// </returns>
    /// <response code="200">Transaction was successfully cancelled</response>
    /// <response code="400">Invalid parameters or transaction cannot be cancelled</response>
    /// <response code="404">Transaction with the specified ID was not found</response>
    /// <response code="500">Internal server error occurred</response>
    /// <remarks>
    /// This operation:
    /// - Changes transaction status to Cancelled
    /// - Records the cancelling user and timestamp
    /// - Can only be performed on OUT transactions that are not already completed or cancelled
    /// - Preserves all original transaction data for audit purposes
    /// 
    /// Sample request:
    /// 
    ///     PATCH /api/inventory-transactions/123/cancel
    ///     {
    ///         "cancelledByUserId": "user-456"
    ///     }
    /// </remarks>
    [HttpPatch("{id:int}/cancel")]
    [ProducesResponseType(typeof(InventoryTransactionDto), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
    public async Task<IActionResult> CancelTransaction(int id, [FromBody] CancelTransactionRequest request)
    {
        _logger.LogInformation("API.TRANSACTIONS.CANCEL: Cancelling transaction ID: {TransactionId} by user: {UserId}",
            id, request?.CancelledByUserId ?? "null");

        if (!ModelState.IsValid || string.IsNullOrWhiteSpace(request?.CancelledByUserId))
        {
            _logger.LogWarning("API.TRANSACTIONS.CANCEL.VALIDATION: Invalid request for transaction ID: {TransactionId}", id);
            return BadRequest("CancelledByUserId is required.");
        }

        var result = await _transactionService.CancelTransactionAsync(id, request!.CancelledByUserId);

        if (!result.Success)
        {
            _logger.LogWarning("API.TRANSACTIONS.CANCEL.FAILED: Failed to cancel transaction ID: {TransactionId} - Error: {ErrorMessage}",
                id, result.ErrorMessage);
        }

        return HandleServiceResult(result);
    }
}

/// <summary>
/// Request model for processing transactions.
/// </summary>
public class ProcessTransactionRequest
{
    /// <summary>
    /// The ID of the user processing the transaction.
    /// </summary>
    public string ProcessedByUserId { get; set; } = string.Empty;
}

/// <summary>
/// Request model for cancelling transactions.
/// </summary>
public class CancelTransactionRequest
{
    /// <summary>
    /// The ID of the user cancelling the transaction.
    /// </summary>
    public string CancelledByUserId { get; set; } = string.Empty;
}


