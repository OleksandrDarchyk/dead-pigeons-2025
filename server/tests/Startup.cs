using api;
using dataccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;
using Testcontainers.PostgreSql;

namespace tests;

public class Startup
{
    public static void ConfigureServices(IServiceCollection services)
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");

        Environment.SetEnvironmentVariable(
            "AppOptions__JwtSecret",
            "BP9bcgZwv3YOoOW5iJga1zlu48J37oB2GOcvfRMtcywKri2Z2SW65S8f+m/kHlb9jzMcC8dcX8R+8244wyIuww=="
        );

        Program.ConfigureServices(services);

        services.RemoveAll(typeof(MyDbContext));
        services.AddScoped<MyDbContext>(_ =>
        {
            var postgreSqlContainer = new PostgreSqlBuilder().Build();
            postgreSqlContainer.StartAsync().GetAwaiter().GetResult();
            var connectionString = postgreSqlContainer.GetConnectionString();

            var options = new DbContextOptionsBuilder<MyDbContext>()
                .UseNpgsql(connectionString)
                .Options;

            var ctx = new MyDbContext(options);
            ctx.Database.EnsureCreated();
            return ctx;
        });

        services.RemoveAll<TimeProvider>();

        var fakeTime = new FakeTimeProvider();
        fakeTime.SetUtcNow(DateTimeOffset.UtcNow);

        services.AddSingleton<TimeProvider>(fakeTime);
    }
}