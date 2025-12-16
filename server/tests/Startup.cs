// tests/Startup.cs
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
    private static readonly object LockObj = new();
    private static PostgreSqlContainer? _container;
    private static bool _schemaInitialized;
    private static bool _cleanupHooked;

    private static PostgreSqlContainer GetOrStartContainer()
    {
        lock (LockObj)
        {
            if (_container != null)
                return _container;

            _container = new PostgreSqlBuilder()
                .WithImage("postgres:15-alpine")
                .Build();

            _container.StartAsync().GetAwaiter().GetResult();

            if (!_cleanupHooked)
            {
                _cleanupHooked = true;
                AppDomain.CurrentDomain.ProcessExit += (_, _) =>
                {
                    try
                    {
                        _container.StopAsync().GetAwaiter().GetResult();
                        _container.DisposeAsync().AsTask().GetAwaiter().GetResult();
                    }
                    catch
                    {
                        // ignore cleanup errors
                    }
                };
            }

            return _container;
        }
    }

    public void ConfigureServices(IServiceCollection services)
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Test");

        Environment.SetEnvironmentVariable(
            "AppOptions__JwtSecret",
            "BP9bcgZwv3YOoOW5iJga1zlu48J37oB2GOcvfRMtcywKri2Z2SW65S8f+m/kHlb9jzMcC8dcX8R+8244wyIuww=="
        );

        Program.ConfigureServices(services);

        services.RemoveAll(typeof(MyDbContext));
        services.AddScoped<MyDbContext>(_ =>
        {
            var container = GetOrStartContainer();
            var cs = container.GetConnectionString();

            var options = new DbContextOptionsBuilder<MyDbContext>()
                .UseNpgsql(cs)
                .Options;

            var ctx = new MyDbContext(options);

            lock (LockObj)
            {
                if (!_schemaInitialized)
                {
                    ctx.Database.EnsureCreated();
                    _schemaInitialized = true;
                }
            }

            return ctx;
        });

        services.AddScoped<TestTransactionScope>();

        services.RemoveAll<TimeProvider>();
        services.RemoveAll<FakeTimeProvider>();

        services.AddScoped<FakeTimeProvider>(_ =>
        {
            var fake = new FakeTimeProvider();
            fake.SetUtcNow(DateTimeOffset.UtcNow); // <-- IMPORTANT
            return fake;
        });

        services.AddScoped<TimeProvider>(sp => sp.GetRequiredService<FakeTimeProvider>());
    }
}
