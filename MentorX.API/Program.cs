using MentorX.API.Extensions;
using MentorX.API.Middleware;
using MentorX.API.Authentication;
using MentorX.Infrastructure.Services;
using Serilog;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/mentorx-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Suppress AutoMapper license warning in development
if (builder.Environment.IsDevelopment())
{
    builder.Logging.AddFilter("LuckyPennySoftware.AutoMapper.License", LogLevel.None);
}

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Swagger/OpenAPI configuration
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "MentorX API",
        Version = "v1",
        Description = "MentorX API Documentation",
        Contact = new OpenApiContact
        {
            Name = "MentorX Team"
        }
    });

    // Add JWT Bearer authentication to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Authentication & Authorization
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = "Supabase";
    options.DefaultChallengeScheme = "Supabase";
})
.AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, SupabaseAuthenticationHandler>("Supabase", options => { });

builder.Services.AddAuthorization();

// Application services
builder.Services.AddApplicationServices();

// Infrastructure services
builder.Services.AddInfrastructureServices(builder.Configuration);

// Supabase service (singleton)
builder.Services.AddSingleton<SupabaseService>();

var app = builder.Build();

// Configure the HTTP request pipeline
// Swagger only in development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "MentorX API v1");
        c.RoutePrefix = string.Empty; // Set Swagger UI at root
    });
}

app.UseSerilogRequestLogging();

// HTTPS redirection disabled for Cloud Run (load balancer handles HTTPS)
if (!app.Environment.IsProduction())
{
    app.UseHttpsRedirection();
}

app.UseCors("AllowAll");

// Middleware
app.UseMiddleware<ErrorHandlingMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
