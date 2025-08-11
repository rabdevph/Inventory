using Inventory.Shared.Dtos.Users;

namespace Inventory.Shared.Dtos.Auth;

public class LoginResponseDto
{
    public UserDto User { get; set; } = null!;
    public string Message { get; set; } = string.Empty;
}
