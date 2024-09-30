using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using SUIA.API.Contracts;
using SUIA.API.Data;
using SUIA.Shared.Models;

namespace SUIA.API.Endpoints;

public sealed class UsersEndpoint(IUserService service) : IEndpoints
{
    public void Register(IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/users").WithTags("Users").RequireAuthorization();
        group.MapGet("/{id}", GetUser);
        group.MapGet("/", GetAllUsers);
        group.MapGet("/claims", GetUserClaims);
        group.MapGet("/logout", LogoutUser);

        group.MapPost("/", CreateUser);

        group.MapPut("/{id}", UpdateUser);
        group.MapPut("/changePassword/{id}", UpdatePassword);

        group.MapDelete("/{id}", DeleteUser);
    }

    private async Task<IResult> GetAllUsers(CancellationToken cancellationToken)
        => Results.Ok(await service.GetAll(cancellationToken));

    private async Task<IResult> GetUser(string id, CancellationToken cancellationToken)
    {
        var user = await service.GetById(id, cancellationToken);        
        if (user is null) return Results.NotFound();
        return Results.Ok(user);
    }

    private async Task<IResult> GetUserClaims(ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        var claims = await service.GetUserClaims(user, cancellationToken);
        if (string.IsNullOrEmpty(claims)) return Results.Unauthorized();
        return Results.Content(claims);        
    }

    private async Task<IResult> LogoutUser(SignInManager<IdentityUser> signInManager, CancellationToken cancellationToken)
    {
        await signInManager.SignOutAsync();
        return Results.Ok();
    }

    private async Task CreateUser(CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
    }

    private async Task<IResult> UpdateUser(string id, UserDto model, CancellationToken cancellationToken)
    {
        var result = await service.UpdateById(id, model, cancellationToken);
        if (result) return Results.NoContent();
        return Results.BadRequest("Failed to update user.");
    }

    private async Task<IResult> UpdatePassword(string id, ChangePasswordRequestDto request, ApplicationDbContext adbc, UserManager<IdentityUser> userManager, CancellationToken cancellationToken)
    {
        var user = await adbc.Users.FindAsync([id], cancellationToken: cancellationToken);
        if (user is null) return Results.NotFound();        
        var result = await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (result.Errors.Any()) return Results.Problem(result.Errors.First().Description, statusCode: 400, title: "Change password failed");
        return Results.NoContent();
    }

    private async Task<IResult> DeleteUser(string id, CancellationToken cancellationToken)
    {
        var result = await service.DeleteById(id, cancellationToken);
        if (result) return Results.NoContent();
        return Results.BadRequest("Failed to delete user.");
    }
}
