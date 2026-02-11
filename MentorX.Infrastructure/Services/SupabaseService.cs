using Microsoft.Extensions.Configuration;
using Supabase;

namespace MentorX.Infrastructure.Services;

public class SupabaseService
{
    private readonly Supabase.Client _client;

    public SupabaseService(IConfiguration configuration)
    {
        var supabaseUrl = configuration["Supabase:Url"] ?? throw new InvalidOperationException("Supabase:Url is not configured");
        
        // Prefer ServiceRoleKey for admin operations, fall back to AnonKey if ServiceRoleKey is not available
        var supabaseKey = configuration["Supabase:ServiceRoleKey"] 
            ?? configuration["Supabase:AnonKey"] 
            ?? throw new InvalidOperationException("Supabase:ServiceRoleKey or Supabase:AnonKey must be configured");

        var options = new SupabaseOptions
        {
            AutoRefreshToken = false,
            AutoConnectRealtime = false
        };

        _client = new Supabase.Client(supabaseUrl, supabaseKey, options);
    }

    public Supabase.Client Client => _client;
}
