using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Automata.Web.Pages.Monitor;

public sealed record MonitorOption(string Value, string Label);
public sealed record MonitorSortOption(string Value, string Label);

public sealed class MonitorMachineRow
{
    public int Number { get; init; }
    public string TradingPoint { get; init; } = string.Empty;
    public string MachineName { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public string StateLabel { get; init; } = string.Empty;
    public string ConnectionType { get; init; } = string.Empty;
    public string ConnectionLabel { get; init; } = string.Empty;
    public int TotalLoadPercent { get; init; }
    public int MinLoadPercent { get; init; }
    public decimal Coins { get; init; }
    public decimal Bills { get; init; }
    public decimal Change { get; init; }
    public decimal CashTotal => Coins + Bills + Change;
    public string Events { get; init; } = string.Empty;
    public string Equipment { get; init; } = string.Empty;
    public string Information { get; init; } = string.Empty;
    public string Extra { get; init; } = string.Empty;
    public DateTime LastPingAt { get; init; }
    public DateTime LastSaleAt { get; init; }
    public DateTime LastCollectionAt { get; init; }
    public DateTime LastServiceAt { get; init; }
    public decimal SalesTodayAmount { get; init; }
    public decimal SalesSinceServiceAmount { get; init; }
    public int SalesSinceServiceCount { get; init; }
    public IReadOnlyList<string> AdditionalStatuses { get; init; } = [];
}

public class IndexModel : PageModel
{
    private static readonly List<MonitorMachineRow> AllRows =
    [
        new()
        {
            Number = 1, TradingPoint = "ТП-001", MachineName = "ТА Парк Победы", State = "working", StateLabel = "Работает", ConnectionType = "4g", ConnectionLabel = "4G",
            TotalLoadPercent = 87, MinLoadPercent = 24, Coins = 260, Bills = 760, Change = 1439, Events = "paper_low", Equipment = "Купюроприемник, QR", Information = "Онлайн",
            Extra = "Пинг 2 мин назад", LastPingAt = DateTime.UtcNow.AddMinutes(-2), LastSaleAt = DateTime.UtcNow.AddMinutes(-4), LastCollectionAt = DateTime.UtcNow.AddHours(-10),
            LastServiceAt = DateTime.UtcNow.AddDays(-2), SalesTodayAmount = 3200, SalesSinceServiceAmount = 5400, SalesSinceServiceCount = 48, AdditionalStatuses = ["paper_low"]
        },
        new()
        {
            Number = 2, TradingPoint = "ТП-007", MachineName = "ТА БЦ Север", State = "service", StateLabel = "На обслуживании", ConnectionType = "ethernet", ConnectionLabel = "Ethernet",
            TotalLoadPercent = 52, MinLoadPercent = 12, Coins = 150, Bills = 340, Change = 560, Events = "door_alarm", Equipment = "Монетоприемник, NFC", Information = "Проверка инженера",
            Extra = "Сервис 35 мин назад", LastPingAt = DateTime.UtcNow.AddMinutes(-8), LastSaleAt = DateTime.UtcNow.AddHours(-2), LastCollectionAt = DateTime.UtcNow.AddDays(-1),
            LastServiceAt = DateTime.UtcNow.AddMinutes(-35), SalesTodayAmount = 1880, SalesSinceServiceAmount = 910, SalesSinceServiceCount = 11, AdditionalStatuses = ["door_alarm"]
        },
        new()
        {
            Number = 3, TradingPoint = "ТП-011", MachineName = "ТА Университет", State = "working", StateLabel = "Работает", ConnectionType = "wifi", ConnectionLabel = "Wi-Fi",
            TotalLoadPercent = 75, MinLoadPercent = 41, Coins = 320, Bills = 980, Change = 1210, Events = "ok", Equipment = "QR, NFC, Датчики", Information = "Стабильно",
            Extra = "Пинг 1 мин назад", LastPingAt = DateTime.UtcNow.AddMinutes(-1), LastSaleAt = DateTime.UtcNow.AddMinutes(-3), LastCollectionAt = DateTime.UtcNow.AddHours(-22),
            LastServiceAt = DateTime.UtcNow.AddDays(-4), SalesTodayAmount = 4100, SalesSinceServiceAmount = 7300, SalesSinceServiceCount = 62, AdditionalStatuses = ["ok"]
        },
        new()
        {
            Number = 4, TradingPoint = "ТП-016", MachineName = "ТА Вокзал", State = "stopped", StateLabel = "Не работает", ConnectionType = "gsm", ConnectionLabel = "GSM",
            TotalLoadPercent = 19, MinLoadPercent = 0, Coins = 45, Bills = 120, Change = 130, Events = "offline", Equipment = "Офлайн-модем", Information = "Нет связи",
            Extra = "Пинг 3 ч назад", LastPingAt = DateTime.UtcNow.AddHours(-3), LastSaleAt = DateTime.UtcNow.AddHours(-5), LastCollectionAt = DateTime.UtcNow.AddDays(-2),
            LastServiceAt = DateTime.UtcNow.AddDays(-9), SalesTodayAmount = 420, SalesSinceServiceAmount = 990, SalesSinceServiceCount = 8, AdditionalStatuses = ["offline"]
        },
    ];

    [BindProperty(SupportsGet = true)]
    public List<string> SelectedStates { get; set; } = [];

    [BindProperty(SupportsGet = true)]
    public string? SelectedConnectionType { get; set; }

    [BindProperty(SupportsGet = true)]
    public List<string> SelectedAdditionalStatuses { get; set; } = [];

    [BindProperty(SupportsGet = true)]
    public string SortBy { get; set; } = "state";

    public bool IsFilterApplied { get; private set; }
    public List<MonitorMachineRow> Rows { get; private set; } = [];
    public int TotalMachines => Rows.Count;
    public decimal TotalCash => Rows.Sum(x => x.CashTotal);
    public bool HasRows => Rows.Count > 0;

    public IReadOnlyList<MonitorOption> StateOptions { get; } =
    [
        new("working", "Работает"),
        new("stopped", "Не работает"),
        new("service", "На обслуживании"),
    ];

    public IReadOnlyList<MonitorOption> ConnectionTypeOptions { get; } =
    [
        new("4g", "4G"),
        new("gsm", "GSM"),
        new("wifi", "Wi-Fi"),
        new("ethernet", "Ethernet"),
    ];

    public IReadOnlyList<MonitorOption> AdditionalStatusOptions { get; } =
    [
        new("paper_low", "Нет бумаги"),
        new("door_alarm", "Открыта дверь"),
        new("offline", "Офлайн"),
        new("ok", "Норма"),
    ];

    public IReadOnlyList<MonitorSortOption> SortOptions { get; } =
    [
        new("state", "По состоянию ТА"),
        new("name", "По названию ТА"),
        new("ping_asc", "По времени пинга ↑"),
        new("ping_desc", "По времени пинга ↓"),
        new("load_total_asc", "По общей загрузке ↑"),
        new("load_total_desc", "По общей загрузке ↓"),
        new("load_min_asc", "По минимальной загрузке ↑"),
        new("load_min_desc", "По минимальной загрузке ↓"),
        new("coins_asc", "По сумме монет ↑"),
        new("coins_desc", "По сумме монет ↓"),
        new("bills_asc", "По сумме купюр ↑"),
        new("bills_desc", "По сумме купюр ↓"),
        new("change_asc", "По сумме сдачи ↑"),
        new("change_desc", "По сумме сдачи ↓"),
        new("sale_time_asc", "По времени продажи ↑"),
        new("sale_time_desc", "По времени продажи ↓"),
        new("collection_time_asc", "По времени инкассации ↑"),
        new("collection_time_desc", "По времени инкассации ↓"),
        new("service_time_asc", "По времени обслуживания ↑"),
        new("service_time_desc", "По времени обслуживания ↓"),
        new("sales_today_asc", "По сумме продаж сегодня ↑"),
        new("sales_today_desc", "По сумме продаж сегодня ↓"),
        new("sales_service_amount_asc", "По сумме продаж с обслуживания ↑"),
        new("sales_service_amount_desc", "По сумме продаж с обслуживания ↓"),
        new("sales_service_count_asc", "По количеству продаж с обслуживания ↑"),
        new("sales_service_count_desc", "По количеству продаж с обслуживания ↓"),
    ];

    public void OnGet([FromQuery] bool apply = false)
    {
        IsFilterApplied = apply;
        IEnumerable<MonitorMachineRow> query = AllRows;

        if (IsFilterApplied)
        {
            if (SelectedStates.Count > 0)
            {
                query = query.Where(x => SelectedStates.Contains(x.State, StringComparer.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(SelectedConnectionType))
            {
                query = query.Where(x => string.Equals(x.ConnectionType, SelectedConnectionType, StringComparison.OrdinalIgnoreCase));
            }

            if (SelectedAdditionalStatuses.Count > 0)
            {
                query = query.Where(x => x.AdditionalStatuses.Any(status =>
                    SelectedAdditionalStatuses.Contains(status, StringComparer.OrdinalIgnoreCase)));
            }
        }

        Rows = ApplySort(query, SortBy).ToList();
    }

    public bool IsStateChecked(string value) => SelectedStates.Contains(value, StringComparer.OrdinalIgnoreCase);
    public bool IsAdditionalStatusChecked(string value) => SelectedAdditionalStatuses.Contains(value, StringComparer.OrdinalIgnoreCase);
    public bool IsSelectedConnectionType(string value) => string.Equals(SelectedConnectionType, value, StringComparison.OrdinalIgnoreCase);

    private static IEnumerable<MonitorMachineRow> ApplySort(IEnumerable<MonitorMachineRow> rows, string sortBy)
    {
        return sortBy switch
        {
            "name" => rows.OrderBy(x => x.MachineName),
            "ping_asc" => rows.OrderBy(x => x.LastPingAt),
            "ping_desc" => rows.OrderByDescending(x => x.LastPingAt),
            "load_total_asc" => rows.OrderBy(x => x.TotalLoadPercent),
            "load_total_desc" => rows.OrderByDescending(x => x.TotalLoadPercent),
            "load_min_asc" => rows.OrderBy(x => x.MinLoadPercent),
            "load_min_desc" => rows.OrderByDescending(x => x.MinLoadPercent),
            "coins_asc" => rows.OrderBy(x => x.Coins),
            "coins_desc" => rows.OrderByDescending(x => x.Coins),
            "bills_asc" => rows.OrderBy(x => x.Bills),
            "bills_desc" => rows.OrderByDescending(x => x.Bills),
            "change_asc" => rows.OrderBy(x => x.Change),
            "change_desc" => rows.OrderByDescending(x => x.Change),
            "sale_time_asc" => rows.OrderBy(x => x.LastSaleAt),
            "sale_time_desc" => rows.OrderByDescending(x => x.LastSaleAt),
            "collection_time_asc" => rows.OrderBy(x => x.LastCollectionAt),
            "collection_time_desc" => rows.OrderByDescending(x => x.LastCollectionAt),
            "service_time_asc" => rows.OrderBy(x => x.LastServiceAt),
            "service_time_desc" => rows.OrderByDescending(x => x.LastServiceAt),
            "sales_today_asc" => rows.OrderBy(x => x.SalesTodayAmount),
            "sales_today_desc" => rows.OrderByDescending(x => x.SalesTodayAmount),
            "sales_service_amount_asc" => rows.OrderBy(x => x.SalesSinceServiceAmount),
            "sales_service_amount_desc" => rows.OrderByDescending(x => x.SalesSinceServiceAmount),
            "sales_service_count_asc" => rows.OrderBy(x => x.SalesSinceServiceCount),
            "sales_service_count_desc" => rows.OrderByDescending(x => x.SalesSinceServiceCount),
            _ => rows.OrderBy(x => x.State).ThenBy(x => x.MachineName),
        };
    }
}
