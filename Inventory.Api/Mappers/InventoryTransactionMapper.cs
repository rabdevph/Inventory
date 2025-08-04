using Inventory.Api.Models;
using Inventory.Shared.Dtos.InventoryTransactions;
using Inventory.Shared.Enums;

namespace Inventory.Api.Mappers;

// Static class containing extension methods for mapping InventoryTransaction entities and DTOs
public static class InventoryTransactionMapper
{
    // Converts a full InventoryTransaction entity to InventoryTransactionDto
    public static InventoryTransactionDto ToDto(this InventoryTransaction transaction)
    {
        return new InventoryTransactionDto
        {
            Id = transaction.Id,
            TransactionCode = transaction.TransactionCode,
            ItemId = transaction.ItemId,
            ItemName = transaction.Item?.Name ?? string.Empty,
            Quantity = transaction.Quantity,
            TransactionDate = transaction.TransactionDate,
            Type = transaction.TransactionType,
            Status = transaction.Status,
            Remarks = transaction.Remarks,
            CreatedAt = transaction.CreatedAt,
            ReceivedByUser = transaction.TransactionType == TransactionType.In
                ? transaction.ReceivedByUser?.FullName
                : null,
            RequestedByEmployee = transaction.TransactionType == TransactionType.Out
                ? transaction.RequestedByEmployee?.FullName
                : null,
            ProcessedByUser = transaction.TransactionType == TransactionType.Out
                ? transaction.ProcessedByUser?.FullName
                : null,
            CancelledByUser = transaction.Status == TransactionStatus.Cancelled
                ? transaction.CancelledByUser?.FullName
                : null,
            CancelledAt = transaction.Status == TransactionStatus.Cancelled
                ? transaction.CancelledAt
                : null
        };
    }

    // Converts InventoryTransaction entity to InventoryTransactionSummaryDto (lightweight version for lists/grids)
    public static InventoryTransactionSummaryDto ToSummaryDto(this InventoryTransaction transaction)
    {
        return new InventoryTransactionSummaryDto
        {
            Id = transaction.Id,
            TransactionCode = transaction.TransactionCode,
            ItemName = transaction.Item?.Name ?? string.Empty,
            Quantity = transaction.Quantity,
            TransactionDate = transaction.TransactionDate,
            Type = transaction.TransactionType,
            Status = transaction.Status,
            CreatedAt = transaction.CreatedAt
        };
    }

    // Converts a collection InventoryTransaction to a collection of InventoryTransactionDtos
    public static IEnumerable<InventoryTransactionDto> ToDto(this IEnumerable<InventoryTransaction> transactions)
    {
        return transactions.Select(ToDto);
    }

    // Converts a collection InventoryTransaction to a collection of InventoryTransactionSummaryDtos
    public static IEnumerable<InventoryTransactionSummaryDto> ToSummaryDto(this IEnumerable<InventoryTransaction> transactions)
    {
        return transactions.Select(ToSummaryDto);
    }

    // Converts a full InventoryTransaction entity to a DTO for IN transactions
    public static CreateInInventoryTransactionDto ToInDto(this InventoryTransaction transaction)
    {
        return new CreateInInventoryTransactionDto
        {
            ItemId = transaction.ItemId,
            Quantity = transaction.Quantity,
            ReceivedDate = transaction.TransactionDate,
            Remarks = transaction.Remarks
        };
    }

    // Converts a collection of InventoryTransaction entities to a collection of DTOs for IN transactions
    public static IEnumerable<CreateInInventoryTransactionDto> ToInDto(this IEnumerable<InventoryTransaction> transactions)
    {
        return transactions.Select(ToInDto);
    }

    // Creates a new InventoryTransaction entity from a CreateInInventoryTransactionDto (for IN requests)
    public static InventoryTransaction ToEntity(this CreateInInventoryTransactionDto dto)
    {
        return new InventoryTransaction
        {
            ItemId = dto.ItemId,
            Quantity = dto.Quantity,
            TransactionDate = dto.ReceivedDate ?? DateTime.UtcNow,
            Remarks = dto.Remarks,
            TransactionType = TransactionType.In,
            Status = TransactionStatus.Completed,
            CreatedAt = DateTime.UtcNow
        };
    }

    // Converts a full InventoryTransaction entity to a DTO for OUT transactions
    public static CreateOutInventoryTransactionDto ToOutDto(this InventoryTransaction transaction)
    {
        return new CreateOutInventoryTransactionDto
        {
            ItemId = transaction.ItemId,
            Quantity = transaction.Quantity,
            RequestedByEmployeeId = transaction.RequestedByEmployeeId ?? 0,
            Remarks = transaction.Remarks
        };
    }

    // Converts a collection of InventoryTransaction entities to a collection of DTOs for OUT transactions
    public static IEnumerable<CreateOutInventoryTransactionDto> ToOutDto(this IEnumerable<InventoryTransaction> transactions)
    {
        return transactions.Select(ToOutDto);
    }

    // Creates a new InventoryTransaction entity from a CreateOutInventoryTransactionDto (for OUT requests)
    public static InventoryTransaction ToEntity(this CreateOutInventoryTransactionDto dto)
    {
        return new InventoryTransaction
        {
            ItemId = dto.ItemId,
            Quantity = dto.Quantity,
            RequestedByEmployeeId = dto.RequestedByEmployeeId,
            Remarks = dto.Remarks,
            TransactionType = TransactionType.Out,
            Status = TransactionStatus.Pending,
            CreatedAt = DateTime.UtcNow
            // TransactionDate will be set when processed
        };
    }

    public static CreateInInventoryTransactionDto Clean(this CreateInInventoryTransactionDto inTransactionDto)
    {
        var trimmedRemarks = string.IsNullOrWhiteSpace(inTransactionDto.Remarks) ? null : inTransactionDto.Remarks.Trim();
        return new CreateInInventoryTransactionDto
        {
            ItemId = inTransactionDto.ItemId,
            Quantity = inTransactionDto.Quantity < 0 ? 0 : inTransactionDto.Quantity,
            ReceivedDate = inTransactionDto.ReceivedDate,
            Remarks = trimmedRemarks
        };
    }

    public static CreateOutInventoryTransactionDto Clean(this CreateOutInventoryTransactionDto outTransaction)
    {
        var trimmedRemarks = string.IsNullOrWhiteSpace(outTransaction.Remarks) ? null : outTransaction.Remarks.Trim();
        return new CreateOutInventoryTransactionDto
        {
            ItemId = outTransaction.ItemId,
            Quantity = outTransaction.Quantity < 0 ? 0 : outTransaction.Quantity,
            RequestedByEmployeeId = outTransaction.RequestedByEmployeeId,
            Remarks = trimmedRemarks
        };
    }
}
