namespace SUIA.Shared.Models;

public class LoginRequestDto : ModelValidator
{
    public string Email { get; set; } = default!;
    public string Password { get; set; } = default!;

    public override bool IsValid()
    {
        ClearErrors();
        if (string.IsNullOrWhiteSpace(Email)) ErrorFor(nameof(Email), "Email is required.");
        else if (Email.IsEmail() == false) ErrorFor(nameof(Email), "Valid email address is required.");
        if (string.IsNullOrWhiteSpace(Password)) ErrorFor(nameof(Password), "Password is required.");
        else if (Password?.Length < 8 || Password?.Length > 20) ErrorFor(nameof(Password), "Password should be between 8 and 20 chars.");
        return !HasErrors();
    }
}