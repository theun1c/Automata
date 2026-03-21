using System.Security.Cryptography;
using Automata.Application.Common;
using Automata.Application.Users.Models;
using Automata.Application.Users.Services;
using Automata.Infrastructure.Data;
using Automata.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Automata.Infrastructure.Services;

/// <summary>
/// CRUD-сервис администрирования пользователей.
/// Отвечает за список, создание, обновление, смену пароля и безопасное удаление.
/// </summary>
public sealed class UserAdministrationService : IUserAdministrationService
{
    private const int Pbkdf2Iterations = 100_000;
    private const int Pbkdf2SaltSize = 16;
    private const int Pbkdf2KeySize = 32;

    private readonly DbContextOptions<AutomataDbContext> _dbContextOptions;

    public UserAdministrationService(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("Строка подключения к БД не задана.", nameof(connectionString));
        }

        _dbContextOptions = new DbContextOptionsBuilder<AutomataDbContext>()
            .UseNpgsql(connectionString)
            .Options;
    }

    public UserAdministrationService(DbContextOptions<AutomataDbContext> dbContextOptions)
    {
        _dbContextOptions = dbContextOptions ?? throw new ArgumentNullException(nameof(dbContextOptions));
    }

    public async Task<IReadOnlyList<UserAdministrationListItem>> GetUsersAsync(
        string? search,
        int? roleId,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = CreateDbContext();

        // Базовая выборка пользователей с ролью.
        var query = dbContext.Users
            .AsNoTracking()
            .Include(user => user.Role)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.Trim().ToLower();

            // Поиск по ФИО, email и телефону.
            query = query.Where(user =>
                user.LastName.ToLower().Contains(normalizedSearch) ||
                user.FirstName.ToLower().Contains(normalizedSearch) ||
                (user.MiddleName != null && user.MiddleName.ToLower().Contains(normalizedSearch)) ||
                user.Email.ToLower().Contains(normalizedSearch) ||
                (user.Phone != null && user.Phone.ToLower().Contains(normalizedSearch)));
        }

        if (roleId.HasValue)
        {
            query = query.Where(user => user.RoleId == roleId.Value);
        }

        var maintenanceUserIds = await dbContext.MaintenanceRecords
            .AsNoTracking()
            .Select(record => record.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var users = await query
            .OrderBy(user => user.LastName)
            .ThenBy(user => user.FirstName)
            .ThenBy(user => user.Email)
            .Select(user => new
            {
                user.Id,
                user.LastName,
                user.FirstName,
                user.MiddleName,
                user.Email,
                user.Phone,
                user.RoleId,
                RoleName = user.Role.Name,
                user.CreatedAt,
            })
            .ToListAsync(cancellationToken);

        var maintenanceSet = maintenanceUserIds.ToHashSet();

        // В ответ добавляем флаг связей с обслуживанием (нужен для безопасного удаления в UI).
        return users.Select(user => new UserAdministrationListItem
        {
            Id = user.Id,
            LastName = user.LastName,
            FirstName = user.FirstName,
            MiddleName = user.MiddleName,
            DisplayName = BuildDisplayName(user.LastName, user.FirstName, user.MiddleName),
            Email = user.Email,
            Phone = user.Phone,
            RoleId = user.RoleId,
            RoleName = user.RoleName,
            CreatedAt = user.CreatedAt,
            HasMaintenanceRecords = maintenanceSet.Contains(user.Id),
        }).ToList();
    }

    public async Task<IReadOnlyList<LookupItem>> GetRolesAsync(CancellationToken cancellationToken = default)
    {
        await using var dbContext = CreateDbContext();

        return await dbContext.Roles
            .AsNoTracking()
            .OrderBy(role => role.Name)
            .Select(role => new LookupItem
            {
                Id = role.Id,
                Name = role.Name,
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<Guid> CreateUserAsync(
        UserEditModel model,
        string password,
        CancellationToken cancellationToken = default)
    {
        if (model is null)
        {
            throw new ArgumentNullException(nameof(model));
        }

        ValidateUserModel(model);
        ValidatePassword(password);

        var normalizedEmail = NormalizeEmail(model.Email);

        await using var dbContext = CreateDbContext();

        await EnsureRoleExistsAsync(dbContext, model.RoleId, cancellationToken);
        await EnsureEmailUniqueAsync(dbContext, normalizedEmail, null, cancellationToken);

        var entity = new User
        {
            Id = Guid.NewGuid(),
            LastName = model.LastName.Trim(),
            FirstName = model.FirstName.Trim(),
            MiddleName = NormalizeOptional(model.MiddleName),
            Email = normalizedEmail,
            Phone = NormalizeOptional(model.Phone),
            // Пароль в БД хранится только в виде хеша.
            PasswordHash = HashPassword(password),
            RoleId = model.RoleId,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        dbContext.Users.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }

    public async Task UpdateUserAsync(
        UserEditModel model,
        Guid actingUserId,
        CancellationToken cancellationToken = default)
    {
        if (model is null)
        {
            throw new ArgumentNullException(nameof(model));
        }

        if (!model.Id.HasValue)
        {
            throw new ArgumentException("Для обновления требуется идентификатор пользователя.", nameof(model));
        }

        ValidateUserModel(model);

        var normalizedEmail = NormalizeEmail(model.Email);

        await using var dbContext = CreateDbContext();

        var entity = await dbContext.Users
            .FirstOrDefaultAsync(user => user.Id == model.Id.Value, cancellationToken);

        if (entity is null)
        {
            throw new InvalidOperationException("Пользователь не найден.");
        }

        if (entity.Id == actingUserId && entity.RoleId != model.RoleId)
        {
            // Блокируем потенциальную потерю собственных прав администратора.
            throw new InvalidOperationException("Нельзя изменить собственную роль.");
        }

        await EnsureRoleExistsAsync(dbContext, model.RoleId, cancellationToken);
        await EnsureEmailUniqueAsync(dbContext, normalizedEmail, entity.Id, cancellationToken);

        entity.LastName = model.LastName.Trim();
        entity.FirstName = model.FirstName.Trim();
        entity.MiddleName = NormalizeOptional(model.MiddleName);
        entity.Email = normalizedEmail;
        entity.Phone = NormalizeOptional(model.Phone);
        entity.RoleId = model.RoleId;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ChangePasswordAsync(
        Guid userId,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        ValidatePassword(newPassword);

        await using var dbContext = CreateDbContext();

        var entity = await dbContext.Users
            .FirstOrDefaultAsync(user => user.Id == userId, cancellationToken);

        if (entity is null)
        {
            throw new InvalidOperationException("Пользователь не найден.");
        }

        entity.PasswordHash = HashPassword(newPassword);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteUserAsync(
        Guid userId,
        Guid actingUserId,
        CancellationToken cancellationToken = default)
    {
        if (userId == actingUserId)
        {
            // Защита от удаления текущей сессии и потери доступа.
            throw new InvalidOperationException("Нельзя удалить текущего пользователя.");
        }

        await using var dbContext = CreateDbContext();

        var hasMaintenanceRecords = await dbContext.MaintenanceRecords
            .AsNoTracking()
            .AnyAsync(record => record.UserId == userId, cancellationToken);

        if (hasMaintenanceRecords)
        {
            throw new InvalidOperationException(
                "Нельзя удалить пользователя: есть связанные записи обслуживания.");
        }

        var entity = await dbContext.Users
            .FirstOrDefaultAsync(user => user.Id == userId, cancellationToken);

        if (entity is null)
        {
            return;
        }

        dbContext.Users.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static void ValidateUserModel(UserEditModel model)
    {
        // Минимальная прикладная валидация перед записью в БД.
        if (string.IsNullOrWhiteSpace(model.LastName))
        {
            throw new InvalidOperationException("Фамилия обязательна.");
        }

        if (string.IsNullOrWhiteSpace(model.FirstName))
        {
            throw new InvalidOperationException("Имя обязательно.");
        }

        if (string.IsNullOrWhiteSpace(model.Email) || !model.Email.Contains('@'))
        {
            throw new InvalidOperationException("Укажите корректный email.");
        }

        if (model.RoleId <= 0)
        {
            throw new InvalidOperationException("Роль пользователя обязательна.");
        }
    }

    private static void ValidatePassword(string password)
    {
        // Простая, но достаточная для текущей версии проверка сложности.
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
        {
            throw new InvalidOperationException("Пароль должен быть не короче 8 символов.");
        }

        if (!password.Any(char.IsDigit) || !password.Any(ch => !char.IsLetterOrDigit(ch)))
        {
            throw new InvalidOperationException("Пароль должен содержать цифру и специальный символ.");
        }
    }

    private static async Task EnsureRoleExistsAsync(
        AutomataDbContext dbContext,
        int roleId,
        CancellationToken cancellationToken)
    {
        var roleExists = await dbContext.Roles
            .AsNoTracking()
            .AnyAsync(role => role.Id == roleId, cancellationToken);

        if (!roleExists)
        {
            throw new InvalidOperationException("Выбранная роль не найдена.");
        }
    }

    private static async Task EnsureEmailUniqueAsync(
        AutomataDbContext dbContext,
        string normalizedEmail,
        Guid? currentUserId,
        CancellationToken cancellationToken)
    {
        var emailExists = await dbContext.Users
            .AsNoTracking()
            .AnyAsync(user =>
                user.Email.ToLower() == normalizedEmail &&
                (!currentUserId.HasValue || user.Id != currentUserId.Value),
                cancellationToken);

        if (emailExists)
        {
            throw new InvalidOperationException("Пользователь с таким email уже существует.");
        }
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToLowerInvariant();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string BuildDisplayName(string lastName, string firstName, string? middleName)
    {
        var parts = new[]
        {
            lastName.Trim(),
            firstName.Trim(),
            middleName?.Trim() ?? string.Empty,
        };

        return string.Join(" ", parts.Where(part => !string.IsNullOrWhiteSpace(part)));
    }

    private static string HashPassword(string password)
    {
        // Формат: pbkdf2-sha256$iterations$salt$hash.
        // Такой формат удобно проверять и мигрировать в будущем без потери совместимости.
        var salt = RandomNumberGenerator.GetBytes(Pbkdf2SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            Pbkdf2Iterations,
            HashAlgorithmName.SHA256,
            Pbkdf2KeySize);

        return $"pbkdf2-sha256${Pbkdf2Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    private AutomataDbContext CreateDbContext()
    {
        return new AutomataDbContext(_dbContextOptions);
    }
}
