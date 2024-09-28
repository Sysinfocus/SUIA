using SUIA.Shared.Models;
using System.Text;

namespace SUIA.Shared.Utilities;
public static class UserExtension
{
    public static UserClaimsModel? FromRawClaims(this string claims)
    {
        var claimsModel = claims.FromJson<LoginResponse>();
        if (claimsModel.Claims is null) return default;
        return Encoding.UTF8.GetString(Convert.FromBase64String(claimsModel.Claims!)).FromJson<UserClaimsModel>();
    }
}
