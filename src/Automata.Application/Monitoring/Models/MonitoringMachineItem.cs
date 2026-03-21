using System.Globalization;

namespace Automata.Application.Monitoring.Models;

/// <summary>
/// Read-model одной записи автомата для модуля мониторинга.
/// </summary>
public sealed class MonitoringMachineItem
{
    // Базовая идентификация автомата.
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Location { get; init; } = string.Empty;
    public string ModelDisplayName { get; init; } = string.Empty;

    // Текущее состояние.
    public int StatusId { get; init; }
    public string StatusName { get; init; } = string.Empty;
    public DateOnly InstalledAt { get; init; }
    public DateOnly? LastServiceAt { get; init; }

    // Операционные показатели.
    public decimal TotalIncome { get; init; }
    public int ProductsCount { get; init; }
    public int LowStockProductsCount { get; init; }
    public DateTimeOffset? LastSaleDateTime { get; init; }

    // Флаги внимания для UI.
    public bool IsAttentionRequired { get; init; }
    public bool IsRefillRecommended => LowStockProductsCount > 0 && !IsAttentionRequired;
    public bool IsStable => !IsAttentionRequired;

    // Подготовленные строки для отображения.
    public string ProductsStockText => $"{ProductsCount} / {LowStockProductsCount}";
    public string LastServiceDisplay => LastServiceAt?.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) ?? "-";
    public string LastSaleDisplay => LastSaleDateTime?.ToLocalTime().ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture) ?? "-";
}
