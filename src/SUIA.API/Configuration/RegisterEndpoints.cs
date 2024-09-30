using SUIA.API.Contracts;

namespace SUIA.API.Configuration;

public static class RegisterEndpoints
{
    public static IServiceCollection AddEndpoints(this IServiceCollection services)
    {
        var endpoints = typeof(RegisterEndpoints).Assembly.GetTypes()
            .Where(p => typeof(IEndpoints).IsAssignableFrom(p) && p.Name != nameof(IEndpoints));
        foreach (var endpoint in endpoints)
            services.AddScoped(typeof(IEndpoints), endpoint);
        return services;
    }

    public static void UseEndpoints(this WebApplication app)
    {
        var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider.GetServices<IEndpoints>();
        foreach (var service in services)
            service.Register(app);
    }
}
