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
        // Force Development environment for Program.ConfigureServices
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");

        // Reuse the normal DI setup from the API project
        Program.ConfigureServices(services);

        // Replace MyDbContext with a Testcontainers-based Postgres
        services.RemoveAll(typeof(MyDbContext));
        services.AddScoped<MyDbContext>(factory =>
        {
            // One Postgres container per test scope
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

        // Replace TimeProvider with FakeTimeProvider for deterministic time in tests
        services.RemoveAll<TimeProvider>();

        var fakeTime = new FakeTimeProvider();

        // Align fake time with real UTC time so JWT lifetime validation works
        fakeTime.SetUtcNow(DateTimeOffset.UtcNow);

        services.AddSingleton<TimeProvider>(fakeTime);
    }
}