using Api.Security;
using dataccess.Entities;
using Microsoft.AspNetCore.Identity;

namespace tests.Security;

public class PasswordHasherTests(IPasswordHasher<User> sut)
{
    [Fact]
    public void Hash_And_Verify_Password_Success()
    {
        var user = new User();
        var password = "S3cret!1";

        var hash = sut.HashPassword(user, password);
        
        var result = sut.VerifyHashedPassword(user, hash, password);

        Assert.Equal(PasswordVerificationResult.Success, result);
        Assert.IsType<NSecArgon2idPasswordHasher>(sut); 
    }
}