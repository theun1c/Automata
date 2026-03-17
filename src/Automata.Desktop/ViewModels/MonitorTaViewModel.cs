using System.Collections.ObjectModel;

namespace Automata.Desktop.ViewModels;

public sealed class MonitorTaRowViewModel
{
    public int Number { get; init; }
    public string TradingPoint { get; init; } = string.Empty;
    public string Connection { get; init; } = string.Empty;
    public string Load { get; init; } = string.Empty;
    public string Cash { get; init; } = string.Empty;
    public string Events { get; init; } = string.Empty;
    public string Equipment { get; init; } = string.Empty;
    public string Information { get; init; } = string.Empty;
    public string Extra { get; init; } = string.Empty;
}

public sealed class MonitorTaViewModel : ViewModelBase
{
    public MonitorTaViewModel()
    {
        Rows = new ObservableCollection<MonitorTaRowViewModel>
        {
            new()
            {
                Number = 1,
                TradingPoint = "903823 / БЦ «Московский»",
                Connection = "T2, 4G",
                Load = "Общая 88%, мин. 22%",
                Cash = "7 820 ₽ (куп), 1 642 ₽ (мон)",
                Events = "11 мин назад / 2 дня назад",
                Equipment = "Купюропр., QR, NFC",
                Information = "MDB / онлайн",
                Extra = "112 / 247",
            },
            new()
            {
                Number = 2,
                TradingPoint = "903828 / ГП «Магнит»",
                Connection = "Ethernet",
                Load = "Общая 72%, мин. 19%",
                Cash = "3 350 ₽ (куп), 1 346 ₽ (мон)",
                Events = "3 часа назад / 5 часов назад",
                Equipment = "Монетопр., NFC",
                Information = "EXE / ST",
                Extra = "28 / 5",
            },
            new()
            {
                Number = 3,
                TradingPoint = "903825 / ДОСААФ",
                Connection = "Wi-Fi",
                Load = "Общая 69%, мин. 24%",
                Cash = "2 600 ₽ (куп), 1 439 ₽ (мон)",
                Events = "18 мин назад / вчера",
                Equipment = "Купюропр., QR",
                Information = "MDB",
                Extra = "31 / 34",
            },
        };
    }

    public ObservableCollection<MonitorTaRowViewModel> Rows { get; }
    public int TotalMachines => Rows.Count;
    public string TotalCash => "22 460 ₽ + 12 129 ₽ (сдача)";
}
