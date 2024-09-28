namespace SUIA.API.Endpoints;

public static class RegisterServices
{
    public static void AddServices(this IServiceCollection services)
    {

    }

    public static void AddEndpoints(this IServiceCollection services)
    {
        var endpoints = typeof(RegisterServices).Assembly.GetTypes()
            .Where(p => typeof(IEndpoints).IsAssignableFrom(p) && p.Name != nameof(IEndpoints));
        foreach(var endpoint in endpoints)
            services.AddScoped(typeof(IEndpoints), endpoint);
    }

    public static void UseEndpoints(this WebApplication app)
    {
        var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider.GetServices<IEndpoints>();
        foreach (var service in services)
            service.Register(app);
    }
}
