using Inventory.Api.Common;
using Inventory.Shared.Dtos.Common;
using Inventory.Shared.Dtos.InventoryTransactions;
using Inventory.Shared.Enums;

namespace Inventory.Api.Interfaces;

public interface IInventoryTransactionService
{
    Task<ServiceResult<PagedResult<InventoryTransactionSummaryDto>>> GetAllTransactionsAsync(
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
        bool sortDescending = false
    );
    Task<ServiceResult<InventoryTransactionDto>> GetTransactionByIdAsync(int id);
    Task<ServiceResult<InventoryTransactionDto>> GetTransactionByItemIdAsync(int itemId);
    Task<ServiceResult<InventoryTransactionDto>> GetTransactionByEmployeeIdAsync(int employeeId);
    Task<ServiceResult<InventoryTransactionDto>> GetTransactionByUserIdAsync(string userId);
    Task<ServiceResult<PagedResult<InventoryTransactionSummaryDto>>> GetPendingOutTransactionsAsync(
        int page = 1,
        int pageSize = 20,
        string? sortBy = null,
        bool sortDescending = false
    );
    Task<ServiceResult<InventoryTransactionDto>> CreateInTransactionAsync(
        CreateInInventoryTransactionDto inTransactionDto,
        string receivedByUserId
    );
    Task<ServiceResult<InventoryTransactionDto>> CreateOutTransactionAsync(CreateOutInventoryTransactionDto outTransactionDto);
    Task<ServiceResult<InventoryTransactionDto>> ProcessOutTransactionAsync(int id, string processedByUserId);
    Task<ServiceResult<InventoryTransactionDto>> CancelTransactionAsync(int id, string cancelledByUserId);
}
