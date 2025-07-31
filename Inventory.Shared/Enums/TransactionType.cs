using System.Text.Json.Serialization;

namespace Inventory.Shared.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TransactionType
{
    In,
    Out,
    Adjustment
}
