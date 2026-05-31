using KjMatzosLookup.Application.Configuration;
using KjMatzosLookup.Application.Interfaces;
using KjMatzosLookup.Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KjMatzosLookup.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddKjMatzosApplication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<CustomerOrderKioskSettings>(
            configuration.GetSection(CustomerOrderKioskSettings.SectionName));
        services.AddScoped<ICustomerOrderLookupKioskService, CustomerOrderLookupKioskService>();
        return services;
    }
}
