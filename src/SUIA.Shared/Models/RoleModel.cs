namespace SUIA.Shared.Models;

public sealed class RoleModel
{
    public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string NormalizedName => Name?.ToUpper()!;
    public string ConcurrencyStamp { get; set; } = Guid.NewGuid().ToString();
}