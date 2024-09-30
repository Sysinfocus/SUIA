using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using SUIA.Shared.Models;
using SUIA.Shared.Utilities;
using System.Net;
using System.Net.Http.Json;
using System.Text;

namespace SUIA.UI.Services;

/*
1. Initiate Get, Post, Put, Delete request.
2. If response is not Unauthorized, complete the request.
3. If response is Unauthorized and Authorization header is empty, complete the request.
4. If response is Unauthorized and Authorization header is not empty, Initiate ResetRefreshToken method.
5. If response is not Unauthorized, retry the previous request.
6. If response is Unauthorized, complete the request.
*/

public sealed class APIService(HttpClient httpClient, Settings settings, IJSRuntime jSRuntime, NavigationManager navigationManager) : IAPIService
{
    private bool isCheckingForRefreshToken, refreshTokenFailed, retryLastApiCall;
    public bool IsRefreshTokenFailed => refreshTokenFailed;

    private async ValueTask<HttpRequestMessage> CreateRequestWithAuthorizationToken(string endpoint, HttpMethod httpMethod, CancellationToken cancellation)
    {
        var token = await jSRuntime.InvokeAsync<string>("localStorage.getItem", "session");
        endpoint = endpoint.StartsWith("http") ? endpoint : settings.ApiEndpoint + endpoint;
        var message = new HttpRequestMessage(httpMethod, endpoint);        
        if (token is null) return message;
        var user = token.FromJson<LoginResponseDto>();
        message.Headers.Add("Authorization", $"Bearer {user.AccessToken}");
        return message;
    }

    private static void SetRequestBody<T>(T value, ref HttpRequestMessage message)
        => message.Content = new StringContent(value.ToJson(), Encoding.UTF8, "application/json");

    private async Task<Results<TOutput>> ProcessResponse<TOutput>(HttpResponseMessage? response, CancellationToken cancellationToken)
    {
        var session = await jSRuntime.InvokeAsync<string>("localStorage.getItem", "session");
        if (refreshTokenFailed || response is null) return default!;
        if (response.IsSuccessStatusCode)
        {
            if (typeof(TOutput).Name is nameof(IEmpty))
            {
                return new Results<TOutput>(response.StatusCode, default, null);
            }
            else if (typeof(TOutput).Name is nameof(String))
            {
                var stringData = await response.Content.ReadAsStringAsync(cancellationToken);
                return new Results<TOutput>(response.StatusCode, default, null, stringData);
            }
            else
            {
                var outputData = await response.Content.ReadFromJsonAsync<TOutput>(cancellationToken);
                return new Results<TOutput>(response.StatusCode, outputData, null);
            }
        }
        else
        {            
            if (response.StatusCode == HttpStatusCode.Unauthorized && isCheckingForRefreshToken == false)
            {
                isCheckingForRefreshToken = true;
                await ProcessRefreshToken(session, cancellationToken);
                if (refreshTokenFailed == false && isCheckingForRefreshToken == false)
                    retryLastApiCall = true;
                return default!;
            }
            else
            {
                try
                {
                    var validationProblem = await response.Content.ReadFromJsonAsync<ValidationProblem>(cancellationToken);
                    return new Results<TOutput>(response.StatusCode, default, response.ReasonPhrase) { Errors = validationProblem };
                }
                catch
                {
                    return new Results<TOutput>(response.StatusCode, default, response.ReasonPhrase);
                }
            }
        }
    }

    private async ValueTask ProcessRefreshToken(string? session, CancellationToken cancellationToken)
    {
        if (!isCheckingForRefreshToken) return;
        if (session is null)
        {            
            refreshTokenFailed = true;
            return;
        }
        var tokens = session.FromJson<LoginResponseDto>();
        var body = new { refreshToken = tokens.RefreshToken };
        var message = new HttpRequestMessage(HttpMethod.Post, settings.ApiEndpoint + "api/identity/refresh")
        {            
            Content = new StringContent(body.ToJson(), Encoding.UTF8, "application/json")
        };
        message.Headers.Add("Authorization", $"Bearer {tokens.AccessToken}");
        var response = await httpClient.SendAsync(message, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            var newTokens = await response.Content.ReadFromJsonAsync<LoginResponseDto>(cancellationToken);
            if (newTokens is null)
            {
                refreshTokenFailed = true;
                return;
            }
            newTokens.Claims = tokens.Claims;
            await jSRuntime.InvokeVoidAsync("localStorage.setItem", "session", newTokens.ToJson());
            isCheckingForRefreshToken = false;
            refreshTokenFailed = false;
            return;
        }
        refreshTokenFailed = true;
        navigationManager.NavigateTo("Login", true, true);
        return;
    }

    public async ValueTask<Results<TOutput>> GetAsync<TOutput>(string endpoints, CancellationToken cancellationToken = default)
    {
        retry:
        retryLastApiCall = false;
        var message = await CreateRequestWithAuthorizationToken(endpoints, HttpMethod.Get, cancellationToken);
        var response = await httpClient.SendAsync(message, cancellationToken);
        var result = await ProcessResponse<TOutput>(response, cancellationToken);
        if (retryLastApiCall) goto retry;
        return result;
    }    

    public async ValueTask<Results<TOutput>> PostAsync<TInput, TOutput>(string endpoints, TInput requestBody, CancellationToken cancellationToken = default)
    {
        retry:
        retryLastApiCall = false;
        var message = await CreateRequestWithAuthorizationToken(endpoints, HttpMethod.Post, cancellationToken);
        SetRequestBody(requestBody, ref message);
        var response = await httpClient.SendAsync(message, cancellationToken);
        var result = await ProcessResponse<TOutput>(response, cancellationToken);
        if (retryLastApiCall) goto retry;
        return result;
    }

    public async ValueTask<Results<TOutput>> PutAsync<TInput, TOutput>(string endpoints, TInput requestBody, CancellationToken cancellationToken = default)
    {
        retry:
        retryLastApiCall = false;
        var message = await CreateRequestWithAuthorizationToken(endpoints, HttpMethod.Put, cancellationToken);
        SetRequestBody(requestBody, ref message);
        var response = await httpClient.SendAsync(message, cancellationToken);
        var result = await ProcessResponse<TOutput>(response, cancellationToken);
        if (retryLastApiCall) goto retry;
        return result;
    }
    public async ValueTask<Results<TOutput>> DeleteAsync<TOutput>(string endpoints, CancellationToken cancellationToken = default)
    {
        retry:
        retryLastApiCall = false;
        var message = await CreateRequestWithAuthorizationToken(endpoints, HttpMethod.Delete, cancellationToken);
        var response = await httpClient.SendAsync(message, cancellationToken);
        var result = await ProcessResponse<TOutput>(response, cancellationToken);
        if (retryLastApiCall) goto retry;
        return result;
    }
}

public record Results<TOutput>(HttpStatusCode StatusCode, TOutput? Data, string? Message, string? StringValue = null)
{
    public bool IsSuccess => ((int)StatusCode) >= 200 && ((int)StatusCode) < 300;
    public ValidationProblem? Errors { get; set; }
};