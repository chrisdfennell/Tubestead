using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tubestead.Domain;
using Tubestead.Infrastructure.Data;
using Tubestead.Infrastructure.Identity;
using Tubestead.Infrastructure.Settings;

namespace Tubestead.Infrastructure;

public static class DependencyInjection
{
    /// <summary>Registers EF Core (provider chosen from config), ASP.NET Core
    /// Identity with Guid keys, and the settings service. Cookie behaviour for
    /// the SPA is configured by the API layer.</summary>
    public static IServiceCollection AddTubesteadInfrastructure(
        this IServiceCollection services, IConfiguration config)
    {
        var dbOptions = DatabaseOptions.FromConfiguration(config);

        services.AddDbContext<TubesteadDbContext>(options =>
        {
            switch (dbOptions.Provider)
            {
                case DatabaseProvider.SqlServer:
                    options.UseSqlServer(dbOptions.ConnectionString,
                        sql => sql.MigrationsAssembly(typeof(TubesteadDbContext).Assembly.FullName));
                    break;

                case DatabaseProvider.Sqlite:
                default:
                    options.UseSqlite(dbOptions.ConnectionString,
                        sql => sql.MigrationsAssembly(typeof(TubesteadDbContext).Assembly.FullName));
                    break;
            }
        });

        services.AddIdentity<ApplicationUser, ApplicationRole>(opt =>
            {
                opt.SignIn.RequireConfirmedAccount = false;
                opt.User.RequireUniqueEmail = true;
                opt.Password.RequiredLength = 8;
                opt.Password.RequireDigit = true;
                opt.Password.RequireLowercase = true;
                opt.Password.RequireUppercase = false;
                opt.Password.RequireNonAlphanumeric = false;
            })
            .AddRoles<ApplicationRole>()
            .AddEntityFrameworkStores<TubesteadDbContext>()
            .AddDefaultTokenProviders();

        services.AddScoped<ISettingsService, SettingsService>();

        return services;
    }

    /// <summary>Applies pending migrations and seeds the Admin/Viewer roles.
    /// Safe to call on every startup.</summary>
    public static async Task MigrateAndSeedAsync(this IServiceProvider services, CancellationToken ct = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TubesteadDbContext>();
        await db.Database.MigrateAsync(ct);

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        foreach (var role in Roles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new ApplicationRole(role));
        }
    }
}
