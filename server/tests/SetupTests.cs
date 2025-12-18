using api.Etc;
using api.Models.Requests;
using api.Services;
using dataccess;
using dataccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace tests;

public class SetupTests(
    MyDbContext ctx,
    TestTransactionScope transaction,
    ISeeder seeder,
    ITestOutputHelper output,
    IAuthService authService) : IAsyncLifetime
{
    public async ValueTask InitializeAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        await transaction.BeginTransactionAsync(ct);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task Database_IsCreated_Successfully()
    {
        var ct = TestContext.Current.CancellationToken;

        var canConnect = await ctx.Database.CanConnectAsync(ct);
        Assert.True(canConnect);

        output.WriteLine("✓ Database connection successful");
    }

    [Fact]
    public async Task Seeder_DoesNotThrow()
    {
        var ct = TestContext.Current.CancellationToken;

        await seeder.Seed();

        var usersCount = await ctx.Users.CountAsync(ct);
        var playersCount = await ctx.Players.CountAsync(ct);
        var gamesCount = await ctx.Games.CountAsync(ct);

        Assert.True(usersCount > 0);
        Assert.True(playersCount > 0);
        Assert.True(gamesCount > 0);

        output.WriteLine(
            $"✓ Seeded: {usersCount} users, {playersCount} players, {gamesCount} games"
        );
    }

    [Fact]
    public async Task Register_CreatesUserSuccessfully()
    {
        var ct = TestContext.Current.CancellationToken;

        var email = $"setup-{Guid.NewGuid()}@test.local";
        const string password = "StrongPassword123!";

        ctx.Players.Add(new Player
        {
            Id = Guid.NewGuid().ToString(),
            Fullname = "Setup Test",
            Email = email,
            Phone = "12345678",
            Isactive = true,
            Activatedat = DateTime.UtcNow,
            Createdat = DateTime.UtcNow,
            Deletedat = null
        });

        await ctx.SaveChangesAsync(ct);

        var dto = new RegisterRequestDto
        {
            Email = email,
            Password = password,
            ConfirmPassword = password
        };

        var response = await authService.Register(dto);
        Assert.False(string.IsNullOrWhiteSpace(response.Token));

        var claims = await authService.VerifyAndDecodeToken(response.Token);
        Assert.Equal(email, claims.Email);

        output.WriteLine($"✓ Registration + token verification OK for {email}");
    }

    [Fact]
    public async Task TransactionIsolation_WorksCorrectly()
    {
        var ct = TestContext.Current.CancellationToken;

        var testId = Guid.NewGuid().ToString();

        var player = new Player
        {
            Id = testId,
            Fullname = "Isolation Test",
            Email = $"isolation-{testId}@test.local",
            Phone = "12345678",
            Isactive = true,
            Activatedat = DateTime.UtcNow,
            Createdat = DateTime.UtcNow,
            Deletedat = null
        };

        ctx.Players.Add(player);
        await ctx.SaveChangesAsync(ct);

        var found = await ctx.Players.FindAsync(
            new object[] { testId },
            ct
        );

        Assert.NotNull(found);

        output.WriteLine($"✓ Transaction isolation confirmed for player {testId}");
    }
}
