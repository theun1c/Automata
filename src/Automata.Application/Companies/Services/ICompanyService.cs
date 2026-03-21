using Automata.Application.Companies.Models;

namespace Automata.Application.Companies.Services;

/// <summary>
/// Контракт базового CRUD сервиса для компаний.
/// </summary>
public interface ICompanyService
{
    Task<IReadOnlyList<CompanyListItem>> GetListAsync(string? search, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CompanyLookupItem>> GetParentLookupAsync(CancellationToken cancellationToken = default);

    Task<CompanyEditModel?> GetByIdAsync(Guid companyId, CancellationToken cancellationToken = default);

    Task<Guid> CreateAsync(CompanyEditModel model, CancellationToken cancellationToken = default);

    Task UpdateAsync(CompanyEditModel model, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid companyId, CancellationToken cancellationToken = default);
}
