using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SUIA.API.Contracts;
using SUIA.API.Data;
using SUIA.Shared.Models;

namespace SUIA.API.Services;

public class RoleService(ApplicationDbContext dbContext, RoleManager<IdentityRole> roleManager) : IRoleService
{
    public async ValueTask<IdentityRole[]> GetAll(CancellationToken cancellationToken)
        => await dbContext.Roles.ToArrayAsync(cancellationToken);

    public async ValueTask<IdentityRole?> GetById(string id, CancellationToken cancellationToken) => await roleManager.FindByIdAsync(id);

    public async ValueTask<IdentityRole?> Create(RolesDto rolesDto, CancellationToken cancellationToken)
    {
        var role = new IdentityRole(rolesDto.Name)
        {
            ConcurrencyStamp = Guid.NewGuid().ToString()
        };
        var result = await roleManager.CreateAsync(role);
        if (result.Errors.Any()) return null;
        return role;
    }

    public async ValueTask<bool> UpdateById(string id, RolesDto rolesDto, CancellationToken cancellationToken)
    {
        var newRole = await roleManager.FindByNameAsync(rolesDto.Name);
        if (newRole is not null && newRole.Id != id) return false;

        var existingRole = await roleManager.FindByIdAsync(id);
        if (existingRole is null) return false;
        existingRole.Name = rolesDto.Name;
        existingRole.NormalizedName = rolesDto.NormalizedName;
        var result = await roleManager.UpdateAsync(existingRole);
        if (result.Errors.Any()) return false;
        return true;
    }

    public async ValueTask<bool> DeleteById(string id, CancellationToken cancellationToken)
    {
        var existingRole = await roleManager.FindByIdAsync(id);
        if (existingRole is null) return false;
        var result = await roleManager.DeleteAsync(existingRole);
        if (result.Errors.Any()) return false;
        return true;
    }
}
