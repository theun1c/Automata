using Automata.Application.Dashboard.Models;

namespace Automata.Application.Dashboard.Services;

/// <summary>
/// Контракт чтения данных главной страницы (dashboard).
/// </summary>
public interface IHomeDashboardService
{
    /// <summary>
    /// Возвращает полную модель dashboard.
    /// </summary>
    Task<HomeDashboardModel> GetDashboardAsync(CancellationToken cancellationToken = default);
}
