namespace SUIA.UI.Services;

public interface IAPIService
{
    ValueTask<Results<TOutput>> GetAsync<TOutput>(string endpoints, CancellationToken cancellationToken = default);
    ValueTask<Results<TOutput>> PostAsync<TInput, TOutput>(string endpoints, TInput requestBody, CancellationToken cancellationToken = default);
    //ValueTask<Results<string?>> PostAsync<TInput>(string endpoints, TInput requestBody, CancellationToken cancellationToken = default);
    ValueTask<Results<TOutput>> PutAsync<TInput, TOutput>(string endpoints, TInput requestBody, CancellationToken cancellationToken = default);
    ValueTask<Results<TOutput>> DeleteAsync<TOutput>(string endpoints, CancellationToken cancellationToken = default);
}

public interface IEmpty;