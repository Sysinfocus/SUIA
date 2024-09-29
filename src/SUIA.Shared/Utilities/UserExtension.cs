using SUIA.Shared.Models;
using System.Text;

namespace SUIA.Shared.Utilities;
public static class UserExtension
{
    public static UserClaimsDto? FromRawClaims(this string claims)
    {
        var claimsModel = claims.FromJson<LoginResponseDto>();
        if (claimsModel.Claims is null) return default;
        return Encoding.UTF8.GetString(Convert.FromBase64String(claimsModel.Claims!)).FromJson<UserClaimsDto>();
    }
}
