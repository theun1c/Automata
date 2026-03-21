namespace Automata.Application.Companies.Models;

/// <summary>
/// Строка списка компаний.
/// </summary>
public sealed class CompanyListItem
{
    public Guid Id { get; init; }
    public Guid? ParentCompanyId { get; init; }
    public string? ParentCompanyName { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? ContactPerson { get; init; }
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string? Address { get; init; }
    public string? Notes { get; init; }
    public DateTimeOffset CreatedAt { get; init; }

    public string ContactsDisplay
    {
        get
        {
            var pieces = new[] { ContactPerson, Phone, Email }
                .Where(part => !string.IsNullOrWhiteSpace(part))
                .Select(part => part!.Trim())
                .ToArray();

            return pieces.Length == 0 ? "-" : string.Join(", ", pieces);
        }
    }
}
