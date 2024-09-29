
namespace SUIA.UI.Services;

public class HttpDelegatingHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var next = await base.SendAsync(request, cancellationToken);
        if (!next.IsSuccessStatusCode)
            Console.WriteLine("Ended...");
        return next;
    }
}
