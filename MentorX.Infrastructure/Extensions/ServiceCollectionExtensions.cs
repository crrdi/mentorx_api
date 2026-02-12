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

        // Storage service for mentor avatars
        services.AddScoped<IStorageService, SupabaseStorageService>();

        // RevenueCat webhook service
        services.AddScoped<IRevenueCatWebhookService, RevenueCatWebhookService>();

        // RevenueCat REST API service
        services.AddHttpClient<IRevenueCatApiService, RevenueCatApiService>(client =>
        {
            client.BaseAddress = new Uri("https://api.revenuecat.com/v1");
            client.DefaultRequestHeaders.Add("X-Platform", "api");
            
            // Set API key from configuration
            var apiKey = configuration["RevenueCat:ApiKey"] ?? Environment.GetEnvironmentVariable("REVENUECAT_API_KEY");
            if (string.IsNullOrEmpty(apiKey))
            {
                Console.WriteLine("⚠️  WARNING: RevenueCat:ApiKey is NOT configured! Purchase verification will FAIL. Set it via user-secrets, appsettings, or REVENUECAT_API_KEY env var.");
            }
            else
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
                Console.WriteLine("✅ RevenueCat API key configured (length: {0})", apiKey.Length);
            }
        });

        return services;
    }
}
