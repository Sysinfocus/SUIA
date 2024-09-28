using Microsoft.AspNetCore.Components.Authorization;
using SUIA.Shared.Models;
using SUIA.Shared.Utilities;
using SUIA.UI.Endpoints;
using SUIA.UI.Services;
using Sysinfocus.AspNetCore.Components;
using System.Security.Claims;

namespace SUIA.UI.Authentication;

public class SUIAAuthenticationStateProvider(BrowserExtensions browserExtensions, IAPIService api, StateManager stateManager) : AuthenticationStateProvider
{
    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var identity = new ClaimsIdentity();
        var session = await browserExtensions.GetFromLocalStorage("session");
        if (session is null)
        {            
            return new AuthenticationState(new ClaimsPrincipal(identity));
        }

        var userClaims = session.FromRawClaims();
        if (userClaims is null) return new AuthenticationState(new ClaimsPrincipal(identity));

        identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, userClaims.Id),
            new Claim(ClaimTypes.Name, userClaims.UserName),
            new Claim(ClaimTypes.Email, userClaims.Email),
            new Claim(ClaimTypes.Role, userClaims.Roles!),
            new Claim("IsAdmin", userClaims.Roles == "Admin" ? "true" : "false"),
        ],
        "SUIA Authentication");
        var user = new ClaimsPrincipal(identity);
        stateManager.Publish("User", user);
        return new AuthenticationState(user);
    }

    public async Task Login(LoginResponse model)
    {
        if (model is null) return;
        await browserExtensions.SetToLocalStorage("session", model.ToJson());
        var claims = await api.GetAsync<string>(EndpointConstants.GET_CLAIMS);
        if (claims is not null && model is not null)
        {
            model.Claims = claims.Data;
        }
        await browserExtensions.SetToLocalStorage("session", model.ToJson());
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        browserExtensions.Goto("/Home");
    }

    public async Task Logout()
    {
        stateManager.ClearState();
        await api.GetAsync<IEmpty>(EndpointConstants.LOGOUT);
        await browserExtensions.RemoveFromLocalStorage("session");
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }
}
