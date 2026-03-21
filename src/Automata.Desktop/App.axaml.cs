using System;
using System.Linq;
using System.Threading.Tasks;
using Automata.Application.Auth.Models;
using Automata.Application.Auth.Services;
using Automata.Application.Companies.Services;
using Automata.Application.Dashboard.Services;
using Automata.Application.Inventory.Services;
using Automata.Application.Machines.Services;
using Automata.Application.Monitoring.Services;
using Automata.Application.Users.Services;
using Automata.Desktop.ViewModels;
using Automata.Desktop.Views;
using Automata.Infrastructure.Services;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;

namespace Automata.Desktop
{
    /// <summary>
    /// Composition root desktop-клиента.
    /// Здесь связываем окна, ViewModel и инфраструктурные сервисы без отдельного DI-контейнера.
    /// </summary>
    public partial class App : Avalonia.Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                DisableAvaloniaDataAnnotationValidation();

                // Создаем набор сервисов один раз на сессию приложения.
                var services = BuildDesktopServices();
                var loginWindow = BuildLoginWindow(desktop, services);
                desktop.MainWindow = loginWindow;
            }

            base.OnFrameworkInitializationCompleted();
        }

        /// <summary>
        /// Открывает отдельное окно регистрации и временно скрывает окно входа,
        /// чтобы пользователь работал только с одним контекстом одновременно.
        /// </summary>
        private static Task OpenRegistrationWindowAsync(Window owner, IAuthService authService)
        {
            var registrationWindow = new RegistrationWindow();
            registrationWindow.DataContext = new RegistrationViewModel(authService, () => registrationWindow.Close());
            var completion = new TaskCompletionSource<object?>();

            void OnRegistrationClosed(object? _, EventArgs __)
            {
                registrationWindow.Closed -= OnRegistrationClosed;

                if (!owner.IsVisible)
                {
                    owner.Show();
                }

                completion.TrySetResult(null);
            }

            registrationWindow.Closed += OnRegistrationClosed;

            try
            {
                owner.Hide();
                registrationWindow.Show();
                return completion.Task;
            }
            catch
            {
                registrationWindow.Closed -= OnRegistrationClosed;
                if (!owner.IsVisible)
                {
                    owner.Show();
                }

                throw;
            }
        }

        private static LoginWindow BuildLoginWindow(
            IClassicDesktopStyleApplicationLifetime desktop,
            DesktopServices services)
        {
            var loginWindow = new LoginWindow();
            loginWindow.DataContext = new LoginViewModel(
                services.AuthService,
                () => OpenRegistrationWindowAsync(loginWindow, services.AuthService),
                user => OpenMainWindow(desktop, loginWindow, services, user));
            return loginWindow;
        }

        private static void OpenMainWindow(
            IClassicDesktopStyleApplicationLifetime desktop,
            Window loginWindow,
            DesktopServices services,
            AuthenticatedUser user)
        {
            var mainWindow = new MainWindow();
            mainWindow.DataContext = new MainWindowViewModel(
                services.HomeDashboardService,
                services.VendingMachineService,
                services.ProductInventoryService,
                services.MonitoringService,
                services.UserAdministrationService,
                () => SignOutToLogin(desktop, mainWindow, services),
                user,
                services.CompanyService);

            desktop.MainWindow = mainWindow;
            mainWindow.Show();
            loginWindow.Close();
        }

        /// <summary>
        /// Сценарий выхода: закрываем главное окно и возвращаем окно входа.
        /// </summary>
        private static void SignOutToLogin(
            IClassicDesktopStyleApplicationLifetime desktop,
            Window mainWindow,
            DesktopServices services)
        {
            var loginWindow = BuildLoginWindow(desktop, services);

            desktop.MainWindow = loginWindow;
            loginWindow.Show();
            mainWindow.Close();
        }

        private static DesktopServices BuildDesktopServices()
        {
            var connectionString = BuildConnectionString();

            return new DesktopServices(
                new HomeDashboardService(connectionString),
                new VendingMachineService(connectionString),
                new ProductInventoryService(connectionString),
                new MonitoringService(connectionString),
                new UserAdministrationService(connectionString),
                new CompanyService(connectionString),
                new AuthService(connectionString));
        }

        private static string BuildConnectionString()
        {
            var connectionString = Environment.GetEnvironmentVariable("AUTOMATA_CONNECTION_STRING");

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                connectionString = "Host=edu.ngknn.ru;Port=5442;Database=Belov;Username=21P;Password=123;Search Path=automata";
            }

            return connectionString;
        }

        /// <summary>
        /// В Avalonia отключаем встроенную DataAnnotations-валидацию,
        /// потому что проект использует свою MVVM-валидацию и сообщения ошибок.
        /// </summary>
        private void DisableAvaloniaDataAnnotationValidation()
        {
            var dataValidationPluginsToRemove =
                BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

            foreach (var plugin in dataValidationPluginsToRemove)
            {
                BindingPlugins.DataValidators.Remove(plugin);
            }
        }

        /// <summary>
        /// Локальный контейнер инфраструктурных зависимостей desktop-клиента.
        /// </summary>
        private sealed record DesktopServices(
            IHomeDashboardService HomeDashboardService,
            IVendingMachineService VendingMachineService,
            IProductInventoryService ProductInventoryService,
            IMonitoringService MonitoringService,
            IUserAdministrationService UserAdministrationService,
            ICompanyService CompanyService,
            IAuthService AuthService);
    }
}
