using SUIA.API.Contracts;
using SUIA.Shared.Models;

namespace SUIA.API.Endpoints;

public sealed class RolesEndpoint(IRoleService service) : IEndpoints
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

    private async Task<IResult> GetAllRoles(CancellationToken cancellationToken)
        => Results.Ok(await service.GetAll(cancellationToken));

    private async Task<IResult> GetRole(string id, CancellationToken cancellationToken)
    {        
        var existingRole = await service.GetById(id, cancellationToken);
        if (existingRole is null) return Results.NotFound();
        return Results.Ok(existingRole);
    }

    private async Task<IResult> CreateRole(RolesDto rolesDto, CancellationToken cancellationToken)
    {
        var result = await service.Create(rolesDto, cancellationToken);
        if (result is null) return Results.BadRequest("Failed to create role.");
        return Results.Ok(result);
    }

    private async Task<IResult> UpdateRole(string id, RolesDto rolesDto, CancellationToken cancellationToken)
    {
        var result = await service.UpdateById(id, rolesDto, cancellationToken);
        if (result) return Results.NoContent();
        return Results.BadRequest("Failed to update role.");        
    }

    private async Task<IResult> DeleteRole(string id, CancellationToken cancellationToken)
    {
        var result = await service.DeleteById(id, cancellationToken);
        if (result) return Results.NoContent();
        return Results.BadRequest("Failed to delete role.");
    }
}
