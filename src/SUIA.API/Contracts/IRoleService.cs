using Microsoft.AspNetCore.Identity;
using SUIA.Shared.Models;

namespace SUIA.API.Contracts;
public interface IRoleService : IServices
{
    ValueTask<IdentityRole?> Create(RolesDto rolesDto, CancellationToken cancellationToken);
    ValueTask<bool> DeleteById(string id, CancellationToken cancellationToken);
    ValueTask<IdentityRole[]> GetAll(CancellationToken cancellationToken);
    ValueTask<IdentityRole?> GetById(string id, CancellationToken cancellationToken);
    ValueTask<bool> UpdateById(string id, RolesDto rolesDto, CancellationToken cancellationToken);
}