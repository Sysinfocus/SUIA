namespace SUIA.Shared.Models;

public class ChangePasswordRequest
{
    public string Id { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string CurrentPassword { get; set; } = default!;
    public string NewPassword { get; set; } = default!;
}