using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SUIA.API.Data;
using SUIA.Shared.Models;
using SUIA.Shared.Utilities;

namespace SUIA.API.Endpoints;

public sealed class UsersEndpoint : IEndpoints
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

    private async Task<IResult> GetAllUsers(HttpContext context, ApplicationDbContext adbc, CancellationToken cancellationToken)
    {
        return Results.Ok(await adbc.Users.ToListAsync(cancellationToken));
    }

    private async Task<IResult> GetUser(string id, ApplicationDbContext adbc, CancellationToken cancellationToken)
    {
        var user = await adbc.Users.FindAsync([id], cancellationToken: cancellationToken);
        if (user is null) return Results.NotFound();
        return Results.Ok(user);
    }

    private async Task<IResult> GetUserClaims(ClaimsPrincipal user, UserManager<IdentityUser> userManager)
    {
        var validUser = await userManager.GetUserAsync(user);
        if (validUser is null) return Results.Unauthorized();
        var claims = new UserClaimsModel
        {
            Id = validUser.Id,
            UserName = validUser.UserName!,
            Email = validUser.Email!,
            Roles = validUser.Email == "admin@sysinfocus.com" ? "Admin" : "User",
        };
        var toBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(claims.ToJson()));
        return Results.Ok(toBase64);
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

    private async Task<IResult> UpdateUser(string id, UserModel model, ApplicationDbContext adbc, CancellationToken cancellationToken)
    {
        var existingUser = await adbc.Users.FindAsync([id], cancellationToken: cancellationToken);
        if (existingUser is null) return Results.NotFound();
        existingUser.EmailConfirmed = model.EmailConfirmed;
        existingUser.PhoneNumberConfirmed = model.PhoneNumberConfirmed;
        existingUser.TwoFactorEnabled = model.TwoFactorEnabled;
        existingUser.LockoutEnabled = model.LockoutEnabled;
        adbc.Update(existingUser);
        await adbc.SaveChangesAsync(cancellationToken);
        return Results.NoContent();
    }

    private async Task<IResult> UpdatePassword(string id, ChangePasswordRequest request, ApplicationDbContext adbc, UserManager<IdentityUser> userManager, CancellationToken cancellationToken)
    {
        var user = await adbc.Users.FindAsync([id], cancellationToken: cancellationToken);
        if (user is null) return Results.NotFound();        
        var result = await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (result.Errors.Any()) return Results.Problem(result.Errors.First().Description, statusCode: 400, title: "Change password failed");
        return Results.NoContent();
    }

    private async Task<IResult> DeleteUser(string id, ApplicationDbContext adbc, CancellationToken cancellationToken)
    {
        var existingUser = await adbc.Users.FindAsync([id], cancellationToken: cancellationToken);
        if (existingUser is null) return Results.NotFound();
        adbc.Remove(existingUser);
        await adbc.SaveChangesAsync(cancellationToken);
        return Results.NoContent();
    }
}
