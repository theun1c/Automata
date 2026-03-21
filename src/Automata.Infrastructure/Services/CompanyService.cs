using Automata.Application.Companies.Models;
using Automata.Application.Companies.Services;
using Automata.Infrastructure.Data;
using Automata.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Automata.Infrastructure.Services;

/// <summary>
/// Простой CRUD-сервис компаний для desktop-экрана администрирования.
/// </summary>
public sealed class CompanyService : ICompanyService
{
    private readonly DbContextOptions<AutomataDbContext> _dbContextOptions;

    public CompanyService(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("Строка подключения к БД не задана.", nameof(connectionString));
        }

        _dbContextOptions = new DbContextOptionsBuilder<AutomataDbContext>()
            .UseNpgsql(connectionString)
            .Options;
    }

    public CompanyService(DbContextOptions<AutomataDbContext> dbContextOptions)
    {
        _dbContextOptions = dbContextOptions ?? throw new ArgumentNullException(nameof(dbContextOptions));
    }

    public async Task<IReadOnlyList<CompanyListItem>> GetListAsync(
        string? search,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = CreateDbContext();

        var query = dbContext.Companies
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalized = search.Trim().ToLowerInvariant();

            query = query.Where(company =>
                company.Name.ToLower().Contains(normalized) ||
                (company.ContactPerson != null && company.ContactPerson.ToLower().Contains(normalized)) ||
                (company.Email != null && company.Email.ToLower().Contains(normalized)) ||
                (company.Phone != null && company.Phone.ToLower().Contains(normalized)));
        }

        return await query
            .OrderBy(company => company.Name)
            .Select(company => new CompanyListItem
            {
                Id = company.Id,
                ParentCompanyId = company.ParentCompanyId,
                ParentCompanyName = company.ParentCompany != null ? company.ParentCompany.Name : null,
                Name = company.Name,
                ContactPerson = company.ContactPerson,
                Phone = company.Phone,
                Email = company.Email,
                Address = company.Address,
                Notes = company.Notes,
                CreatedAt = company.CreatedAt,
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CompanyLookupItem>> GetParentLookupAsync(CancellationToken cancellationToken = default)
    {
        await using var dbContext = CreateDbContext();

        return await dbContext.Companies
            .AsNoTracking()
            .OrderBy(company => company.Name)
            .Select(company => new CompanyLookupItem
            {
                Id = company.Id,
                Name = company.Name,
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<CompanyEditModel?> GetByIdAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        await using var dbContext = CreateDbContext();

        return await dbContext.Companies
            .AsNoTracking()
            .Where(company => company.Id == companyId)
            .Select(company => new CompanyEditModel
            {
                Id = company.Id,
                ParentCompanyId = company.ParentCompanyId,
                Name = company.Name,
                ContactPerson = company.ContactPerson,
                Phone = company.Phone,
                Email = company.Email,
                Address = company.Address,
                Notes = company.Notes,
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Guid> CreateAsync(CompanyEditModel model, CancellationToken cancellationToken = default)
    {
        if (model is null)
        {
            throw new ArgumentNullException(nameof(model));
        }

        Validate(model);

        await using var dbContext = CreateDbContext();
        await EnsureParentExistsAsync(dbContext, model.ParentCompanyId, cancellationToken);

        var entity = new Company
        {
            Id = Guid.NewGuid(),
            ParentCompanyId = model.ParentCompanyId,
            Name = model.Name.Trim(),
            ContactPerson = Normalize(model.ContactPerson),
            Phone = Normalize(model.Phone),
            Email = Normalize(model.Email),
            Address = Normalize(model.Address),
            Notes = Normalize(model.Notes),
            CreatedAt = DateTimeOffset.UtcNow,
        };

        dbContext.Companies.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }

    public async Task UpdateAsync(CompanyEditModel model, CancellationToken cancellationToken = default)
    {
        if (model is null)
        {
            throw new ArgumentNullException(nameof(model));
        }

        if (!model.Id.HasValue)
        {
            throw new ArgumentException("Для обновления компании требуется идентификатор.", nameof(model));
        }

        Validate(model);

        await using var dbContext = CreateDbContext();

        var entity = await dbContext.Companies
            .FirstOrDefaultAsync(company => company.Id == model.Id.Value, cancellationToken);

        if (entity is null)
        {
            throw new InvalidOperationException("Компания не найдена.");
        }

        if (model.ParentCompanyId.HasValue && model.ParentCompanyId.Value == entity.Id)
        {
            throw new InvalidOperationException("Компания не может быть вышестоящей сама для себя.");
        }

        await EnsureParentExistsAsync(dbContext, model.ParentCompanyId, cancellationToken);

        entity.ParentCompanyId = model.ParentCompanyId;
        entity.Name = model.Name.Trim();
        entity.ContactPerson = Normalize(model.ContactPerson);
        entity.Phone = Normalize(model.Phone);
        entity.Email = Normalize(model.Email);
        entity.Address = Normalize(model.Address);
        entity.Notes = Normalize(model.Notes);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        await using var dbContext = CreateDbContext();

        var hasChildren = await dbContext.Companies
            .AsNoTracking()
            .AnyAsync(company => company.ParentCompanyId == companyId, cancellationToken);

        if (hasChildren)
        {
            throw new InvalidOperationException("Нельзя удалить компанию, у которой есть дочерние компании.");
        }

        var entity = await dbContext.Companies
            .FirstOrDefaultAsync(company => company.Id == companyId, cancellationToken);

        if (entity is null)
        {
            return;
        }

        dbContext.Companies.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static void Validate(CompanyEditModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Name))
        {
            throw new InvalidOperationException("Название компании обязательно.");
        }

        if (string.IsNullOrWhiteSpace(model.Address))
        {
            throw new InvalidOperationException("Адрес компании обязателен.");
        }

        if (string.IsNullOrWhiteSpace(model.ContactPerson) &&
            string.IsNullOrWhiteSpace(model.Phone) &&
            string.IsNullOrWhiteSpace(model.Email))
        {
            throw new InvalidOperationException("Укажите хотя бы один контакт компании.");
        }

        if (model.Email is not null && !string.IsNullOrWhiteSpace(model.Email) && !model.Email.Contains('@'))
        {
            throw new InvalidOperationException("Укажите корректный email компании.");
        }
    }

    private static async Task EnsureParentExistsAsync(
        AutomataDbContext dbContext,
        Guid? parentCompanyId,
        CancellationToken cancellationToken)
    {
        if (!parentCompanyId.HasValue)
        {
            return;
        }

        var exists = await dbContext.Companies
            .AsNoTracking()
            .AnyAsync(company => company.Id == parentCompanyId.Value, cancellationToken);

        if (!exists)
        {
            throw new InvalidOperationException("Выбранная вышестоящая компания не найдена.");
        }
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private AutomataDbContext CreateDbContext()
    {
        return new AutomataDbContext(_dbContextOptions);
    }
}
