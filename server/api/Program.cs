using System.Text.Json.Serialization;
using api.Etc;
using Api.Security;
using api.Services;
using dataccess.Entities;
using Microsoft.AspNetCore.Identity;
using Scalar.AspNetCore;
using Sieve.Models;
using Sieve.Services;

namespace api;

public class Program
{
    public static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.InjectAppOptions();//here we use appOptions
        services.AddMyDbContext();
        services.AddControllers().AddJsonOptions(opts =>
        {
            opts.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.Preserve;
            opts.JsonSerializerOptions.MaxDepth = 128;
        });
        services.AddOpenApiDocument(options =>
        {
            options.Title = "Dead Pigeons API";

            // add buttom Authorize
            options.AddSecurity("JWT", Array.Empty<string>(), new NSwag.OpenApiSecurityScheme
            {
                Type = NSwag.OpenApiSecuritySchemeType.ApiKey,
                Name = "Authorization",
                In = NSwag.OpenApiSecurityApiKeyLocation.Header,
                Description = "Write: Bearer {your token}"
            });

            // add token to all operations
            options.OperationProcessors.Add(new NSwag.Generation.Processors.Security.AspNetCoreOperationSecurityScopeProcessor("JWT"));
        });   
        services.AddCors();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ISeeder, SieveTestSeeder>();
        services.AddScoped<IPlayerService, PlayerService>();
        services.AddScoped<IGameService, GameService>();
        services.AddScoped<IBoardService, BoardService>();
        services.AddScoped<ITransactionService, TransactionService>();
        services.AddScoped<IPasswordHasher<User>, NSecArgon2idPasswordHasher>();

        
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();
        //we can delete later  services.Configure<SieveOptions>(options =>
        // {
        //     options.CaseSensitive = false;
        //     options.DefaultPageSize = 10;
        //     options.MaxPageSize = 100;
        // });
        // services.AddScoped<ISieveProcessor, ApplicationSieveProcessor>();
    }

    public static void Main()
    {
        var builder = WebApplication.CreateBuilder();

        ConfigureServices(builder.Services);
        var app = builder.Build();
        app.UseExceptionHandler(); 
        app.UseOpenApi();
        app.UseSwaggerUi();
        app.MapScalarApiReference(options => options.OpenApiRoutePattern = "/swagger/v1/swagger.json"
        );
        app.UseCors(config => config.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin().SetIsOriginAllowed(x => true));
        app.MapControllers();

        if (app.Environment.IsDevelopment())
        {
            app.GenerateApiClientsFromOpenApi("/../../client/src/core/generated-client.ts")
                .GetAwaiter()
                .GetResult();
        
            using var scope = app.Services.CreateScope();
            var seeder = scope.ServiceProvider.GetRequiredService<ISeeder>();
            seeder.Seed().GetAwaiter().GetResult();
        }

        app.Run();


    }
}