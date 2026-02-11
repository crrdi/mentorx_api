using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MentorX.Application.Interfaces;
using MentorX.Infrastructure.Data.DbContext;
using MentorX.Infrastructure.Services;

namespace MentorX.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Add DbContext
        var connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        
        services.AddDbContext<MentorXDbContext>(options =>
            options.UseNpgsql(connectionString));

        // Add Supabase service
        services.AddSingleton<SupabaseService>();

        // RevenueCat webhook service
        services.AddScoped<IRevenueCatWebhookService, RevenueCatWebhookService>();

        return services;
    }
}
