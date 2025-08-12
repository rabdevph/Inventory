using System.ComponentModel.DataAnnotations;

namespace Inventory.Shared.Dtos.Auth;

public class LoginDto
{
    [Required]
    public string Username { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string Password { get; set; } = string.Empty;
}
