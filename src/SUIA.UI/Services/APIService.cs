using SUIA.Shared.Models;
using SUIA.Shared.Utilities;
using SUIA.UI.Endpoints;
using Sysinfocus.AspNetCore.Components;
using System.Net;
using System.Net.Http.Json;

namespace SUIA.UI.Services;

public interface IAPIService
{
    //ValueTask<Results<string?>> GetStringAsync(string endpoints, CancellationToken cancellationToken = default);
    ValueTask<Results<TOutput>> GetAsync<TOutput>(string endpoints, CancellationToken cancellationToken = default);
    ValueTask<Results<string?>> PostAsync<TInput>(string endpoints, TInput requestBody, CancellationToken cancellationToken = default);
    ValueTask<Results<TOutput>> PostAsync<TInput, TOutput>(string endpoints, TInput requestBody, CancellationToken cancellationToken = default);
    ValueTask<Results<TOutput>> PutAsync<TInput, TOutput>(string endpoints, TInput requestBody, CancellationToken cancellationToken = default);
    ValueTask<Results<TOutput>> DeleteAsync<TOutput>(string endpoints, CancellationToken cancellationToken = default);
}

public interface IEmpty;

public record Results<TOutput>(HttpStatusCode StatusCode, TOutput? Data, string? Message)
{
    public bool IsSuccess => ((int)StatusCode) >= 200 && ((int)StatusCode) < 300;
};

public class APIService(BrowserExtensions browserExtensions, HttpClient httpClient, Settings setting, ILogger<IAPIService> logger) : IAPIService
{
    public async ValueTask<Results<TOutput>> GetAsync<TOutput>(string endpoints, CancellationToken cancellationToken = default)
    {
        endpoints = await SetEndpointAuthentication(endpoints);
        try
        {
            var result = await httpClient.GetAsync(endpoints, cancellationToken);
            return await APIResult<TOutput>(result, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message);
            return new Results<TOutput>(HttpStatusCode.InternalServerError, default, ex.InnerException?.Message);
        }
    }

    public async ValueTask<Results<string?>> PostAsync<TInput>(string endpoints, TInput requestBody, CancellationToken cancellationToken = default)
    {
        endpoints = await SetEndpointAuthentication(endpoints);
        try
        {
            var result = await httpClient.PostAsJsonAsync(endpoints, requestBody, cancellationToken);
            if (!result.IsSuccessStatusCode) return new Results<string?>(result.StatusCode, default, result.ReasonPhrase);
            var data = await result.Content.ReadAsStringAsync(cancellationToken: cancellationToken);
            return new Results<string?>(result.StatusCode, data, null);
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message);
            return new Results<string?>(HttpStatusCode.InternalServerError, ex.Message, ex.InnerException?.Message);
        }
    }

    public async ValueTask<Results<TOutput>> PostAsync<TInput, TOutput>(string endpoints, TInput requestBody, CancellationToken cancellationToken = default)
    {
        endpoints = await SetEndpointAuthentication(endpoints);
        try
        {
            var result = await httpClient.PostAsJsonAsync(endpoints, requestBody, cancellationToken);
            return await APIResult<TOutput>(result, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message);
            return new Results<TOutput>(HttpStatusCode.InternalServerError, default, ex.InnerException?.Message);
        }
    }

    public async ValueTask<Results<TOutput>> PutAsync<TInput, TOutput>(string endpoints, TInput requestBody, CancellationToken cancellationToken = default)
    {
        endpoints = await SetEndpointAuthentication(endpoints);
        try
        {
            var result = await httpClient.PutAsJsonAsync(endpoints, requestBody, cancellationToken);
            return await APIResult<TOutput>(result, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message);
            return new Results<TOutput>(HttpStatusCode.InternalServerError, default, ex.InnerException?.Message);
        }
    }

    public async ValueTask<Results<TOutput>> DeleteAsync<TOutput>(string endpoints, CancellationToken cancellationToken = default)
    {
        endpoints = await SetEndpointAuthentication(endpoints);
        try
        {
            var result = await httpClient.DeleteAsync(endpoints, cancellationToken);
            return await APIResult<TOutput>(result, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message);
            return new Results<TOutput>(HttpStatusCode.InternalServerError, default, ex.InnerException?.Message);
        }
    }

    private static async ValueTask<Results<TOutput>> APIResult<TOutput>(HttpResponseMessage result, CancellationToken cancellationToken)
    {
        if (!result.IsSuccessStatusCode) return new Results<TOutput>(result.StatusCode, default, result.ReasonPhrase);        
        if (typeof(TOutput).Name is nameof(IEmpty)) return new Results<TOutput>(result.StatusCode, default, null);
        var data = await result.Content.ReadFromJsonAsync<TOutput>(cancellationToken: cancellationToken);
        return new Results<TOutput>(result.StatusCode, data, null);
    }

    private async Task<string> SetEndpointAuthentication(string endpoint)
    {
        if (!endpoint.StartsWith("http")) endpoint = setting.ApiEndpoint + endpoint;
        httpClient.DefaultRequestHeaders.Remove("Authorization");
        var auth = await browserExtensions.GetFromLocalStorage("session");
        if (auth is null) return endpoint;
        var tokens = auth.FromJson<LoginResponse>();
        if (httpClient.DefaultRequestHeaders.Authorization is null)
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {tokens.AccessToken}");
        return endpoint;
    }

    private async Task ResetRefreshToken()
    {
        var auth = await browserExtensions.GetFromLocalStorage("session");
        if (auth is null) return;
        var tokens = auth.FromJson<LoginResponse>();
        var body = new RefreshTokenRequest { RefreshToken = tokens.RefreshToken };
        Results<LoginResponse> result = await PostAsync<RefreshTokenRequest, LoginResponse>(EndpointConstants.REFRESH_TOKEN, body);
        if (result.IsSuccess && result.Data is not null)
        {
            tokens.AccessToken = result.Data.AccessToken;
            tokens.ExpiresIn = result.Data.ExpiresIn;
            tokens.RefreshToken = result.Data.RefreshToken;
            tokens.TokenType = result.Data.TokenType;
        }
        await browserExtensions.SetToLocalStorage("session", tokens.ToJson());
    }
}