using System.Security.Cryptography;
using System.Text;
using Bogus;
using dataccess;

namespace api.Etc;

public class SieveTestSeeder(MyDbContext ctx, TimeProvider timeProvider) : ISeeder
{
    public async Task Seed() { }
}