namespace SUIA.Shared.Models;

public sealed class RolesDto : ModelValidator
{
    public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string NormalizedName => Name?.ToUpper()!;
    public string ConcurrencyStamp { get; set; } = Guid.NewGuid().ToString();

    public override bool IsValid()
    {
        ClearErrors();
        if (string.IsNullOrWhiteSpace(Name)) ErrorFor(nameof(Name), "Role Name is required.");        
        return !HasErrors();
    }
}