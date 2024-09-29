using SUIA.Shared.Models;
using SUIA.Shared.Utilities;
using SUIA.UI.Endpoints;
using Sysinfocus.AspNetCore.Components;
using System.Net;
using System.Net.Http.Json;
using System.Text;

namespace SUIA.UI.Services;

public record Results<TOutput>(HttpStatusCode StatusCode, TOutput? Data, string? Message)
{
    public bool IsSuccess => ((int)StatusCode) >= 200 && ((int)StatusCode) < 300;
    public ValidationProblem? Errors { get; set; }
};

public class APIService(BrowserExtensions browserExtensions, HttpClient httpClient, Settings setting) : IAPIService
{
    private bool isCheckingForRefreshToken;
    private async ValueTask<LoginResponseDto?> GetTokens()
    {
        var auth = await browserExtensions.GetFromLocalStorage("session", null);
        return auth?.FromJson<LoginResponseDto>();
    }

    public async ValueTask<Results<TOutput>> GetAsync<TOutput>(string endpoints, CancellationToken cancellationToken = default)
    {
        retry:
        endpoints = await SetEndpointAuthentication(endpoints);
        try
        {
            var result = await httpClient.GetAsync(endpoints, cancellationToken);
            if (isCheckingForRefreshToken == false && result.StatusCode != HttpStatusCode.Unauthorized || httpClient.DefaultRequestHeaders.Authorization is null)
            {
                return await APIResult<TOutput>(result, cancellationToken);
            }
            else
            {
                isCheckingForRefreshToken = true;
                var tokens = await GetTokens();
                if (tokens is not null && await ResetRefreshToken(tokens))
                    goto retry;
                else if (tokens is not null)
                    browserExtensions.Goto("Logout", true);
                return default!;
            }
        }
        catch (Exception ex)
        {            
            return new Results<TOutput>(HttpStatusCode.InternalServerError, default, ex.Message);
        }
    }

    public async ValueTask<Results<string?>> PostAsync<TInput>(string endpoints, TInput requestBody, CancellationToken cancellationToken = default)
    {
        retry_post:
        endpoints = await SetEndpointAuthentication(endpoints);
        try
        {
            var result = await httpClient.PostAsJsonAsync(endpoints, requestBody, cancellationToken);
            if (isCheckingForRefreshToken == false && result.StatusCode != HttpStatusCode.Unauthorized || httpClient.DefaultRequestHeaders.Authorization is null)
            {
                if (!result.IsSuccessStatusCode) return new Results<string?>(result.StatusCode, default, result.ReasonPhrase);
                var data = await result.Content.ReadAsStringAsync(cancellationToken: cancellationToken);
                return new Results<string?>(result.StatusCode, data, null);                
            }
            else
            {
                isCheckingForRefreshToken = true;
                var tokens = await GetTokens();
                if (tokens is not null && await ResetRefreshToken(tokens))
                    goto retry_post;
                else if (tokens is not null)
                    browserExtensions.Goto("Logout", true);
                return default!;
            }
        }
        catch (Exception ex)
        {
            return new Results<string?>(HttpStatusCode.InternalServerError, default, ex.Message);
        }
    }

    public async ValueTask<Results<TOutput>> PostAsync<TInput, TOutput>(string endpoints, TInput requestBody, CancellationToken cancellationToken = default)
    {
        retry_post_2:
        endpoints = await SetEndpointAuthentication(endpoints);
        try
        {
            var result = await httpClient.PostAsJsonAsync(endpoints, requestBody, cancellationToken);
            if (isCheckingForRefreshToken == false && result.StatusCode != HttpStatusCode.Unauthorized || httpClient.DefaultRequestHeaders.Authorization is null)
            {
                return await APIResult<TOutput>(result, cancellationToken);
            }
            else
            {
                isCheckingForRefreshToken = true;
                var tokens = await GetTokens();
                if (tokens is not null && await ResetRefreshToken(tokens))
                    goto retry_post_2;
                else if (tokens is not null)
                    browserExtensions.Goto("Logout", true);
                return default!;
            }            
        }
        catch (Exception ex)
        {
            return new Results<TOutput>(HttpStatusCode.InternalServerError, default, ex.Message);
        }
    }

    public async ValueTask<Results<TOutput>> PutAsync<TInput, TOutput>(string endpoints, TInput requestBody, CancellationToken cancellationToken = default)
    {
        retry_put:
        endpoints = await SetEndpointAuthentication(endpoints);
        try
        {
            var result = await httpClient.PutAsJsonAsync(endpoints, requestBody, cancellationToken);
            if (isCheckingForRefreshToken == false && result.StatusCode != HttpStatusCode.Unauthorized || httpClient.DefaultRequestHeaders.Authorization is null)
            {
                return await APIResult<TOutput>(result, cancellationToken);
            }
            else
            {
                isCheckingForRefreshToken = true;
                var tokens = await GetTokens();
                if (tokens is not null && await ResetRefreshToken(tokens))
                    goto retry_put;
                else if (tokens is not null)
                    browserExtensions.Goto("Logout", true);
                return default!;
            }
        }
        catch (Exception ex)
        {
            return new Results<TOutput>(HttpStatusCode.InternalServerError, default, ex.Message);
        }
    }

    public async ValueTask<Results<TOutput>> DeleteAsync<TOutput>(string endpoints, CancellationToken cancellationToken = default)
    {
        retry_delete:
        endpoints = await SetEndpointAuthentication(endpoints);
        try
        {
            var result = await httpClient.DeleteAsync(endpoints, cancellationToken);
            if (isCheckingForRefreshToken == false && result.StatusCode != HttpStatusCode.Unauthorized || httpClient.DefaultRequestHeaders.Authorization is null)
            {
                return await APIResult<TOutput>(result, cancellationToken);
            }
            else
            {
                isCheckingForRefreshToken = true;
                var tokens = await GetTokens();
                if (tokens is not null && await ResetRefreshToken(tokens))
                    goto retry_delete;
                else if (tokens is not null)
                    browserExtensions.Goto("Logout", true);
                return default!;
            }            
        }
        catch (Exception ex)
        {
            return new Results<TOutput>(HttpStatusCode.InternalServerError, default, ex.Message);
        }
    }

    private async ValueTask<Results<TOutput>> APIResult<TOutput>(HttpResponseMessage result, CancellationToken cancellationToken)
    {
        if (!result.IsSuccessStatusCode)
        {
            var errorData = await result.Content.ReadFromJsonAsync<ValidationProblem>(cancellationToken);
            return new Results<TOutput>(result.StatusCode, default, result.ReasonPhrase) { Errors = errorData };
        }
        if (typeof(TOutput).Name is nameof(IEmpty)) return new Results<TOutput>(result.StatusCode, default, null);           
        var data = await result.Content.ReadFromJsonAsync<TOutput>(cancellationToken: cancellationToken);
        return new Results<TOutput>(result.StatusCode, data, null);
    }

    private async ValueTask<string> SetEndpointAuthentication(string endpoint)
    {
        if (!endpoint.StartsWith("http")) endpoint = setting.ApiEndpoint + endpoint;
        httpClient.DefaultRequestHeaders.Remove("Authorization");
        var auth = await browserExtensions.GetFromLocalStorage("session");
        if (auth is null) return endpoint;
        var tokens = auth.FromJson<LoginResponseDto>();
        if (httpClient.DefaultRequestHeaders.Authorization is null)
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {tokens.AccessToken}");
        return endpoint;
    }

    private async ValueTask<bool> ResetRefreshToken(LoginResponseDto tokens)
    {
        if (isCheckingForRefreshToken == false) return false;
        isCheckingForRefreshToken = false;        
        var body = new RefreshTokenDto(tokens.RefreshToken);
        var newRequest = new HttpRequestMessage() {
            RequestUri = new Uri(setting.ApiEndpoint + EndpointConstants.REFRESH_TOKEN),
            Method = HttpMethod.Post,
            Content = new StringContent(body.ToJson(), Encoding.UTF8, "application/json")
        };
        newRequest.Headers.Add("Authorization", tokens.AccessToken);
        var result = await httpClient.SendAsync(newRequest);
        if (result.IsSuccessStatusCode)
        {            
            var data = await result.Content.ReadFromJsonAsync<LoginResponseDto>()!;
            var newTokens = new LoginResponseDto(data!.TokenType,data.AccessToken,data.ExpiresIn,data.RefreshToken)
            {
                Claims = tokens.Claims
            };
            await browserExtensions.SetToLocalStorage("session", newTokens.ToJson());
            return true;
        }
        else
        {
            if (tokens is not null) browserExtensions.Goto("Logout", true);
            return false;
        }
    }
}