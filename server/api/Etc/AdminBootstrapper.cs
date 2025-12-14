using api.Configuration;
using dataccess;
using dataccess.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace api.Etc;


public class AdminBootstrapper
{
    private readonly MyDbContext _ctx;
    private readonly IOptions<AdminBootstrapOptions> _options;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<AdminBootstrapper> _logger;

    public AdminBootstrapper(
        MyDbContext ctx,
        IOptions<AdminBootstrapOptions> options,
        IPasswordHasher<User> passwordHasher,
        TimeProvider timeProvider,
        ILogger<AdminBootstrapper> logger)
    {
        _ctx = ctx;
        _options = options;
        _passwordHasher = passwordHasher;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task RunAsync()
    {
        _logger.LogInformation("AdminBootstrapper: started");

        var existingAdmin = await _ctx.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Role == "Admin" && u.Deletedat == null);

        if (existingAdmin != null)
        {
            _logger.LogInformation(
                "AdminBootstrapper: Active admin already exists (Id = {Id}). Skipping.",
                existingAdmin.Id);

            return;
        }

        var email = _options.Value.Email;
        var password = _options.Value.Password;

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            _logger.LogWarning(
                "AdminBootstrapper: No admin bootstrap credentials configured. Skipping admin creation.");

            return;
        }

        var normalizedEmail = email.Trim().ToLowerInvariant();
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        var existingUser = await _ctx.Users
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail && u.Deletedat == null);

        User user;

        if (existingUser != null)
        {
            user = existingUser;
            user.Role = "Admin";
            user.Createdat ??= now;

            _logger.LogInformation(
                "AdminBootstrapper: Upgrading existing user (Id = {Id}) to Admin.",
                user.Id);
        }
        else
        {
            user = new User
            {
                Id = Guid.NewGuid().ToString(),
                Email = normalizedEmail,
                Role = "Admin",
                Createdat = now,
            };

            _ctx.Users.Add(user);

            _logger.LogInformation(
                "AdminBootstrapper: Creating new Admin user with email {Email}.",
                normalizedEmail);
        }

        var hashedPassword = _passwordHasher.HashPassword(user, password);
        user.Passwordhash = hashedPassword;

        

        await _ctx.SaveChangesAsync();

        _logger.LogInformation(
            "AdminBootstrapper: Admin user ensured with email {Email}.",
            normalizedEmail);
    }
}
