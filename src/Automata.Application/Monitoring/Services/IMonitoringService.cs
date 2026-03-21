using Automata.Application.Common;
using Automata.Application.Monitoring.Models;

namespace Automata.Application.Monitoring.Services;

/// <summary>
/// Контракт сервиса мониторинга, общий для desktop и web.
/// </summary>
public interface IMonitoringService
{
    /// <summary>
    /// Возвращает справочник статусов для фильтра.
    /// </summary>
    Task<IReadOnlyList<LookupItem>> GetStatusesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Возвращает список автоматов и сводку мониторинга с учетом фильтра.
    /// </summary>
    Task<MonitoringDashboardModel> GetDashboardAsync(
        MonitoringFilterModel filter,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Экспортирует мониторинг в CSV.
    /// </summary>
    Task<string> ExportCsvAsync(
        MonitoringFilterModel filter,
        CancellationToken cancellationToken = default);
}
