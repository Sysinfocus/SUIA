﻿namespace SUIA.Shared.Models;

public class UserClaimsModel
{
    public string Id { get; set; } = default!;
    public string UserName { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string? Roles { get; set; }
}