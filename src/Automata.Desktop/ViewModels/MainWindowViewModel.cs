using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Automata.Desktop.ViewModels
{
    public enum DesktopSection
    {
        Login,
        Registration,
        Dashboard,
        MonitorTa,
        Machines,
        Companies,
    }

    public partial class MainWindowViewModel : ViewModelBase
    {
        private readonly Dictionary<DesktopSection, ViewModelBase> _sections;

        [ObservableProperty]
        private DesktopSection currentSection;

        [ObservableProperty]
        private ViewModelBase currentPage = null!;

        [ObservableProperty]
        private bool isAdminExpanded = true;

        public MainWindowViewModel()
        {
            var loginPage = new LoginViewModel(() => CurrentSection = DesktopSection.Registration);
            var registrationPage = new RegistrationViewModel(() => CurrentSection = DesktopSection.Login);

            _sections = new Dictionary<DesktopSection, ViewModelBase>
            {
                [DesktopSection.Login] = loginPage,
                [DesktopSection.Registration] = registrationPage,
                [DesktopSection.Dashboard] = new DashboardViewModel(),
                [DesktopSection.MonitorTa] = new MonitorTaViewModel(),
                [DesktopSection.Machines] = new MachinesViewModel(),
                [DesktopSection.Companies] = new CompaniesViewModel(),
            };

            CurrentSection = DesktopSection.Login;
            CurrentPage = _sections[CurrentSection];
        }

        public string EmployeeInitials => "АА";
        public string EmployeeName => "Автоматов А. А.";
        public string EmployeeRole => "Администратор";
        public string CompanyTitle => "ООО Торговые Автоматы";
        public string AdminMenuTitle => IsAdminExpanded ? "⚙  Администрирование     ˄" : "⚙  Администрирование     ˅";

        public string PageTitle => CurrentSection switch
        {
            DesktopSection.Login => "Вход",
            DesktopSection.Registration => "Регистрация",
            DesktopSection.Dashboard => "Главная",
            DesktopSection.MonitorTa => "Монитор ТА",
            DesktopSection.Machines => "Торговые автоматы",
            DesktopSection.Companies => "Компании",
            _ => "Раздел",
        };

        public string TopBarPath => CurrentSection switch
        {
            DesktopSection.Login => "Авторизация / Вход",
            DesktopSection.Registration => "Авторизация / Регистрация",
            DesktopSection.Dashboard => "Главная",
            DesktopSection.MonitorTa => "Главная / Монитор ТА",
            DesktopSection.Machines => "Администрирование / Торговые автоматы",
            DesktopSection.Companies => "Администрирование / Компании",
            _ => "Раздел",
        };

        public string ContentHeader => CurrentSection switch
        {
            DesktopSection.Login => "Авторизация. Вход",
            DesktopSection.Registration => "Авторизация. Регистрация",
            DesktopSection.Dashboard => "Личный кабинет. Главная",
            DesktopSection.MonitorTa => "Монитор ТА",
            DesktopSection.Machines => "Администрирование. Торговые автоматы",
            DesktopSection.Companies => "Администрирование. Компании",
            _ => "Раздел",
        };

        partial void OnCurrentSectionChanged(DesktopSection value)
        {
            if (_sections.TryGetValue(value, out var page))
            {
                CurrentPage = page;
                OnPropertyChanged(nameof(PageTitle));
                OnPropertyChanged(nameof(TopBarPath));
                OnPropertyChanged(nameof(ContentHeader));
            }
        }

        [RelayCommand]
        private void ShowLogin() => CurrentSection = DesktopSection.Login;

        [RelayCommand]
        private void ShowRegistration() => CurrentSection = DesktopSection.Registration;

        [RelayCommand]
        private void ShowDashboard() => CurrentSection = DesktopSection.Dashboard;

        [RelayCommand]
        private void ShowMonitorTa() => CurrentSection = DesktopSection.MonitorTa;

        [RelayCommand]
        private void ShowMachines() => CurrentSection = DesktopSection.Machines;

        [RelayCommand]
        private void ShowCompanies() => CurrentSection = DesktopSection.Companies;

        [RelayCommand]
        private void ToggleAdmin()
        {
            IsAdminExpanded = !IsAdminExpanded;
            OnPropertyChanged(nameof(AdminMenuTitle));
        }
    }
}
