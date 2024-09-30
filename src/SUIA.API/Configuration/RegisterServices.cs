using SUIA.API.Contracts;

namespace SUIA.API.Configuration;

public static class RegisterServices
{
    public static IServiceCollection AddServices(this IServiceCollection services)
    {
        var iServiceContracts = typeof(RegisterServices).Assembly.GetTypes()
            .Where(p => typeof(IServices).IsAssignableFrom(p) && p.Name != nameof(IServices) && p.IsInterface);

        foreach(var service in iServiceContracts)
        {
            var serviceContracts = service.Assembly.GetTypes()
                .Where(p => service.IsAssignableFrom(p) && p.Name != nameof(service.Name) && !p.IsInterface);

            foreach (var serviceType in serviceContracts)
            {
                var type = serviceType.GetInterface($"I{serviceType.Name}")!;
                services.AddScoped(type, serviceType);
            }
        }

        return services;
    }
}
