using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Automata.Desktop.ViewModels
{
    public partial class MachineRowViewModel : ObservableObject
    {
        [ObservableProperty]
        private int number;

        [ObservableProperty]
        private int id;

        [ObservableProperty]
        private string name = string.Empty;

        [ObservableProperty]
        private string model = string.Empty;

        [ObservableProperty]
        private string company = string.Empty;

        [ObservableProperty]
        private string modem = string.Empty;

        [ObservableProperty]
        private string place = string.Empty;

        [ObservableProperty]
        private string activeFrom = string.Empty;

        [ObservableProperty]
        private bool isOdd;

        public string RowBackground => IsOdd ? "#F8FAFC" : "#FFFFFF";
        public string ModemDisplay => string.IsNullOrWhiteSpace(Modem) ? "-1" : Modem;

        partial void OnIsOddChanged(bool value)
        {
            OnPropertyChanged(nameof(RowBackground));
        }

        partial void OnModemChanged(string value)
        {
            OnPropertyChanged(nameof(ModemDisplay));
        }
    }

    public partial class MachineEditorFormModel : ObservableObject
    {
        [ObservableProperty] private string machineName = string.Empty;
        [ObservableProperty] private string manufacturer = string.Empty;
        [ObservableProperty] private string model = string.Empty;
        [ObservableProperty] private string workMode = string.Empty;
        [ObservableProperty] private string slaveManufacturer = string.Empty;
        [ObservableProperty] private string slaveModel = string.Empty;
        [ObservableProperty] private string address = string.Empty;
        [ObservableProperty] private string place = string.Empty;
        [ObservableProperty] private string coordinates = string.Empty;
        [ObservableProperty] private string machineNumber = string.Empty;
        [ObservableProperty] private string workingHours = "08:00-22:00";
        [ObservableProperty] private string timezone = "Europe/Moscow";
        [ObservableProperty] private string productMatrix = string.Empty;
        [ObservableProperty] private string criticalTemplate = string.Empty;
        [ObservableProperty] private string notificationTemplate = string.Empty;
        [ObservableProperty] private string client = string.Empty;
        [ObservableProperty] private string manager = string.Empty;
        [ObservableProperty] private string engineer = string.Empty;
        [ObservableProperty] private string technician = string.Empty;
        [ObservableProperty] private bool hasCoinAcceptor = true;
        [ObservableProperty] private bool hasBillAcceptor = true;
        [ObservableProperty] private bool hasCashlessModule = true;
        [ObservableProperty] private bool hasQrPayment = true;
        [ObservableProperty] private string serviceRfidCard = string.Empty;
        [ObservableProperty] private string collectionRfidCard = string.Empty;
        [ObservableProperty] private string loadingRfidCard = string.Empty;
        [ObservableProperty] private string kitOnlineCashboxId = string.Empty;
        [ObservableProperty] private string servicePriority = "Средний";
        [ObservableProperty] private string modem = string.Empty;
        [ObservableProperty] private string notes = string.Empty;
    }

    public partial class MachinesViewModel : ViewModelBase
    {
        private readonly List<MachineRowViewModel> _allMachines;

        [ObservableProperty]
        private string filterName = string.Empty;

        [ObservableProperty]
        private int selectedPageSize = 5;

        [ObservableProperty]
        private int currentPage = 1;

        [ObservableProperty]
        private bool isTileView;

        [ObservableProperty]
        private string recordsCounterText = "0 из 0";

        [ObservableProperty]
        private string actionMessage = " ";

        [ObservableProperty]
        private bool isMachineFormOpen;

        [ObservableProperty]
        private string machineFormTitle = "Создание торгового автомата";

        [ObservableProperty]
        private MachineEditorFormModel machineForm = new();

        [ObservableProperty]
        private bool isDeleteConfirmOpen;

        [ObservableProperty]
        private bool isUnbindConfirmOpen;

        [ObservableProperty]
        private MachineRowViewModel? selectedMachine;

        public MachinesViewModel()
        {
            PageSizeOptions = new ObservableCollection<int> { 5, 10, 20 };
            VisibleMachines = new ObservableCollection<MachineRowViewModel>();
            _allMachines = BuildMockMachines();
            RefreshVisibleMachines();
        }

        public ObservableCollection<int> PageSizeOptions { get; }
        public ObservableCollection<MachineRowViewModel> VisibleMachines { get; }

        public bool IsTableView => !IsTileView;
        public bool CanGoPrev => CurrentPage > 1;
        public bool CanGoNext => CurrentPage < TotalPages;
        public bool HasMachines => VisibleMachines.Count > 0;
        public string PageText => $"Страница {CurrentPage} из {TotalPages}";

        partial void OnFilterNameChanged(string value)
        {
            CurrentPage = 1;
            RefreshVisibleMachines();
        }

        partial void OnSelectedPageSizeChanged(int value)
        {
            CurrentPage = 1;
            RefreshVisibleMachines();
        }

        partial void OnCurrentPageChanged(int value)
        {
            RefreshVisibleMachines();
        }

        partial void OnIsTileViewChanged(bool value)
        {
            OnPropertyChanged(nameof(IsTableView));
        }

        [RelayCommand]
        private void ApplyFilter()
        {
            CurrentPage = 1;
            RefreshVisibleMachines();
            ActionMessage = "Фильтр обновлен (mock).";
        }

        [RelayCommand]
        private void ClearFilter()
        {
            FilterName = string.Empty;
            CurrentPage = 1;
            RefreshVisibleMachines();
            ActionMessage = "Фильтр очищен.";
        }

        [RelayCommand]
        private void ToggleView()
        {
            IsTileView = !IsTileView;
        }

        [RelayCommand]
        private void PrevPage()
        {
            if (!CanGoPrev)
            {
                return;
            }

            CurrentPage--;
        }

        [RelayCommand]
        private void NextPage()
        {
            if (!CanGoNext)
            {
                return;
            }

            CurrentPage++;
        }

        [RelayCommand]
        private void ExportCsv()
        {
            ActionMessage = "Экспорт CSV: UI-заглушка (файл не создается).";
        }

        [RelayCommand]
        private void OpenCreateMachineForm()
        {
            MachineFormTitle = "Создание торгового автомата";
            MachineForm = new MachineEditorFormModel();
            IsMachineFormOpen = true;
        }

        [RelayCommand]
        private void OpenEditMachineForm(MachineRowViewModel row)
        {
            MachineFormTitle = $"Редактирование ТА #{row.Id}";
            MachineForm = new MachineEditorFormModel
            {
                MachineName = row.Name,
                Model = row.Model,
                Manufacturer = "Necta",
                Address = row.Place.Split(" / ")[0],
                Place = row.Place.Contains(" / ", StringComparison.Ordinal) ? row.Place.Split(" / ")[1] : row.Place,
                MachineNumber = $"VM-{row.Id:000}",
                Modem = row.ModemDisplay,
                Notes = "Демо-форма редактирования",
            };
            IsMachineFormOpen = true;
        }

        [RelayCommand]
        private void SaveMachineForm()
        {
            IsMachineFormOpen = false;
            ActionMessage = "Сохранение ТА: UI-only режим, данные не отправляются.";
        }

        [RelayCommand]
        private void CloseMachineForm()
        {
            IsMachineFormOpen = false;
        }

        [RelayCommand]
        private void AskDeleteMachine(MachineRowViewModel row)
        {
            SelectedMachine = row;
            IsDeleteConfirmOpen = true;
        }

        [RelayCommand]
        private void CancelDeleteMachine()
        {
            IsDeleteConfirmOpen = false;
            SelectedMachine = null;
        }

        [RelayCommand]
        private void ConfirmDeleteMachine()
        {
            if (SelectedMachine is null)
            {
                return;
            }

            _allMachines.Remove(SelectedMachine);
            IsDeleteConfirmOpen = false;
            ActionMessage = $"ТА «{SelectedMachine.Name}» удален (mock).";
            SelectedMachine = null;
            RefreshVisibleMachines();
        }

        [RelayCommand]
        private void AskUnbindModem(MachineRowViewModel row)
        {
            SelectedMachine = row;
            IsUnbindConfirmOpen = true;
        }

        [RelayCommand]
        private void CancelUnbindModem()
        {
            IsUnbindConfirmOpen = false;
            SelectedMachine = null;
        }

        [RelayCommand]
        private void ConfirmUnbindModem()
        {
            if (SelectedMachine is null)
            {
                return;
            }

            SelectedMachine.Modem = "-1";
            IsUnbindConfirmOpen = false;
            ActionMessage = $"Для ТА «{SelectedMachine.Name}» модем отвязан, отображается -1.";
            SelectedMachine = null;
            RefreshVisibleMachines();
        }

        private void RefreshVisibleMachines()
        {
            var filtered = GetFilteredMachines();
            var totalFiltered = filtered.Count;
            var safePageSize = Math.Max(1, SelectedPageSize);
            var pages = Math.Max(1, (int)Math.Ceiling(totalFiltered / (double)safePageSize));

            if (CurrentPage > pages)
            {
                CurrentPage = pages;
                return;
            }

            var start = (CurrentPage - 1) * safePageSize;
            var pageRows = filtered.Skip(start).Take(safePageSize).ToList();

            VisibleMachines.Clear();

            for (var index = 0; index < pageRows.Count; index++)
            {
                var row = pageRows[index];
                row.Number = start + index + 1;
                row.IsOdd = row.Number % 2 == 1;
                VisibleMachines.Add(row);
            }

            RecordsCounterText = $"{VisibleMachines.Count} из {totalFiltered}";
            OnPropertyChanged(nameof(TotalPages));
            OnPropertyChanged(nameof(PageText));
            OnPropertyChanged(nameof(CanGoPrev));
            OnPropertyChanged(nameof(CanGoNext));
            OnPropertyChanged(nameof(HasMachines));
        }

        private List<MachineRowViewModel> GetFilteredMachines()
        {
            IEnumerable<MachineRowViewModel> query = _allMachines;

            if (!string.IsNullOrWhiteSpace(FilterName))
            {
                query = query.Where(x => x.Name.Contains(FilterName.Trim(), StringComparison.OrdinalIgnoreCase));
            }

            return query.ToList();
        }

        private static List<MachineRowViewModel> BuildMockMachines()
        {
            return new List<MachineRowViewModel>
            {
                new() { Id = 101, Name = "ТА Парк Победы", Model = "Necta Canto", Company = "ООО ВендПарк", Modem = "M-0041", Place = "Москва, ул. Минская 2 / ТЦ Парк", ActiveFrom = "12.09.2023" },
                new() { Id = 102, Name = "ТА Бизнес Центр Север", Model = "Saeco Atlante", Company = "ООО ВендПарк", Modem = "-1", Place = "Москва, Дмитровское шоссе 7 / БЦ Север", ActiveFrom = "04.11.2023" },
                new() { Id = 103, Name = "ТА Университет", Model = "Jofemar Vision", Company = "АО ГородАвтомат", Modem = "M-0033", Place = "Москва, Ленинские горы 1 / Корпус Б", ActiveFrom = "20.01.2024" },
                new() { Id = 104, Name = "ТА Станция Киевская", Model = "Necta Kikko", Company = "АО ГородАвтомат", Modem = "M-0028", Place = "Москва, пл. Киевского вокзала 1 / Вестибюль", ActiveFrom = "07.02.2024" },
                new() { Id = 105, Name = "ТА Арена", Model = "Rheavendors XS", Company = "ООО АвтоСнаб", Modem = "M-0064", Place = "Москва, ул. Лужники 24 / Сектор C", ActiveFrom = "16.03.2024" },
                new() { Id = 106, Name = "ТА Технопарк", Model = "Necta Canto", Company = "ООО АвтоСнаб", Modem = "M-0071", Place = "Москва, пр-т Андропова 18 / Технопарк", ActiveFrom = "22.05.2024" },
                new() { Id = 107, Name = "ТА Склад Восток", Model = "Saeco Phedra", Company = "ООО ВендЛогистик", Modem = "M-0088", Place = "Балашиха, Индустриальный проезд 4 / КПП", ActiveFrom = "03.07.2024" },
                new() { Id = 108, Name = "ТА ТЦ Ясень", Model = "Jofemar Coffeemar", Company = "ООО ВендЛогистик", Modem = "M-0095", Place = "Москва, Новоясеневский пр-т 11 / 2 этаж", ActiveFrom = "14.08.2024" },
            };
        }

        private int TotalPages
        {
            get
            {
                var filteredCount = GetFilteredMachines().Count;
                return Math.Max(1, (int)Math.Ceiling(filteredCount / (double)Math.Max(1, SelectedPageSize)));
            }
        }
    }
}
