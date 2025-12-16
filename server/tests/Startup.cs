// tests/Startup.cs - FIXED VERSION
using api;
using dataccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;
using Testcontainers.PostgreSql;

namespace tests;

public class Startup
{
    private static PostgreSqlContainer? _sharedContainer;
    private static readonly object _lock = new();
    private static bool _schemaInitialized = false; //  Track schema initialization

    private static PostgreSqlContainer GetSharedContainer()
    {
        lock (_lock)
        {
            if (_sharedContainer != null)
                return _sharedContainer;

            _sharedContainer = new PostgreSqlBuilder()
                .WithImage("postgres:15-alpine")
                .Build();

            _sharedContainer.StartAsync().GetAwaiter().GetResult();

            return _sharedContainer;
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
            var container = GetSharedContainer();
            var connectionString = container.GetConnectionString();

            var options = new DbContextOptionsBuilder<MyDbContext>()
                .UseNpgsql(connectionString)
                .EnableSensitiveDataLogging()
                .Options;

            var ctx = new MyDbContext(options);
            lock (_lock)
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
        var fakeTime = new FakeTimeProvider();
        fakeTime.SetUtcNow(DateTimeOffset.UtcNow);
        services.AddSingleton<TimeProvider>(fakeTime);
    }
}

public class TestTransactionScope : IDisposable
{
    private readonly MyDbContext _context;
    private IDbContextTransaction? _transaction;

    public TestTransactionScope(MyDbContext context)
    {
        _context = context;
    }

    public async Task BeginTransactionAsync(CancellationToken ct = default)
    {
        //  Ensure no existing transaction
        if (_transaction != null)
        {
            await _transaction.RollbackAsync(ct);
            await _transaction.DisposeAsync();
        }

        _transaction = await _context.Database.BeginTransactionAsync(ct);
    }

    public void Dispose()
    {
        if (_transaction is null)
            return;

        try
        {
            //  Force rollback synchronously
            _transaction.Rollback();
        }
        catch
        {
            // Ignore rollback errors (transaction may already be aborted)
        }
        finally
        {
            _transaction.Dispose();
            _transaction = null;
        }
    }
}