using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Automata.Desktop.ViewModels
{
    public sealed class NetworkStatusViewModel
    {
        public NetworkStatusViewModel(string title, int count, string color, double barWidth)
        {
            Title = title;
            Count = count;
            Color = color;
            BarWidth = barWidth;
        }

        public string Title { get; }
        public int Count { get; }
        public string Color { get; }
        public double BarWidth { get; }
    }

    public sealed class SummaryCardViewModel
    {
        public SummaryCardViewModel(string title, string value)
        {
            Title = title;
            Value = value;
        }

        public string Title { get; }
        public string Value { get; }
    }

    public sealed class NewsItemViewModel
    {
        public NewsItemViewModel(string title, string date, string description)
        {
            Title = title;
            Date = date;
            Description = description;
        }

        public string Title { get; }
        public string Date { get; }
        public string Description { get; }
    }

    public partial class SalesPointViewModel : ObservableObject
    {
        private readonly decimal _amount;
        private readonly int _quantity;

        [ObservableProperty]
        private string value = string.Empty;

        [ObservableProperty]
        private double barWidth;

        [ObservableProperty]
        private double columnHeight;

        public SalesPointViewModel(string day, decimal amount, int quantity)
        {
            Day = day;
            _amount = amount;
            _quantity = quantity;
        }

        public string Day { get; }
        public decimal Amount => _amount;
        public int Quantity => _quantity;

        public void Update(bool isAmountMode, decimal maxAmount, int maxQuantity)
        {
            if (isAmountMode)
            {
                var ratio = maxAmount <= 0 ? 0 : (double)(_amount / maxAmount);
                BarWidth = Math.Round(Math.Clamp(ratio, 0, 1) * 360, 2);
                ColumnHeight = Math.Round(Math.Clamp(ratio, 0, 1) * 150, 2);
                Value = $"{_amount:N0} ₽";
                return;
            }

            var quantityRatio = maxQuantity <= 0 ? 0 : (double)_quantity / maxQuantity;
            BarWidth = Math.Round(Math.Clamp(quantityRatio, 0, 1) * 360, 2);
            ColumnHeight = Math.Round(Math.Clamp(quantityRatio, 0, 1) * 150, 2);
            Value = $"{_quantity} шт.";
        }
    }

    public partial class DashboardViewModel : ViewModelBase
    {
        [ObservableProperty]
        private bool isAmountMode = true;

        public DashboardViewModel()
        {
            NetworkStatuses = new ObservableCollection<NetworkStatusViewModel>
            {
                new("Работает", 96, "#16A34A", 320),
                new("Не работает", 8, "#DC2626", 110),
                new("На обслуживании", 12, "#2563EB", 170),
            };

            SummaryCards = new ObservableCollection<SummaryCardViewModel>
            {
                new("Денег в ТА", "2 459 000 ₽"),
                new("Сдача в ТА", "486 300 ₽"),
                new("Выручка сегодня", "184 200 ₽"),
                new("Выручка вчера", "176 900 ₽"),
                new("Инкассировано сегодня", "81 000 ₽"),
                new("Инкассировано вчера", "73 200 ₽"),
                new("Обслужено ТА сегодня", "14"),
                new("Обслужено ТА вчера", "11"),
            };

            SalesDynamics = new ObservableCollection<SalesPointViewModel>
            {
                new("07.03", 128_000m, 814),
                new("08.03", 136_200m, 870),
                new("09.03", 119_400m, 766),
                new("10.03", 141_000m, 892),
                new("11.03", 152_500m, 940),
                new("12.03", 147_800m, 915),
                new("13.03", 159_300m, 972),
                new("14.03", 165_400m, 1_004),
                new("15.03", 171_900m, 1_036),
                new("16.03", 184_200m, 1_092),
            };

            NewsItems = new ObservableCollection<NewsItemViewModel>
            {
                new("Обновление каталога товаров", "16.03.2026", "Добавлены новые позиции в матрицы сети."),
                new("Плановые профилактические работы", "15.03.2026", "Проверка модемов в центральном кластере."),
                new("Пик продаж выходного дня", "14.03.2026", "Суммарная выручка выросла на 8,2%."),
            };

            UpdateChart();
        }

        public ObservableCollection<NetworkStatusViewModel> NetworkStatuses { get; }
        public ObservableCollection<SummaryCardViewModel> SummaryCards { get; }
        public ObservableCollection<SalesPointViewModel> SalesDynamics { get; }
        public ObservableCollection<NewsItemViewModel> NewsItems { get; }

        public string EfficiencyText => "82.8%";
        public string ChartMode => IsAmountMode ? "Сумма" : "Количество";

        partial void OnIsAmountModeChanged(bool value)
        {
            UpdateChart();
            OnPropertyChanged(nameof(ChartMode));
        }

        [RelayCommand]
        private void ShowAmount()
        {
            IsAmountMode = true;
        }

        [RelayCommand]
        private void ShowQuantity()
        {
            IsAmountMode = false;
        }

        private void UpdateChart()
        {
            var maxAmount = SalesDynamics.Max(point => point.Amount);
            var maxQuantity = SalesDynamics.Max(point => point.Quantity);

            foreach (var point in SalesDynamics)
            {
                point.Update(IsAmountMode, maxAmount, maxQuantity);
            }
        }
    }
}
