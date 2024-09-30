using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using SUIA.Shared.Models;
using SUIA.Shared.Utilities;
using SUIA.UI.Endpoints;
using Sysinfocus.AspNetCore.Components;
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

//public class APIService(BrowserExtensions browserExtensions, HttpClient httpClient, Settings setting) : IAPIService
//{
//    private bool isCheckingForRefreshToken, refreshTokenFailed;
//    private async ValueTask<LoginResponseDto?> GetTokens()
//    {
//        var auth = await browserExtensions.GetFromLocalStorage("session", null);
//        return auth?.FromJson<LoginResponseDto>();
//    }

//    private async ValueTask<Results<TOutput>?> ProcessRequest<TOutput>(HttpResponseMessage result, CancellationToken cancellationToken)
//    {
//        if (isCheckingForRefreshToken == false &&
//            result.StatusCode != HttpStatusCode.Unauthorized ||
//            httpClient.DefaultRequestHeaders.Authorization is null)
//        {
//            return await APIResult<TOutput>(result, cancellationToken);
//        }
//        else
//        {
//            isCheckingForRefreshToken = true;
//            var tokens = await GetTokens();
//            if (tokens is not null && await ResetRefreshToken(tokens))
//                return null;
//            //else if (tokens is not null)
//            //    browserExtensions.Goto("Logout", true);
//            return null;
//        }
//    }

//    public async ValueTask<Results<TOutput>> GetAsync<TOutput>(string endpoints, CancellationToken cancellationToken = default)
//    {
//        retry:
//        if (refreshTokenFailed)
//        {
//            browserExtensions.Goto("Logout");
//            return default!;
//        }
//        endpoints = await SetEndpointAuthentication(endpoints);
//        try
//        {
//            var result = await httpClient.GetAsync(endpoints, cancellationToken);
//            var final = await ProcessRequest<TOutput>(result, cancellationToken);
//            if (final is not null) return final;
//            goto retry;            
//        }
//        catch (Exception ex)
//        {            
//            return new Results<TOutput>(HttpStatusCode.InternalServerError, default, ex.Message);
//        }
//    }

//    public async ValueTask<Results<string?>> PostAsync<TInput>(string endpoints, TInput requestBody, CancellationToken cancellationToken = default)
//    {
//        retry_post:
//        endpoints = await SetEndpointAuthentication(endpoints);
//        try
//        {
//            var result = await httpClient.PostAsJsonAsync(endpoints, requestBody, cancellationToken);
//            var final = await ProcessRequest<string?>(result, cancellationToken);
//            if (final is not null) return final;
//            goto retry_post;            
//        }
//        catch (Exception ex)
//        {
//            return new Results<string?>(HttpStatusCode.InternalServerError, default, ex.Message);
//        }
//    }

//    public async ValueTask<Results<TOutput>> PostAsync<TInput, TOutput>(string endpoints, TInput requestBody, CancellationToken cancellationToken = default)
//    {
//        retry_post_2:
//        endpoints = await SetEndpointAuthentication(endpoints);
//        try
//        {
//            var result = await httpClient.PostAsJsonAsync(endpoints, requestBody, cancellationToken);
//            var final = await ProcessRequest<TOutput>(result, cancellationToken);
//            if (final is not null) return final;
//            goto retry_post_2;
//        }
//        catch (Exception ex)
//        {
//            return new Results<TOutput>(HttpStatusCode.InternalServerError, default, ex.Message);
//        }
//    }

//    public async ValueTask<Results<TOutput>> PutAsync<TInput, TOutput>(string endpoints, TInput requestBody, CancellationToken cancellationToken = default)
//    {
//        retry_put:
//        endpoints = await SetEndpointAuthentication(endpoints);
//        try
//        {
//            var result = await httpClient.PutAsJsonAsync(endpoints, requestBody, cancellationToken);
//            var final = await ProcessRequest<TOutput>(result, cancellationToken);
//            if (final is not null) return final;
//            goto retry_put;
//        }
//        catch (Exception ex)
//        {
//            return new Results<TOutput>(HttpStatusCode.InternalServerError, default, ex.Message);
//        }
//    }

//    public async ValueTask<Results<TOutput>> DeleteAsync<TOutput>(string endpoints, CancellationToken cancellationToken = default)
//    {
//        retry_delete:
//        endpoints = await SetEndpointAuthentication(endpoints);
//        try
//        {
//            var result = await httpClient.DeleteAsync(endpoints, cancellationToken);
//            var final = await ProcessRequest<TOutput>(result, cancellationToken);
//            if (final is not null) return final;
//            goto retry_delete;
//        }
//        catch (Exception ex)
//        {
//            return new Results<TOutput>(HttpStatusCode.InternalServerError, default, ex.Message);
//        }
//    }

//    private async ValueTask<Results<TOutput>> APIResult<TOutput>(HttpResponseMessage result, CancellationToken cancellationToken)
//    {
//        if (!result.IsSuccessStatusCode)
//        {
//            var errorData = await result.Content.ReadFromJsonAsync<ValidationProblem>(cancellationToken);
//            return new Results<TOutput>(result.StatusCode, default, result.ReasonPhrase) { Errors = errorData };
//        }
//        if (typeof(TOutput).Name is nameof(IEmpty)) return new Results<TOutput>(result.StatusCode, default, null);           
//        var data = await result.Content.ReadFromJsonAsync<TOutput>(cancellationToken: cancellationToken);
//        return new Results<TOutput>(result.StatusCode, data, null);
//    }

//    private async ValueTask<string> SetEndpointAuthentication(string endpoint)
//    {
//        if (!endpoint.StartsWith("http")) endpoint = setting.ApiEndpoint + endpoint;
//        httpClient.DefaultRequestHeaders.Remove("Authorization");
//        var auth = await browserExtensions.GetFromLocalStorage("session");
//        if (auth is null) return endpoint;
//        var tokens = auth.FromJson<LoginResponseDto>();
//        if (httpClient.DefaultRequestHeaders.Authorization is null)
//            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {tokens.AccessToken}");
//        return endpoint;
//    }

//    private async ValueTask<bool> ResetRefreshToken(LoginResponseDto tokens)
//    {
//        if (isCheckingForRefreshToken == false) return false;
//        //isCheckingForRefreshToken = false;
//        var body = new RefreshTokenDto(tokens.RefreshToken);
//        var newRequest = new HttpRequestMessage() {
//            RequestUri = new Uri(setting.ApiEndpoint + EndpointConstants.REFRESH_TOKEN),
//            Method = HttpMethod.Post,
//            Content = new StringContent(body.ToJson(), Encoding.UTF8, "application/json")
//        };
//        newRequest.Headers.Add("Authorization", tokens.AccessToken);
//        var result = await httpClient.SendAsync(newRequest);
//        if (result.IsSuccessStatusCode)
//        {            
//            var data = await result.Content.ReadFromJsonAsync<LoginResponseDto>()!;
//            var newTokens = new LoginResponseDto(data!.TokenType,data.AccessToken,data.ExpiresIn,data.RefreshToken)
//            {
//                Claims = tokens.Claims
//            };
//            await browserExtensions.SetToLocalStorage("session", newTokens.ToJson());
//            refreshTokenFailed = false;
//            return true;
//        }
//        else
//        {
//            refreshTokenFailed = true;
//            if (tokens is not null) browserExtensions.Goto("Logout");
//            return false;
//        }
//    }
//}