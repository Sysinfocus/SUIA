namespace SUIA.Shared.Models;
public sealed class UserDto
{
    public string Id { get; set; } = default!;
    public string Username { get; set; } = default!;
    public string Email { get; set; } = default!;
    public bool EmailConfirmed { get; set; }
    public bool PhoneNumberConfirmed { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public bool TwoFactorEnabled { get; set; }
    public string? LockoutEnd { get; set; }
    public bool LockoutEnabled { get; set; }
    public int AccessFailedCount { get; set; }
}