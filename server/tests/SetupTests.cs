using System.Text.Json;
using api.Etc;
using api.Models.Requests;
using api.Services;
using dataccess;
using dataccess.Entities;

namespace tests;

public class SetupTests(
    MyDbContext ctx,
    ISeeder seeder,
    ITestOutputHelper outputHelper,
    IAuthService authService)
{
    [Fact]
    public async Task Seeder_DoesNotThrow()
    {
        await seeder.Seed();
    }

    [Fact]
    public async Task Register_DoesNotThrow_AndTokenCanBeVerified()
    {
        // 1) Arrange: create a player that does NOT have a User yet
        var email = "register-tests@dp.dk";

        ctx.Players.Add(new Player
        {
            Id = Guid.NewGuid().ToString(),
            Fullname = "Register Test Player",
            Email = email,
            Phone = "12345678",
            Isactive = true,
            Activatedat = DateTime.UtcNow,
            Createdat = DateTime.UtcNow,
            Deletedat = null
        });

        await ctx.SaveChangesAsync();

        // 2) Act: call Register with this email
        var dto = new RegisterRequestDto
        {
            Email = email,
            Password = "SuperStrongPassword123!",
            ConfirmPassword = "SuperStrongPassword123!"
        };

        var result = await authService.Register(dto);

        // 3) Assert: token is not empty and can be decoded
        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result.Token));

        var claims = await authService.VerifyAndDecodeToken(result.Token);

        outputHelper.WriteLine(result.Token);
        outputHelper.WriteLine(JsonSerializer.Serialize(claims));

        Assert.Equal(email, claims.Email);
        Assert.Equal("User", claims.Role);   // Roles.User if you prefer constant
        Assert.False(string.IsNullOrWhiteSpace(claims.Id));
    }
}