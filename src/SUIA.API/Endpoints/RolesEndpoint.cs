using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SUIA.API.Data;
using SUIA.Shared.Models;

namespace SUIA.API.Endpoints;

public sealed class RolesEndpoint : IEndpoints
{
    public void Register(IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/roles").WithTags("Roles").RequireAuthorization();
        group.MapGet("/{id}", GetRole);
        group.MapGet("/", GetAllRoles);

        group.MapPost("/", CreateRole);

        group.MapPut("/{id}", UpdateRole);

        group.MapDelete("/{id}", DeleteRole);
    }

    private async Task<IResult> GetAllRoles(HttpContext context, ApplicationDbContext adbc, CancellationToken cancellationToken)
    {
        return Results.Ok(await adbc.Roles.ToListAsync(cancellationToken));
    }

    private async Task<IResult> GetRole(string id, ApplicationDbContext adbc, RoleManager<IdentityRole> roleManager, CancellationToken cancellationToken)
    {        
        var existingRole = await roleManager.FindByIdAsync(id);
        if (existingRole is null) return Results.NotFound();
        return Results.Ok(existingRole);
    }

    private async Task<IResult> CreateRole(RoleModel model, ApplicationDbContext adbc, RoleManager<IdentityRole> roleManager, CancellationToken cancellationToken)
    {
        var role = new IdentityRole(model.Name);
        var result = await roleManager.CreateAsync(role);
        if (result.Errors.Any()) return Results.Problem(result.Errors.First().Description, null, 400, "Failed to create role.");
        return Results.Ok(result);
    }

    private async Task<IResult> UpdateRole(string id, RoleModel model, ApplicationDbContext adbc, RoleManager<IdentityRole> roleManager, CancellationToken cancellationToken)
    {
        var existingRole = await roleManager.FindByIdAsync(id);
        if (existingRole is null) return Results.NotFound();
        existingRole.Name = model.Name;
        existingRole.NormalizedName = model.NormalizedName;        
        var result = await roleManager.UpdateAsync(existingRole);
        if (result.Errors.Any()) return Results.Problem(result.Errors.First().Description, null, 400, "Failed to update role.");
        return Results.NoContent();
    }

    private async Task<IResult> DeleteRole(string id, ApplicationDbContext adbc, RoleManager<IdentityRole> roleManager, CancellationToken cancellationToken)
    {
        var existingRole = await roleManager.FindByIdAsync(id);
        if (existingRole is null) return Results.NotFound();
        var result = await roleManager.DeleteAsync(existingRole);
        if (result.Errors.Any()) return Results.Problem(result.Errors.First().Description, null, 400, "Failed to delete role.");
        return Results.NoContent();
    }
}
