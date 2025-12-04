using System.Text.Json;
using api.Etc;
using api.Models.Requests;
using Api.Security;
using api.Services;
using dataccess;

public class SetupTests(
    MyDbContext ctx,
    ISeeder seeder,
    ITestOutputHelper outputHelper,
    IAuthService authService)
{
    [Fact]
    public async Task RegisterReturnsJwtWhichCanVerifyAgain()
    {
        // 1) перевіряємо, що DI дав не null
        Assert.NotNull(authService);

        var result = await authService.Register(new RegisterRequestDto
        {
            Email = "test@example.com",
            Password = "SuperStrongPassword123!"
        });

        // 2) перевіряємо, що сервіс повернув результат з токеном
        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result.Token));

        // 3) лише якщо токен є – пробуємо його розкодувати
        var claims = await authService.VerifyAndDecodeToken(result.Token);
        Assert.NotNull(claims);

        outputHelper.WriteLine(result.Token);
        outputHelper.WriteLine(JsonSerializer.Serialize(claims));

        Assert.Equal("test@example.com", claims.Email);
        Assert.Equal(Roles.User, claims.Role);
        Assert.False(string.IsNullOrWhiteSpace(claims.Id));
    }


    [Fact]
    public async Task SeederDoesNotThrowException()
    {
        Assert.NotNull(seeder);

        try
        {
            await seeder.Seed();
        }
        catch (Exception ex)
        {
            // це дасть повний текст винятку зі стеком
            outputHelper.WriteLine(ex.ToString());
            throw;
        }
    }

    

}