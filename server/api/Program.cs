using api.Etc;
using Api.Security;
using api.Services;
using dataccess.Entities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Scalar.AspNetCore;
using System.Text.Json.Serialization;

namespace api;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Normal path when the real app is starting
        ConfigureServices(builder.Services, builder.Configuration);

        var app = builder.Build();

        ConfigureApp(app);

        app.Run();
    }

    /// <summary>
    /// Used by tests (Startup in the tests project)
    /// </summary>
    public static void ConfigureServices(IServiceCollection services)
    {
        // Build configuration manually when tests call this overload
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();
        
        services.AddSingleton<IConfiguration>(configuration);

        ConfigureServices(services, configuration);
    }

    /// <summary>
    /// Shared registration, used by both the real app and the tests
    /// </summary>
    public static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // TimeProvider + AppOptions + DbContext
        services.AddSingleton(TimeProvider.System);
        services.InjectAppOptions();          // uses AppOptions from configuration (Db, JwtSecret)
        services.AddMyDbContext();

        // Controllers + JSON configuration
        services.AddControllers().AddJsonOptions(opts =>
        {
            opts.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
            
            opts.JsonSerializerOptions.MaxDepth = 32;
        });


        // OpenAPI / Swagger + Sieve string constants
        services.AddOpenApiDocument(options =>
        {
            options.Title = "Dead Pigeons API";

            options.AddSecurity("JWT", Array.Empty<string>(), new NSwag.OpenApiSecurityScheme
            {
                Type = NSwag.OpenApiSecuritySchemeType.ApiKey,
                Name = "Authorization",
                In = NSwag.OpenApiSecurityApiKeyLocation.Header,
                Description = "Write: Bearer {your token}"
            });

            options.OperationProcessors.Add(
                new NSwag.Generation.Processors.Security.AspNetCoreOperationSecurityScopeProcessor("JWT"));
            
        });

        services.AddCors();

        // Application services
        // Application services
        services.AddScoped<IAuthService, AuthService>();

        // ISeeder here is mainly for tests (they inject ISeeder).
        // In Development we will explicitly use DevSeeder in Program.ConfigureApp.
        services.AddScoped<ISeeder, TestSeeder>();

        services.AddScoped<DevSeeder>(); // Dev seeder for local runs

        services.AddScoped<IPlayerService, PlayerService>();
        services.AddScoped<IGameService, GameService>();
        services.AddScoped<IBoardService, BoardService>();
        services.AddScoped<ITransactionService, TransactionService>();


        // Password hasher (Argon2id)
        services.AddScoped<IPasswordHasher<User>, NSecArgon2idPasswordHasher>();

        // JWT token service (used by JwtBearer validation)
        services.AddScoped<ITokenService, JwtService>();
        
        // Authentication setup
        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultSignInScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = JwtService.ValidationParameters(configuration);

                // Simple debug logging
                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        Console.WriteLine($"Authentication failed: {context.Exception}");
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        Console.WriteLine("Token validated successfully");
                        return Task.CompletedTask;
                    }
                };
            });

        // Global "whitelist" style authorization:
        // everything requires an authenticated user unless [AllowAnonymous] is used
        services.AddAuthorization(options =>
        {
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
        });

        // Global exception handling (maps ValidationException etc. to ProblemDetails)
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();
    }

    public static void ConfigureApp(WebApplication app)
    {
        // Centralized exception handler should be early in the pipeline
        app.UseExceptionHandler();

        // OpenAPI / Swagger / Scalar
        app.UseOpenApi();
        app.UseSwaggerUi();
        app.MapScalarApiReference(options =>
            options.OpenApiRoutePattern = "/swagger/v1/swagger.json"
        );

        // CORS configuration
        app.UseCors(config =>
            config
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowAnyOrigin()
                .SetIsOriginAllowed(_ => true));

        // Authentication + Authorization
        app.UseAuthentication();
        app.UseAuthorization();

        // Map controllers
        app.MapControllers();

        // Dev-only: generate TypeScript client and seed the database
        // Dev-only: generate TypeScript client and seed the database
        // Dev-only: generate TypeScript client and seed the database
        if (app.Environment.IsDevelopment())
        {
            app.GenerateApiClientsFromOpenApi("/../../client/src/core/generated-client.ts")
                .GetAwaiter()
                .GetResult();

            // Use Dev seeder here so we do NOT wipe the database on each run.
            using var scope = app.Services.CreateScope();
            var devSeeder = scope.ServiceProvider.GetRequiredService<DevSeeder>();
            devSeeder.Seed().GetAwaiter().GetResult();
        }


    }
}
