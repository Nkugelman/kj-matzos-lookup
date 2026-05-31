using BackOffice.Infrastructure.DBContext.Tenant;
using KjMatzosLookup.Application.Interfaces;
using KjMatzosLookup.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KjMatzosLookup.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddKjMatzosInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Single-tenant kiosk: connect straight to the KJ Matzos tenant database.
        services.AddDbContext<TenantDBContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("TenantConnection"),
                sql => sql.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorNumbersToAdd: null)));

        services.AddScoped<ICustomerOrderLookupRepository, CustomerOrderLookupRepository>();
        return services;
    }
}
