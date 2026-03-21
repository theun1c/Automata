namespace Automata.Application.Companies.Models;

/// <summary>
/// Элемент выбора компании в выпадающем списке.
/// </summary>
public sealed class CompanyLookupItem
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
}
