using System.Text.Json;
using System.Text.Json.Serialization;
using api.Etc;
using Api.Security;
using api.Services;
using dataccess.Entities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Scalar.AspNetCore;
using Sieve.Models;
using Sieve.Services;

namespace api;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Normal path when app is running
        ConfigureServices(builder.Services, builder.Configuration);

        var app = builder.Build();

        ConfigureApp(app);

        app.Run();
    }

    /// <summary>
    /// Used by tests (Startup in tests project)
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
    /// Real registration, shared between app and tests
    /// </summary>
    public static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // TimeProvider + AppOptions + DbContext
        services.AddSingleton(TimeProvider.System);
        services.InjectAppOptions();          // uses AppOptions from configuration (Db, JwtSecret)
        services.AddMyDbContext();

        // Controllers + JSON
        services.AddControllers().AddJsonOptions(opts =>
        {
            opts.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.Preserve;
            opts.JsonSerializerOptions.MaxDepth = 128;
        });

        // OpenAPI / Swagger + Sieve string constants (добавлено з твого прикладу)
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

            // 👇 ЦЕ з твого library-туторіалу
            options.AddStringConstants(typeof(SieveConstants));
        });

        services.AddCors();

        // Application services
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ISeeder, SieveTestSeeder>();
        services.AddScoped<IPlayerService, PlayerService>();
        services.AddScoped<IGameService, GameService>();
        services.AddScoped<IBoardService, BoardService>();
        services.AddScoped<ITransactionService, TransactionService>();

        // Password hasher (Argon2id)
        services.AddScoped<IPasswordHasher<User>, NSecArgon2idPasswordHasher>();

        // JWT token service (для JwtBearer валідації)
        services.AddScoped<ITokenService, JwtService>();

        // 🔽 Sieve configuration (додано ТІЛЬКИ з того, що ти скинув)
        services.Configure<SieveOptions>(options =>
        {
            options.CaseSensitive = false;
            options.DefaultPageSize = 10;
            options.MaxPageSize = 100;
        });

        services.AddScoped<ISieveProcessor, ApplicationSieveProcessor>();

        // Authentication & Authorization (як у вчителя)
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

                // Debug-логування
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

        services.AddAuthorization();

        // Global exception handling
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();
    }

    public static void ConfigureApp(WebApplication app)
    {
        // Exception handler must be early in pipeline
        app.UseExceptionHandler();

        // OpenAPI / Swagger / Scalar
        app.UseOpenApi();
        app.UseSwaggerUi();
        app.MapScalarApiReference(options =>
            options.OpenApiRoutePattern = "/swagger/v1/swagger.json"
        );

        // CORS
        app.UseCors(config =>
            config
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowAnyOrigin()
                .SetIsOriginAllowed(_ => true));

        // Authentication + Authorization
        app.UseAuthentication();
        app.UseAuthorization();

        // Controllers
        app.MapControllers();

        // Dev-only: generate TS client + seed DB
        if (app.Environment.IsDevelopment())
        {
            app.GenerateApiClientsFromOpenApi("/../../client/src/core/generated-client.ts")
                .GetAwaiter()
                .GetResult();

            using var scope = app.Services.CreateScope();
            var seeder = scope.ServiceProvider.GetRequiredService<ISeeder>();
            seeder.Seed().GetAwaiter().GetResult();
        }
    }
}
