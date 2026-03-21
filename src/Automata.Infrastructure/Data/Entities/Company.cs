namespace Automata.Infrastructure.Data.Entities;

/// <summary>
/// Компания в модуле администрирования.
/// </summary>
public class Company
{
    public Guid Id { get; set; }
    public Guid? ParentCompanyId { get; set; }
    public string Name { get; set; } = null!;
    public string? ContactPerson { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public Company? ParentCompany { get; set; }
    public ICollection<Company> ChildCompanies { get; set; } = new List<Company>();
}
