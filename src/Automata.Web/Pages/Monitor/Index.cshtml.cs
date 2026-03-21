using System.Text;
using Automata.Application.Common;
using Automata.Application.Monitoring.Models;
using Automata.Application.Monitoring.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Automata.Web.Pages.Monitor;

/// <summary>
/// Вариант сортировки для web-формы фильтров.
/// </summary>
public sealed record MonitorSortOption(string Value, string Label);

/// <summary>
/// Строка таблицы мониторинга в web-интерфейсе.
/// </summary>
public sealed class MonitorMachineRowViewModel
{
    public int Number { get; init; }
    public string MachineName { get; init; } = string.Empty;
    public string Location { get; init; } = string.Empty;
    public string ModelDisplayName { get; init; } = string.Empty;
    public string ConnectionText { get; init; } = string.Empty;
    public string StatusText { get; init; } = string.Empty;
    public string StatusKind { get; init; } = "working";
    public string LoadOverallText { get; init; } = string.Empty;
    public string LoadMinText { get; init; } = string.Empty;
    public string MoneyLine1 { get; init; } = string.Empty;
    public string MoneyLine2 { get; init; } = string.Empty;
    public string MoneyLine3 { get; init; } = string.Empty;
    public string EventsLine1 { get; init; } = string.Empty;
    public string EventsLine2 { get; init; } = string.Empty;
    public string EquipmentText { get; init; } = string.Empty;
    public string InformationText { get; init; } = string.Empty;
    public string ExtraText { get; init; } = string.Empty;
    public bool IsAttentionRequired { get; init; }
    public bool IsRefillRecommended { get; init; }
}

/// <summary>
/// PageModel единственной web-страницы мониторинга.
/// Использует общий MonitoringService, как и desktop.
/// </summary>
public class IndexModel : PageModel
{
    private readonly IMonitoringService _monitoringService;

    public IndexModel(IMonitoringService monitoringService)
    {
        _monitoringService = monitoringService;
    }

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? StatusId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string SortBy { get; set; } = "name_asc";

    public IReadOnlyList<LookupItem> Statuses { get; private set; } = [];
    public IReadOnlyList<MonitorMachineRowViewModel> Rows { get; private set; } = [];
    public MonitoringSummaryModel Summary { get; private set; } = new();
    public bool HasRows => Rows.Count > 0;
    public string LastUpdatedText { get; private set; } = "-";
    public string? ErrorMessage { get; private set; }

    public IReadOnlyList<MonitorSortOption> SortOptions { get; } =
    [
        new("name_asc", "По названию А-Я"),
        new("name_desc", "По названию Я-А"),
        new("status", "По статусу"),
        new("income_desc", "По доходу (убывание)"),
        new("low_stock_desc", "По низким остаткам"),
        new("last_sale_desc", "По времени последней продажи"),
        new("attention", "Сначала требующие внимания"),
    ];

    public async Task OnGetAsync()
    {
        await LoadAsync();
    }

    public async Task<IActionResult> OnGetExportCsvAsync()
    {
        try
        {
            // Экспорт берёт тот же фильтр, что и текущая таблица.
            var csv = await _monitoringService.ExportCsvAsync(BuildFilter());
            var bytes = new UTF8Encoding(true).GetBytes(csv);
            var fileName = $"monitoring-{DateTime.Now:yyyyMMdd-HHmmss}.csv";
            return File(bytes, "text/csv; charset=utf-8", fileName);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Не удалось выполнить экспорт: {ex.Message}";
            await LoadAsync();
            return Page();
        }
    }

    private async Task LoadAsync()
    {
        try
        {
            // Загружаем справочники и агрегированную модель мониторинга.
            Statuses = await _monitoringService.GetStatusesAsync();

            var dashboard = await _monitoringService.GetDashboardAsync(BuildFilter());
            Rows = dashboard.Machines
                .Select((machine, index) => BuildRow(machine, index + 1))
                .ToList();
            Summary = dashboard.Summary;
            LastUpdatedText = DateTime.Now.ToString("dd.MM.yyyy HH:mm");
            ErrorMessage = null;
        }
        catch (Exception ex)
        {
            Rows = [];
            Summary = new MonitoringSummaryModel();
            LastUpdatedText = "-";
            ErrorMessage = $"Не удалось загрузить мониторинг: {ex.Message}";
        }
    }

    private MonitoringFilterModel BuildFilter()
    {
        // Единый источник фильтрации для таблицы и экспорта.
        return new MonitoringFilterModel
        {
            Search = Search,
            StatusId = StatusId,
            SortBy = SortBy,
        };
    }

    private static MonitorMachineRowViewModel BuildRow(MonitoringMachineItem machine, int number)
    {
        // Адаптация read-model в строки web-таблицы.
        var (overallLoad, minLoad) = BuildLoadTexts(machine.ProductsCount, machine.LowStockProductsCount);
        var statusKind = ResolveStatusKind(machine.StatusName);

        return new MonitorMachineRowViewModel
        {
            Number = number,
            MachineName = machine.Name,
            Location = machine.Location,
            ModelDisplayName = machine.ModelDisplayName,
            ConnectionText = ResolveConnection(machine.StatusName),
            StatusText = machine.StatusName,
            StatusKind = statusKind,
            LoadOverallText = overallLoad,
            LoadMinText = minLoad,
            MoneyLine1 = $"Доход: {machine.TotalIncome:N0} ₽",
            MoneyLine2 = $"Товаров: {machine.ProductsCount}",
            MoneyLine3 = $"Низкий остаток: {machine.LowStockProductsCount}",
            EventsLine1 = $"Продажа: {BuildTimeAgo(machine.LastSaleDateTime)}",
            EventsLine2 = $"Сервис: {machine.LastServiceDisplay}",
            EquipmentText = machine.IsAttentionRequired ? "Купюропр., QR" : "Купюропр., QR, NFC",
            InformationText = machine.IsAttentionRequired ? "EXE / проверка" : "MDB / онлайн",
            ExtraText = $"{machine.ProductsCount} / {machine.LowStockProductsCount}",
            IsAttentionRequired = machine.IsAttentionRequired,
            IsRefillRecommended = machine.IsRefillRecommended,
        };
    }

    private static string ResolveStatusKind(string statusName)
    {
        var normalized = statusName.Trim().ToLowerInvariant();

        if (normalized.Contains("не") && normalized.Contains("рабоч"))
        {
            return "stopped";
        }

        if (normalized.Contains("обслуж"))
        {
            return "service";
        }

        return "working";
    }

    private static string ResolveConnection(string statusName)
    {
        return ResolveStatusKind(statusName) switch
        {
            "stopped" => "GSM / офлайн",
            "service" => "Ethernet / сервис",
            _ => "4G / онлайн",
        };
    }

    private static (string Overall, string Min) BuildLoadTexts(int productsCount, int lowStockCount)
    {
        if (productsCount <= 0)
        {
            return ("Общая 0%", "Мин. 0%");
        }

        var safeProducts = Math.Max(productsCount, 1);
        var overall = Math.Clamp((int)Math.Round((double)(safeProducts - lowStockCount) / safeProducts * 100), 0, 100);
        var min = Math.Clamp((int)Math.Round((double)lowStockCount / safeProducts * 100), 0, 100);
        return ($"Общая {overall}%", $"Мин. {min}%");
    }

    private static string BuildTimeAgo(DateTimeOffset? dateTime)
    {
        if (!dateTime.HasValue)
        {
            return "нет данных";
        }

        var delta = DateTimeOffset.Now - dateTime.Value.ToLocalTime();

        if (delta.TotalMinutes < 1)
        {
            return "только что";
        }

        if (delta.TotalMinutes < 60)
        {
            return $"{(int)delta.TotalMinutes} мин. назад";
        }

        if (delta.TotalHours < 24)
        {
            return $"{(int)delta.TotalHours} ч назад";
        }

        if (delta.TotalDays < 2)
        {
            return "вчера";
        }

        return $"{(int)delta.TotalDays} дн назад";
    }
}
