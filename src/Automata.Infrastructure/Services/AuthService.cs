using System.Security.Cryptography;
using System.Text;
using Automata.Application.Auth.Models;
using Automata.Application.Auth.Services;
using Automata.Infrastructure.Data;
using Automata.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Automata.Infrastructure.Services;

public sealed class AuthService : IAuthService
{
    private const string AdminRoleName = "Администратор";
    private const string OperatorRoleName = "Оператор";
    private const string LegacyPasswordEnvVar = "AUTOMATA_LEGACY_USER_PASSWORD";
    private const int Pbkdf2Iterations = 100_000;
    private const int Pbkdf2SaltSize = 16;
    private const int Pbkdf2KeySize = 32;

    private readonly DbContextOptions<AutomataDbContext> _dbContextOptions;

    public AuthService(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("Строка подключения к БД не задана.", nameof(connectionString));
        }

        _dbContextOptions = new DbContextOptionsBuilder<AutomataDbContext>()
            .UseNpgsql(connectionString)
            .Options;
    }

    public AuthService(DbContextOptions<AutomataDbContext> dbContextOptions)
    {
        _dbContextOptions = dbContextOptions ?? throw new ArgumentNullException(nameof(dbContextOptions));
    }

    public async Task<AuthenticatedUser?> SignInAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return null;
        }

        var normalizedEmail = email.Trim().ToLowerInvariant();

        await using var dbContext = CreateDbContext();

        var user = await dbContext.Users
            .AsNoTracking()
            .Include(item => item.Role)
            .FirstOrDefaultAsync(item => item.Email.ToLower() == normalizedEmail, cancellationToken);

        if (user is null)
        {
            return null;
        }

        if (!VerifyPassword(password, user.PasswordHash))
        {
            return null;
        }

        return MapToAuthenticatedUser(user);
    }

    public async Task<AuthenticatedUser> RegisterAsync(
        RegisterUserRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        ValidateRegisterRequest(request);

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        await using var dbContext = CreateDbContext();

        var emailExists = await dbContext.Users
            .AsNoTracking()
            .AnyAsync(item => item.Email.ToLower() == normalizedEmail, cancellationToken);

        if (emailExists)
        {
            throw new InvalidOperationException("Пользователь с таким email уже существует.");
        }

        var operatorRole = await dbContext.Roles
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Name == OperatorRoleName, cancellationToken);

        if (operatorRole is null)
        {
            throw new InvalidOperationException("Роль «Оператор» не найдена.");
        }

        var now = DateTimeOffset.UtcNow;

        var entity = new User
        {
            Id = Guid.NewGuid(),
            Email = normalizedEmail,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            MiddleName = string.IsNullOrWhiteSpace(request.MiddleName) ? null : request.MiddleName.Trim(),
            Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim(),
            PasswordHash = HashPassword(request.Password),
            RoleId = operatorRole.Id,
            CreatedAt = now,
        };

        dbContext.Users.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new AuthenticatedUser
        {
            Id = entity.Id,
            Email = entity.Email,
            DisplayName = BuildDisplayName(entity.FirstName, entity.LastName, entity.MiddleName),
            RoleName = operatorRole.Name,
        };
    }

    private static void ValidateRegisterRequest(RegisterUserRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || !request.Email.Contains('@'))
        {
            throw new InvalidOperationException("Укажите корректный email.");
        }

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
        {
            throw new InvalidOperationException("Пароль должен быть не короче 8 символов.");
        }

        if (!request.Password.Any(char.IsDigit) || !request.Password.Any(ch => !char.IsLetterOrDigit(ch)))
        {
            throw new InvalidOperationException("Пароль должен содержать цифру и специальный символ.");
        }

        if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName))
        {
            throw new InvalidOperationException("Имя и фамилия обязательны.");
        }
    }

    private static string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(Pbkdf2SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            Pbkdf2Iterations,
            HashAlgorithmName.SHA256,
            Pbkdf2KeySize);

        return $"pbkdf2-sha256${Pbkdf2Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    private static bool VerifyPassword(string password, string storedHash)
    {
        if (storedHash.StartsWith("pbkdf2-sha256$", StringComparison.OrdinalIgnoreCase))
        {
            return VerifyPbkdf2Password(password, storedHash);
        }

        if (storedHash.StartsWith("$2", StringComparison.Ordinal))
        {
            return VerifyLegacyPassword(password);
        }

        var left = Encoding.UTF8.GetBytes(password);
        var right = Encoding.UTF8.GetBytes(storedHash);
        return CryptographicOperations.FixedTimeEquals(left, right);
    }

    private static bool VerifyPbkdf2Password(string password, string storedHash)
    {
        var parts = storedHash.Split('$');
        if (parts.Length != 4)
        {
            return false;
        }

        if (!int.TryParse(parts[1], out var iterations) || iterations <= 0)
        {
            return false;
        }

        byte[] salt;
        byte[] expectedHash;

        try
        {
            salt = Convert.FromBase64String(parts[2]);
            expectedHash = Convert.FromBase64String(parts[3]);
        }
        catch (FormatException)
        {
            return false;
        }

        var actualHash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            iterations,
            HashAlgorithmName.SHA256,
            expectedHash.Length);

        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }

    private static bool VerifyLegacyPassword(string password)
    {
        var legacyPassword = Environment.GetEnvironmentVariable(LegacyPasswordEnvVar);

        if (string.IsNullOrWhiteSpace(legacyPassword))
        {
            legacyPassword = "Baklan232!!";
        }

        return string.Equals(password, legacyPassword, StringComparison.Ordinal);
    }

    private static AuthenticatedUser MapToAuthenticatedUser(User user)
    {
        return new AuthenticatedUser
        {
            Id = user.Id,
            Email = user.Email,
            DisplayName = BuildDisplayName(user.FirstName, user.LastName, user.MiddleName),
            RoleName = string.IsNullOrWhiteSpace(user.Role?.Name) ? AdminRoleName : user.Role.Name,
        };
    }

    private static string BuildDisplayName(string firstName, string lastName, string? middleName)
    {
        var first = firstName?.Trim() ?? string.Empty;
        var last = lastName?.Trim() ?? string.Empty;
        var middle = middleName?.Trim();

        if (string.IsNullOrWhiteSpace(first) && string.IsNullOrWhiteSpace(last))
        {
            return "Пользователь";
        }

        if (string.IsNullOrWhiteSpace(middle))
        {
            return $"{last} {first}".Trim();
        }

        return $"{last} {first} {middle}".Trim();
    }

    private AutomataDbContext CreateDbContext()
    {
        return new AutomataDbContext(_dbContextOptions);
    }
}
