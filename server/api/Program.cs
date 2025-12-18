using api.Configuration; // Admin bootstrap options (Email + Password from config)
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

        ConfigureServices(builder.Services, builder.Configuration);

        var app = builder.Build();

        ConfigureApp(app);

        app.Run();
    }
    
    public static void ConfigureServices(IServiceCollection services)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();
        
        services.AddSingleton<IConfiguration>(configuration);

        ConfigureServices(services, configuration);
    }
    
    public static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(TimeProvider.System);
        services.InjectAppOptions();        
        services.AddMyDbContext();

        services
            .AddOptions<AdminBootstrapOptions>()
            .Bind(configuration.GetSection("AdminBootstrap"));

        services.AddControllers().AddJsonOptions(opts =>
        {
            opts.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
            
            opts.JsonSerializerOptions.MaxDepth = 32;
        });

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

        services.AddScoped<IAuthService, AuthService>();
        
        services.AddScoped<ISeeder, TestSeeder>();
        services.AddScoped<DevSeeder>(); 
        services.AddScoped<GameSeeder>();
        
        services.AddScoped<IPlayerService, PlayerService>();
        services.AddScoped<IGameService, GameService>();
        services.AddScoped<IBoardService, BoardService>();
        services.AddScoped<ITransactionService, TransactionService>();
        services.AddScoped<AdminBootstrapper>();

        services.AddScoped<IPasswordHasher<User>, NSecArgon2idPasswordHasher>();
        services.AddScoped<ITokenService, JwtService>();
        
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

    
        services.AddAuthorization(options =>
        {
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
        });
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();
    }

    public static void ConfigureApp(WebApplication app)
    {
        app.UseExceptionHandler();
        app.UseOpenApi();
        app.UseSwaggerUi();
        app.MapScalarApiReference(options =>
            options.OpenApiRoutePattern = "/swagger/v1/swagger.json"
        );
        app.UseCors(config =>
            config
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowAnyOrigin()
                .SetIsOriginAllowed(_ => true));

        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        
        if (app.Environment.IsProduction())
        {
            using var scope = app.Services.CreateScope();

            var gameSeeder = scope.ServiceProvider.GetRequiredService<GameSeeder>();
            gameSeeder.SeedGamesIfMissingAsync().GetAwaiter().GetResult();

            var bootstrapper = scope.ServiceProvider.GetRequiredService<AdminBootstrapper>();
            bootstrapper.RunAsync().GetAwaiter().GetResult();
        }


    
    
    
        Console.WriteLine($"ENV = {app.Environment.EnvironmentName}");

        if (app.Environment.IsDevelopment())
        {
            app.GenerateApiClientsFromOpenApi("/../../client/src/core/api/generated/generated-client.ts")
                .GetAwaiter()
                .GetResult();
            using var scope = app.Services.CreateScope();
            var devSeeder = scope.ServiceProvider.GetRequiredService<DevSeeder>();
            devSeeder.Seed().GetAwaiter().GetResult();
        }
    }
}
