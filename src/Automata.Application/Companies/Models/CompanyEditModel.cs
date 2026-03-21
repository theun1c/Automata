namespace Automata.Application.Companies.Models;

/// <summary>
/// DTO формы создания/редактирования компании.
/// </summary>
public sealed class CompanyEditModel
{
    public Guid? Id { get; init; }
    public Guid? ParentCompanyId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? ContactPerson { get; init; }
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string? Address { get; init; }
    public string? Notes { get; init; }
}
