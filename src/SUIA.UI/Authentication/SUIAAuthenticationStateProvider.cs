using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using SUIA.Shared.Models;
using SUIA.Shared.Utilities;
using SUIA.UI.Endpoints;
using SUIA.UI.Services;
using Sysinfocus.AspNetCore.Components;
using System.Security.Claims;

namespace SUIA.UI.Authentication;

public class SUIAAuthenticationStateProvider(IJSRuntime jSRuntime, NavigationManager navigationManager, IAPIService api, StateManager stateManager) : AuthenticationStateProvider
{
    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {

        var identity = new ClaimsIdentity();
        var session = await jSRuntime.InvokeAsync<string>("localStorage.getItem", "session");
        if (session is null) return new AuthenticationState(new ClaimsPrincipal(identity));

        var url = navigationManager.ToBaseRelativePath(navigationManager.Uri);
        if (string.IsNullOrWhiteSpace(url) || url.StartsWith("login", StringComparison.OrdinalIgnoreCase))
            return new AuthenticationState(new ClaimsPrincipal(identity));

        var userClaims = session.FromRawClaims();
        if (userClaims is null) return new AuthenticationState(new ClaimsPrincipal(identity));

        if (await ValidateUser() == false) return new AuthenticationState(new ClaimsPrincipal(identity));

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

    private async ValueTask<bool> ValidateUser()
    {        
        var result = await api.GetAsync<UserInfoDto>(EndpointConstants.GET_INFO);
        if (result is null) return false;
        return result.StatusCode != System.Net.HttpStatusCode.Unauthorized;
    }

    public async Task Login(LoginResponseDto model)
    {
        if (model is null) return;
        await jSRuntime.InvokeVoidAsync("localStorage.setItem", "session", model.ToJson());
        var claims = await api.GetAsync<string>(EndpointConstants.GET_CLAIMS);
        if (claims is not null && model is not null)
        {
            model.Claims = claims.StringValue;
        }
        await jSRuntime.InvokeVoidAsync("localStorage.setItem", "session", model.ToJson());
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());        
    }  

    public async Task Logout()
    {
        stateManager.ClearState();
        try
        {
            await api.GetAsync<IEmpty>(EndpointConstants.LOGOUT);
        }
        finally
        {
            await jSRuntime.InvokeVoidAsync("localStorage.removeItem", "session");
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }
    }
}
