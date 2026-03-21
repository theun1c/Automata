using Automata.Application.Dashboard.Models;
using Automata.Application.Dashboard.Services;
using Automata.Desktop.ViewModels;

namespace Automata.Tests;

public class DashboardViewModelTests
{
    [Fact]
    public async Task RefreshCommand_LoadsDashboardData()
    {
        var dashboard = new HomeDashboardModel
        {
            TotalMachines = 10,
            WorkingMachines = 7,
            NotWorkingMachines = 3,
            LowStockProductsCount = 4,
            MoneyInMachines = 125000m,
            ChangeInMachines = 12000m,
            RevenueToday = 24000m,
            RevenueYesterday = 23000m,
            EncashedToday = 9000m,
            EncashedYesterday = 8800m,
            ServicedMachinesToday = 2,
            ServicedMachinesYesterday = 1,
            MachineStatuses =
            [
                new DashboardMachineStatusItem { Name = "Рабочий", Count = 7 },
                new DashboardMachineStatusItem { Name = "Не рабочий", Count = 3 },
            ],
            SalesDynamics =
            [
                new DashboardSalesDynamicsItem
                {
                    Day = new DateOnly(2026, 3, 19),
                    Amount = 120m,
                    Quantity = 2,
                },
                new DashboardSalesDynamicsItem
                {
                    Day = new DateOnly(2026, 3, 20),
                    Amount = 140m,
                    Quantity = 3,
                },
            ],
            RecentSales =
            [
                new RecentSaleItem
                {
                    SaleDate = new DateTimeOffset(2026, 3, 20, 12, 0, 0, TimeSpan.Zero),
                    MachineName = "ТА-1",
                    ProductName = "Вода",
                    Quantity = 2,
                    SaleAmount = 140m,
                    PaymentMethod = "Карта",
                },
            ],
            RecentMaintenance =
            [
                new RecentMaintenanceItem
                {
                    ServiceDate = new DateTimeOffset(2026, 3, 20, 10, 0, 0, TimeSpan.Zero),
                    MachineName = "ТА-2",
                    EngineerName = "Иванов Петр",
                    WorkDescription = "Проверка",
                },
            ],
            TopMachines =
            [
                new TopMachineItem
                {
                    MachineName = "ТА-3",
                    Location = "Холл",
                    TotalIncome = 10000m,
                },
            ],
            LowStockProducts =
            [
                new LowStockProductItem
                {
                    ProductName = "Сок",
                    MachineName = "ТА-1",
                    Quantity = 1,
                    MinStock = 5,
                },
            ],
        };

        var service = new FakeHomeDashboardService(dashboard);
        var viewModel = new DashboardViewModel(service);

        await viewModel.RefreshCommand.ExecuteAsync(null);

        Assert.Null(viewModel.ErrorMessage);
        Assert.Equal("10", viewModel.TotalMachinesText);
        Assert.Equal("7", viewModel.WorkingMachinesText);
        Assert.Equal("3", viewModel.NotWorkingMachinesText);
        Assert.Equal("125 000 ₽", viewModel.TotalIncomeText);
        Assert.Equal("4", viewModel.LowStockProductsText);

        Assert.Single(viewModel.RecentSales);
        Assert.Single(viewModel.RecentMaintenance);
        Assert.Single(viewModel.TopMachines);
        Assert.Single(viewModel.LowStockProducts);
        Assert.Equal("ТА-3", viewModel.TopMachines[0].MachineName);
    }

    private sealed class FakeHomeDashboardService(HomeDashboardModel dashboard) : IHomeDashboardService
    {
        public Task<HomeDashboardModel> GetDashboardAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(dashboard);
        }
    }
}
