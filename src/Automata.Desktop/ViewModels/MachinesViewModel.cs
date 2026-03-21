using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Automata.Application.Common;
using Automata.Application.Machines.Models;
using Automata.Application.Machines.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Automata.Desktop.ViewModels
{
    /// <summary>
    /// Состояние формы создания/редактирования торгового автомата.
    /// </summary>
    public partial class MachineEditFormModel : ObservableObject
    {
        [ObservableProperty]
        private Guid? id;

        [ObservableProperty]
        private int statusId = 1;

        [ObservableProperty]
        private DateOnly installedAt = DateOnly.FromDateTime(DateTime.Today);

        [ObservableProperty]
        private DateOnly? lastServiceAt;

        [ObservableProperty]
        private decimal totalIncome;

        [ObservableProperty]
        private string machineName = string.Empty;

        [ObservableProperty]
        private string address = string.Empty;

        [ObservableProperty]
        private string place = string.Empty;

        [ObservableProperty]
        private string? coordinates;

        [ObservableProperty]
        private string machineNumber = string.Empty;

        [ObservableProperty]
        private string? selectedOperatingMode;

        [ObservableProperty]
        private string? workingHours;

        [ObservableProperty]
        private string? selectedTimeZone;

        [ObservableProperty]
        private string? notes;

        [ObservableProperty]
        private string? kitOnlineCashboxId;

        [ObservableProperty]
        private string? selectedServicePriority;

        [ObservableProperty]
        private bool supportsCoinAcceptor = true;

        [ObservableProperty]
        private bool supportsBillAcceptor = true;

        [ObservableProperty]
        private bool supportsCashlessModule;

        [ObservableProperty]
        private bool supportsQrPayments;

        [ObservableProperty]
        private string? serviceRfidCards;

        [ObservableProperty]
        private string? collectionRfidCards;

        [ObservableProperty]
        private string? loadingRfidCards;

        [ObservableProperty]
        private LookupItem? selectedModem;

        [ObservableProperty]
        private LookupItem? selectedProductMatrix;

        [ObservableProperty]
        private LookupItem? selectedCriticalTemplate;

        [ObservableProperty]
        private LookupItem? selectedNotificationTemplate;

        [ObservableProperty]
        private UserLookupItem? selectedManager;

        [ObservableProperty]
        private UserLookupItem? selectedEngineer;

        [ObservableProperty]
        private UserLookupItem? selectedTechnicianOperator;

        [ObservableProperty]
        private string? clientName;
    }

    /// <summary>
    /// ViewModel раздела "Администрирование / Торговые автоматы".
    /// Поддерживает таблицу, фильтры и модальную форму карточки автомата.
    /// </summary>
    public partial class MachinesViewModel : ViewModelBase
    {
        private readonly IVendingMachineService _vendingMachineService;
        private readonly LookupItem _allStatusesItem = new() { Id = 0, Name = "Все статусы" };
        private readonly LookupItem _emptyLookupItem = new() { Id = 0, Name = "Не задан" };
        private readonly UserLookupItem _emptyUserItem = new() { Id = Guid.Empty, DisplayName = "Не задан" };
        private readonly List<MachineModelLookupItem> _machineModelsCatalog = new();

        private CancellationTokenSource? _autoFilterDelayTokenSource;
        private bool _suppressAutoFiltering;
        private bool _suppressModelSelectionRefresh;
        private bool _editorLookupsLoaded;
        private int _reloadRequestVersion;

        private static readonly TimeSpan AutoFilterDelay = TimeSpan.FromMilliseconds(250);

        [ObservableProperty]
        private string? searchText;

        [ObservableProperty]
        private LookupItem? selectedStatus;

        [ObservableProperty]
        private LookupItem? selectedSort;

        [ObservableProperty]
        private VendingMachineListItem? selectedMachine;

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private string? errorMessage;

        [ObservableProperty]
        private string? actionMessage;

        [ObservableProperty]
        private string recordsCounterText = "0";

        [ObservableProperty]
        private bool isEditorOpen;

        [ObservableProperty]
        private bool isEditMode;

        [ObservableProperty]
        private string editorTitle = "Создание торгового автомата";

        [ObservableProperty]
        private MachineEditFormModel editForm = new();

        [ObservableProperty]
        private string? selectedMainBrand;

        [ObservableProperty]
        private MachineModelLookupItem? selectedMainModel;

        [ObservableProperty]
        private string? selectedSlaveBrand;

        [ObservableProperty]
        private MachineModelLookupItem? selectedSlaveModel;

        public MachinesViewModel()
            : this(new DesignVendingMachineService())
        {
        }

        public MachinesViewModel(IVendingMachineService vendingMachineService)
        {
            _vendingMachineService = vendingMachineService;

            // Коллекции таблицы, фильтров и lookup-данных editor-формы.
            Machines = new ObservableCollection<VendingMachineListItem>();
            Statuses = new ObservableCollection<LookupItem> { _allStatusesItem };
            SortOptions = new ObservableCollection<LookupItem>
            {
                new() { Id = 1, Name = "Все (по названию А-Я)" },
                new() { Id = 2, Name = "Название Я-А" },
                new() { Id = 3, Name = "Локация А-Я" },
                new() { Id = 4, Name = "Доход: по убыванию" },
                new() { Id = 5, Name = "Дата установки: новые" },
            };

            MainBrands = new ObservableCollection<string>();
            MainModels = new ObservableCollection<MachineModelLookupItem>();
            SlaveBrands = new ObservableCollection<string>();
            SlaveModels = new ObservableCollection<MachineModelLookupItem>();
            Modems = new ObservableCollection<LookupItem>();
            ProductMatrices = new ObservableCollection<LookupItem>();
            CriticalTemplates = new ObservableCollection<LookupItem>();
            NotificationTemplates = new ObservableCollection<LookupItem>();
            ManagerUsers = new ObservableCollection<UserLookupItem>();
            EngineerUsers = new ObservableCollection<UserLookupItem>();
            TechnicianUsers = new ObservableCollection<UserLookupItem>();
            OperatingModes = new ObservableCollection<string> { "Стандартный", "Расширенный", "24/7" };
            TimeZones = new ObservableCollection<string> { "UTC +3", "UTC +4", "UTC +5", "UTC +6" };
            ServicePriorities = new ObservableCollection<string> { "Низкий", "Средний", "Высокий" };

            _suppressAutoFiltering = true;
            SelectedStatus = _allStatusesItem;
            SelectedSort = SortOptions[0];
            _suppressAutoFiltering = false;

            _ = LoadAsync();
        }

        public ObservableCollection<VendingMachineListItem> Machines { get; }
        public ObservableCollection<LookupItem> Statuses { get; }
        public ObservableCollection<LookupItem> SortOptions { get; }
        public ObservableCollection<string> MainBrands { get; }
        public ObservableCollection<MachineModelLookupItem> MainModels { get; }
        public ObservableCollection<string> SlaveBrands { get; }
        public ObservableCollection<MachineModelLookupItem> SlaveModels { get; }
        public ObservableCollection<LookupItem> Modems { get; }
        public ObservableCollection<LookupItem> ProductMatrices { get; }
        public ObservableCollection<LookupItem> CriticalTemplates { get; }
        public ObservableCollection<LookupItem> NotificationTemplates { get; }
        public ObservableCollection<UserLookupItem> ManagerUsers { get; }
        public ObservableCollection<UserLookupItem> EngineerUsers { get; }
        public ObservableCollection<UserLookupItem> TechnicianUsers { get; }
        public ObservableCollection<string> OperatingModes { get; }
        public ObservableCollection<string> TimeZones { get; }
        public ObservableCollection<string> ServicePriorities { get; }

        public bool HasMachines => Machines.Count > 0;
        public bool HasNoMachines => !IsLoading && string.IsNullOrWhiteSpace(ErrorMessage) && Machines.Count == 0;
        public bool CanEditSelectedMachine => SelectedMachine is not null;
        public bool CanDeleteSelectedMachine => SelectedMachine is not null;
        public string ModeHint => IsEditorOpen
            ? "Заполните поля формы и нажмите «Сохранить»."
            : "Создание и редактирование выполняются в модальном окне.";

        partial void OnSearchTextChanged(string? value)
        {
            TriggerAutoFiltering();
        }

        partial void OnSelectedStatusChanged(LookupItem? value)
        {
            TriggerAutoFiltering();
        }

        partial void OnSelectedSortChanged(LookupItem? value)
        {
            if (_suppressAutoFiltering)
            {
                return;
            }

            ApplySortToCurrentMachines();
        }

        partial void OnSelectedMachineChanged(VendingMachineListItem? value)
        {
            OpenEditMachineFormCommand.NotifyCanExecuteChanged();
            OpenSelectedMachineCommand.NotifyCanExecuteChanged();
            DeleteSelectedMachineCommand.NotifyCanExecuteChanged();
            OnPropertyChanged(nameof(CanDeleteSelectedMachine));
        }

        partial void OnSelectedMainBrandChanged(string? value)
        {
            if (_suppressModelSelectionRefresh)
            {
                return;
            }

            RebuildMainModels(SelectedMainModel?.Id);
        }

        partial void OnSelectedSlaveBrandChanged(string? value)
        {
            if (_suppressModelSelectionRefresh)
            {
                return;
            }

            RebuildSlaveModels(SelectedSlaveModel?.Id);
        }

        [RelayCommand]
        private async Task LoadAsync()
        {
            if (IsLoading)
            {
                return;
            }

            // Полная загрузка статусов и списка автоматов.
            IsLoading = true;
            ErrorMessage = null;
            ActionMessage = null;
            CancelPendingAutoFiltering();

            try
            {
                var statuses = await _vendingMachineService.GetStatusesAsync();

                _suppressAutoFiltering = true;
                try
                {
                    Statuses.Clear();
                    Statuses.Add(_allStatusesItem);
                    foreach (var status in statuses)
                    {
                        Statuses.Add(status);
                    }

                    if (SelectedStatus is null || !Statuses.Any(item => item.Id == SelectedStatus.Id))
                    {
                        SelectedStatus = _allStatusesItem;
                    }
                }
                finally
                {
                    _suppressAutoFiltering = false;
                }

                var requestVersion = Interlocked.Increment(ref _reloadRequestVersion);
                await ReloadMachinesAsync(requestVersion);
            }
            catch (Exception ex)
            {
                Machines.Clear();
                Statuses.Clear();
                RecordsCounterText = "0";
                ErrorMessage = $"Не удалось загрузить список автоматов: {ex.Message}";
                OnPropertyChanged(nameof(HasMachines));
                OnPropertyChanged(nameof(HasNoMachines));
            }
            finally
            {
                IsLoading = false;
                OnPropertyChanged(nameof(HasNoMachines));
            }
        }

        [RelayCommand]
        private async Task ApplyFilterAsync()
        {
            await ReloadMachinesSafeAsync();
        }

        [RelayCommand]
        private async Task RefreshMachinesAsync()
        {
            await ReloadMachinesSafeAsync(SelectedMachine?.Id);
        }

        [RelayCommand]
        private async Task ResetFilterAsync()
        {
            _suppressAutoFiltering = true;
            SearchText = null;
            SelectedStatus = _allStatusesItem;
            SelectedSort = SortOptions[0];
            _suppressAutoFiltering = false;

            ActionMessage = null;
            ErrorMessage = null;

            await ReloadMachinesSafeAsync();
        }

        [RelayCommand]
        private async Task OpenCreateMachineFormAsync()
        {
            try
            {
                await EnsureEditorLookupsLoadedAsync();
                OpenCreateEditor();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Не удалось открыть форму создания: {ex.Message}";
            }
        }

        [RelayCommand(CanExecute = nameof(CanEditSelectedMachine))]
        private async Task OpenEditMachineFormAsync()
        {
            if (SelectedMachine is null)
            {
                ErrorMessage = "Сначала выберите автомат в списке.";
                return;
            }

            try
            {
                await EnsureEditorLookupsLoadedAsync();

                var machine = await _vendingMachineService.GetMachineForEditAsync(SelectedMachine.Id);
                if (machine is null)
                {
                    ErrorMessage = "Карточка автомата не найдена.";
                    return;
                }

                OpenEditEditor(machine);
                ErrorMessage = null;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Не удалось открыть карточку автомата: {ex.Message}";
            }
        }

        [RelayCommand(CanExecute = nameof(CanEditSelectedMachine))]
        private Task OpenSelectedMachineAsync()
        {
            return OpenEditMachineFormAsync();
        }

        [RelayCommand(CanExecute = nameof(CanDeleteSelectedMachine))]
        private async Task DeleteSelectedMachineAsync()
        {
            if (SelectedMachine is null)
            {
                ErrorMessage = "Сначала выберите автомат для удаления.";
                return;
            }

            try
            {
                var deletedName = SelectedMachine.Name;
                await _vendingMachineService.DeleteMachineAsync(SelectedMachine.Id);

                if (IsEditorOpen && EditForm.Id == SelectedMachine.Id)
                {
                    IsEditorOpen = false;
                }

                ErrorMessage = null;
                ActionMessage = $"Торговый автомат «{deletedName}» удален.";
                await ReloadMachinesSafeAsync();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Не удалось удалить автомат: {ex.Message}";
            }
        }

        [RelayCommand]
        private void CancelMachineForm()
        {
            IsEditorOpen = false;
            ErrorMessage = null;
        }

        [RelayCommand]
        private async Task SaveMachineFormAsync()
        {
            // Единая точка сохранения create/edit сценариев.
            var validationError = ValidateEditor();
            if (validationError is not null)
            {
                ErrorMessage = validationError;
                return;
            }

            var model = BuildEditModel();

            try
            {
                Guid machineId;
                if (IsEditMode)
                {
                    await _vendingMachineService.UpdateMachineAsync(model);
                    machineId = model.Id!.Value;
                    ActionMessage = "Торговый автомат успешно обновлен.";
                }
                else
                {
                    machineId = await _vendingMachineService.CreateMachineAsync(model);
                    ActionMessage = "Торговый автомат успешно создан.";
                }

                ErrorMessage = null;
                IsEditorOpen = false;
                await ReloadMachinesSafeAsync(machineId);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Не удалось сохранить автомат: {ex.Message}";
            }
        }

        private async Task ReloadMachinesSafeAsync(Guid? preferredMachineId = null)
        {
            var requestVersion = Interlocked.Increment(ref _reloadRequestVersion);

            IsLoading = true;
            ErrorMessage = null;

            try
            {
                await ReloadMachinesAsync(requestVersion, preferredMachineId);
            }
            catch (Exception ex)
            {
                if (requestVersion == _reloadRequestVersion)
                {
                    Machines.Clear();
                    RecordsCounterText = "0";
                    ErrorMessage = $"Не удалось загрузить список автоматов: {ex.Message}";
                    OnPropertyChanged(nameof(HasMachines));
                    OnPropertyChanged(nameof(HasNoMachines));
                }
            }
            finally
            {
                if (requestVersion == _reloadRequestVersion)
                {
                    IsLoading = false;
                    OnPropertyChanged(nameof(HasNoMachines));
                }
            }
        }

        private async Task ReloadMachinesAsync(
            int requestVersion,
            Guid? preferredMachineId = null,
            CancellationToken cancellationToken = default)
        {
            // Читаем список автоматов с учетом текущих фильтров.
            int? selectedStatusId = SelectedStatus is { Id: > 0 }
                ? SelectedStatus.Id
                : null;

            var items = await _vendingMachineService.GetListAsync(
                SearchText,
                selectedStatusId,
                cancellationToken);

            if (requestVersion != _reloadRequestVersion)
            {
                return;
            }

            var sortedItems = ApplySorting(items);

            Machines.Clear();
            foreach (var item in sortedItems)
            {
                Machines.Add(item);
            }

            RecordsCounterText = Machines.Count.ToString();

            var machineIdToSelect = preferredMachineId ?? SelectedMachine?.Id;

            if (machineIdToSelect.HasValue)
            {
                SelectedMachine = Machines.FirstOrDefault(machine => machine.Id == machineIdToSelect.Value);
            }
            else
            {
                SelectedMachine = null;
            }

            OnPropertyChanged(nameof(HasMachines));
            OnPropertyChanged(nameof(HasNoMachines));
        }

        private void TriggerAutoFiltering()
        {
            if (_suppressAutoFiltering)
            {
                return;
            }

            // Debounce фильтрации, чтобы не дергать сервис на каждый символ.
            StartAutoFiltering();
        }

        private void StartAutoFiltering()
        {
            _autoFilterDelayTokenSource?.Cancel();
            _autoFilterDelayTokenSource?.Dispose();

            var tokenSource = new CancellationTokenSource();
            _autoFilterDelayTokenSource = tokenSource;
            _ = RunDelayedAutoFilteringAsync(tokenSource);
        }

        private async Task RunDelayedAutoFilteringAsync(CancellationTokenSource tokenSource)
        {
            try
            {
                await Task.Delay(AutoFilterDelay, tokenSource.Token);
                await ReloadMachinesSafeAsync();
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                if (ReferenceEquals(_autoFilterDelayTokenSource, tokenSource))
                {
                    _autoFilterDelayTokenSource = null;
                }

                tokenSource.Dispose();
            }
        }

        private void CancelPendingAutoFiltering()
        {
            _autoFilterDelayTokenSource?.Cancel();
            _autoFilterDelayTokenSource?.Dispose();
            _autoFilterDelayTokenSource = null;
        }

        private void ApplySortToCurrentMachines()
        {
            if (Machines.Count == 0)
            {
                return;
            }

            var selectedId = SelectedMachine?.Id;
            var sorted = ApplySorting(Machines.ToList());

            Machines.Clear();
            foreach (var item in sorted)
            {
                Machines.Add(item);
            }

            if (selectedId.HasValue)
            {
                SelectedMachine = Machines.FirstOrDefault(machine => machine.Id == selectedId.Value);
            }
        }

        private IEnumerable<VendingMachineListItem> ApplySorting(IEnumerable<VendingMachineListItem> source)
        {
            return (SelectedSort?.Id ?? 1) switch
            {
                2 => source.OrderByDescending(item => item.Name, StringComparer.CurrentCultureIgnoreCase),
                3 => source.OrderBy(item => item.Location, StringComparer.CurrentCultureIgnoreCase),
                4 => source.OrderByDescending(item => item.TotalIncome),
                5 => source.OrderByDescending(item => item.InstalledAt),
                _ => source.OrderBy(item => item.Name, StringComparer.CurrentCultureIgnoreCase),
            };
        }

        private async Task EnsureEditorLookupsLoadedAsync()
        {
            var lookups = await _vendingMachineService.GetEditorLookupsAsync();

            _machineModelsCatalog.Clear();
            _machineModelsCatalog.AddRange(lookups.MachineModels);

            FillLookupCollection(Modems, lookups.Modems, includeEmpty: false);
            FillLookupCollection(ProductMatrices, lookups.ProductMatrices, includeEmpty: true);
            FillLookupCollection(CriticalTemplates, lookups.CriticalValueTemplates, includeEmpty: true);
            FillLookupCollection(NotificationTemplates, lookups.NotificationTemplates, includeEmpty: true);

            FillUserCollections(lookups.Users);
            BuildBrandCollections();
            _editorLookupsLoaded = true;
        }

        private void FillLookupCollection(
            ObservableCollection<LookupItem> target,
            IReadOnlyList<LookupItem> source,
            bool includeEmpty)
        {
            target.Clear();

            if (includeEmpty)
            {
                target.Add(_emptyLookupItem);
            }

            foreach (var item in source)
            {
                target.Add(item);
            }
        }

        private void FillUserCollections(IReadOnlyList<UserLookupItem> users)
        {
            ManagerUsers.Clear();
            EngineerUsers.Clear();
            TechnicianUsers.Clear();

            ManagerUsers.Add(_emptyUserItem);
            EngineerUsers.Add(_emptyUserItem);
            TechnicianUsers.Add(_emptyUserItem);

            foreach (var user in users.OrderBy(item => item.DisplayName, StringComparer.CurrentCultureIgnoreCase))
            {
                var roleName = user.RoleName.Trim();

                if (string.Equals(roleName, "Администратор", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(roleName, "Оператор", StringComparison.OrdinalIgnoreCase))
                {
                    ManagerUsers.Add(user);
                }

                if (string.Equals(roleName, "Инженер", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(roleName, "Администратор", StringComparison.OrdinalIgnoreCase))
                {
                    EngineerUsers.Add(user);
                }

                if (string.Equals(roleName, "Оператор", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(roleName, "Администратор", StringComparison.OrdinalIgnoreCase))
                {
                    TechnicianUsers.Add(user);
                }
            }
        }

        private void BuildBrandCollections()
        {
            var brands = _machineModelsCatalog
                .Select(model => model.Brand)
                .Distinct(StringComparer.CurrentCultureIgnoreCase)
                .OrderBy(brand => brand, StringComparer.CurrentCultureIgnoreCase)
                .ToList();

            MainBrands.Clear();
            foreach (var brand in brands)
            {
                MainBrands.Add(brand);
            }

            SlaveBrands.Clear();
            SlaveBrands.Add("-");
            foreach (var brand in brands)
            {
                SlaveBrands.Add(brand);
            }
        }

        private void OpenCreateEditor()
        {
            if (!_editorLookupsLoaded)
            {
                return;
            }

            IsEditMode = false;
            EditorTitle = "Создание торгового автомата";

            EditForm = new MachineEditFormModel
            {
                SelectedOperatingMode = OperatingModes.FirstOrDefault(),
                SelectedTimeZone = TimeZones.FirstOrDefault(),
                SelectedServicePriority = ServicePriorities.FirstOrDefault(),
                SelectedModem = Modems.FirstOrDefault(),
                SelectedProductMatrix = ProductMatrices.FirstOrDefault(),
                SelectedCriticalTemplate = CriticalTemplates.FirstOrDefault(),
                SelectedNotificationTemplate = NotificationTemplates.FirstOrDefault(),
                SelectedManager = ManagerUsers.FirstOrDefault(),
                SelectedEngineer = EngineerUsers.FirstOrDefault(),
                SelectedTechnicianOperator = TechnicianUsers.FirstOrDefault(),
            };

            _suppressModelSelectionRefresh = true;
            SelectedMainBrand = MainBrands.FirstOrDefault();
            SelectedSlaveBrand = SlaveBrands.FirstOrDefault();
            _suppressModelSelectionRefresh = false;

            RebuildMainModels(null);
            RebuildSlaveModels(null);
            IsEditorOpen = true;
        }

        private void OpenEditEditor(VendingMachineEditModel machine)
        {
            if (!_editorLookupsLoaded)
            {
                return;
            }

            IsEditMode = true;
            EditorTitle = "Редактирование торгового автомата";

            EditForm = new MachineEditFormModel
            {
                Id = machine.Id,
                StatusId = machine.StatusId,
                InstalledAt = machine.InstalledAt,
                LastServiceAt = machine.LastServiceAt,
                TotalIncome = machine.TotalIncome,
                MachineName = machine.Name,
                Address = machine.Address,
                Place = machine.Place,
                Coordinates = machine.Coordinates,
                MachineNumber = machine.MachineNumber,
                SelectedOperatingMode = string.IsNullOrWhiteSpace(machine.OperatingMode)
                    ? OperatingModes.FirstOrDefault()
                    : machine.OperatingMode,
                WorkingHours = machine.WorkingHours,
                SelectedTimeZone = string.IsNullOrWhiteSpace(machine.TimeZone)
                    ? TimeZones.FirstOrDefault()
                    : machine.TimeZone,
                Notes = machine.Notes,
                KitOnlineCashboxId = machine.KitOnlineCashboxId,
                SelectedServicePriority = string.IsNullOrWhiteSpace(machine.ServicePriority)
                    ? ServicePriorities.FirstOrDefault()
                    : machine.ServicePriority,
                SupportsCoinAcceptor = machine.SupportsCoinAcceptor,
                SupportsBillAcceptor = machine.SupportsBillAcceptor,
                SupportsCashlessModule = machine.SupportsCashlessModule,
                SupportsQrPayments = machine.SupportsQrPayments,
                ServiceRfidCards = machine.ServiceRfidCards,
                CollectionRfidCards = machine.CollectionRfidCards,
                LoadingRfidCards = machine.LoadingRfidCards,
                SelectedModem = FindLookupById(Modems, machine.ModemId),
                SelectedProductMatrix = FindLookupById(ProductMatrices, machine.ProductMatrixId),
                SelectedCriticalTemplate = FindLookupById(CriticalTemplates, machine.CriticalValueTemplateId),
                SelectedNotificationTemplate = FindLookupById(NotificationTemplates, machine.NotificationTemplateId),
                SelectedManager = FindUserById(ManagerUsers, machine.ManagerUserId),
                SelectedEngineer = FindUserById(EngineerUsers, machine.EngineerUserId),
                SelectedTechnicianOperator = FindUserById(TechnicianUsers, machine.TechnicianOperatorUserId),
                ClientName = machine.ClientName,
            };

            _suppressModelSelectionRefresh = true;
            var mainModel = _machineModelsCatalog.FirstOrDefault(item => item.Id == machine.MachineModelId);
            var slaveModel = machine.SlaveMachineModelId.HasValue
                ? _machineModelsCatalog.FirstOrDefault(item => item.Id == machine.SlaveMachineModelId.Value)
                : null;

            SelectedMainBrand = mainModel?.Brand ?? MainBrands.FirstOrDefault();
            SelectedSlaveBrand = slaveModel?.Brand ?? "-";
            _suppressModelSelectionRefresh = false;

            RebuildMainModels(mainModel?.Id);
            RebuildSlaveModels(slaveModel?.Id);
            IsEditorOpen = true;
        }

        private static LookupItem? FindLookupById(IEnumerable<LookupItem> collection, int? id)
        {
            if (!id.HasValue || id.Value <= 0)
            {
                return collection.FirstOrDefault(item => item.Id == 0);
            }

            return collection.FirstOrDefault(item => item.Id == id.Value)
                   ?? collection.FirstOrDefault(item => item.Id == 0);
        }

        private static UserLookupItem? FindUserById(IEnumerable<UserLookupItem> collection, Guid? id)
        {
            if (!id.HasValue || id.Value == Guid.Empty)
            {
                return collection.FirstOrDefault(item => item.Id == Guid.Empty);
            }

            return collection.FirstOrDefault(item => item.Id == id.Value)
                   ?? collection.FirstOrDefault(item => item.Id == Guid.Empty);
        }

        private void RebuildMainModels(int? preferredModelId)
        {
            MainModels.Clear();

            if (string.IsNullOrWhiteSpace(SelectedMainBrand))
            {
                SelectedMainModel = null;
                return;
            }

            var models = _machineModelsCatalog
                .Where(model => string.Equals(model.Brand, SelectedMainBrand, StringComparison.OrdinalIgnoreCase))
                .OrderBy(model => model.ModelName, StringComparer.CurrentCultureIgnoreCase)
                .ToList();

            foreach (var model in models)
            {
                MainModels.Add(model);
            }

            SelectedMainModel = preferredModelId.HasValue
                ? MainModels.FirstOrDefault(model => model.Id == preferredModelId.Value)
                : null;

            SelectedMainModel ??= MainModels.FirstOrDefault();
        }

        private void RebuildSlaveModels(int? preferredModelId)
        {
            SlaveModels.Clear();

            if (string.IsNullOrWhiteSpace(SelectedSlaveBrand) || SelectedSlaveBrand == "-")
            {
                SelectedSlaveModel = null;
                return;
            }

            var models = _machineModelsCatalog
                .Where(model => string.Equals(model.Brand, SelectedSlaveBrand, StringComparison.OrdinalIgnoreCase))
                .OrderBy(model => model.ModelName, StringComparer.CurrentCultureIgnoreCase)
                .ToList();

            foreach (var model in models)
            {
                SlaveModels.Add(model);
            }

            SelectedSlaveModel = preferredModelId.HasValue
                ? SlaveModels.FirstOrDefault(model => model.Id == preferredModelId.Value)
                : null;
        }

        private string? ValidateEditor()
        {
            if (SelectedMainModel is null)
            {
                return "Укажите модель торгового автомата.";
            }

            if (string.IsNullOrWhiteSpace(EditForm.MachineName))
            {
                return "Название торгового автомата обязательно.";
            }

            if (string.IsNullOrWhiteSpace(EditForm.MachineNumber))
            {
                return "Номер автомата обязателен.";
            }

            if (string.IsNullOrWhiteSpace(EditForm.Address))
            {
                return "Адрес обязателен.";
            }

            if (string.IsNullOrWhiteSpace(EditForm.Place))
            {
                return "Место установки обязательно.";
            }

            if (string.IsNullOrWhiteSpace(EditForm.SelectedTimeZone))
            {
                return "Часовой пояс обязателен.";
            }

            if (string.IsNullOrWhiteSpace(EditForm.SelectedServicePriority))
            {
                return "Приоритет обслуживания обязателен.";
            }

            if (string.IsNullOrWhiteSpace(EditForm.SelectedOperatingMode))
            {
                return "Режим работы обязателен.";
            }

            if (EditForm.SelectedModem is null)
            {
                return "Модем обязателен.";
            }

            if (!(EditForm.SupportsCoinAcceptor || EditForm.SupportsBillAcceptor ||
                  EditForm.SupportsCashlessModule || EditForm.SupportsQrPayments))
            {
                return "Выберите хотя бы одну платежную систему.";
            }

            if (!string.IsNullOrWhiteSpace(EditForm.WorkingHours))
            {
                var normalized = EditForm.WorkingHours.Trim();
                var parts = normalized.Split('-', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != 2 ||
                    !TimeOnly.TryParse(parts[0], out var from) ||
                    !TimeOnly.TryParse(parts[1], out var to) ||
                    from >= to)
                {
                    return "Время работы должно быть в формате HH:mm-HH:mm.";
                }
            }

            return null;
        }

        private VendingMachineEditModel BuildEditModel()
        {
            // Явный маппинг формы в DTO, чтобы все поля сохранялись предсказуемо.
            return new VendingMachineEditModel
            {
                Id = EditForm.Id,
                Name = EditForm.MachineName.Trim(),
                MachineModelId = SelectedMainModel!.Id,
                SlaveMachineModelId = SelectedSlaveModel?.Id,
                StatusId = EditForm.StatusId,
                InstalledAt = EditForm.InstalledAt,
                LastServiceAt = EditForm.LastServiceAt,
                TotalIncome = EditForm.TotalIncome,
                Address = EditForm.Address.Trim(),
                Place = EditForm.Place.Trim(),
                Coordinates = EditForm.Coordinates,
                MachineNumber = EditForm.MachineNumber.Trim(),
                OperatingMode = EditForm.SelectedOperatingMode!.Trim(),
                WorkingHours = EditForm.WorkingHours,
                TimeZone = EditForm.SelectedTimeZone!.Trim(),
                Notes = EditForm.Notes,
                KitOnlineCashboxId = EditForm.KitOnlineCashboxId,
                ServicePriority = EditForm.SelectedServicePriority!.Trim(),
                SupportsCoinAcceptor = EditForm.SupportsCoinAcceptor,
                SupportsBillAcceptor = EditForm.SupportsBillAcceptor,
                SupportsCashlessModule = EditForm.SupportsCashlessModule,
                SupportsQrPayments = EditForm.SupportsQrPayments,
                ServiceRfidCards = EditForm.ServiceRfidCards,
                CollectionRfidCards = EditForm.CollectionRfidCards,
                LoadingRfidCards = EditForm.LoadingRfidCards,
                ManagerUserId = NormalizeUserId(EditForm.SelectedManager),
                EngineerUserId = NormalizeUserId(EditForm.SelectedEngineer),
                TechnicianOperatorUserId = NormalizeUserId(EditForm.SelectedTechnicianOperator),
                ClientName = string.IsNullOrWhiteSpace(EditForm.ClientName) ? null : EditForm.ClientName.Trim(),
                ModemId = EditForm.SelectedModem?.Id,
                ProductMatrixId = NormalizeLookupId(EditForm.SelectedProductMatrix),
                CriticalValueTemplateId = NormalizeLookupId(EditForm.SelectedCriticalTemplate),
                NotificationTemplateId = NormalizeLookupId(EditForm.SelectedNotificationTemplate),
            };
        }

        private static int? NormalizeLookupId(LookupItem? lookup)
        {
            return lookup is { Id: > 0 } ? lookup.Id : null;
        }

        private static Guid? NormalizeUserId(UserLookupItem? user)
        {
            if (user is null || user.Id == Guid.Empty)
            {
                return null;
            }

            return user.Id;
        }

        private sealed class DesignVendingMachineService : IVendingMachineService
        {
            public Task<IReadOnlyList<VendingMachineListItem>> GetListAsync(
                string? search,
                int? statusId,
                CancellationToken cancellationToken = default)
            {
                IReadOnlyList<VendingMachineListItem> items = Array.Empty<VendingMachineListItem>();
                return Task.FromResult(items);
            }

            public Task<IReadOnlyList<LookupItem>> GetStatusesAsync(CancellationToken cancellationToken = default)
            {
                IReadOnlyList<LookupItem> items = Array.Empty<LookupItem>();
                return Task.FromResult(items);
            }

            public Task<VendingMachineEditorLookups> GetEditorLookupsAsync(CancellationToken cancellationToken = default)
            {
                return Task.FromResult(new VendingMachineEditorLookups());
            }

            public Task<VendingMachineEditModel?> GetMachineForEditAsync(Guid machineId, CancellationToken cancellationToken = default)
            {
                return Task.FromResult<VendingMachineEditModel?>(null);
            }

            public Task<Guid> CreateMachineAsync(VendingMachineEditModel model, CancellationToken cancellationToken = default)
            {
                return Task.FromResult(Guid.NewGuid());
            }

            public Task UpdateMachineAsync(VendingMachineEditModel model, CancellationToken cancellationToken = default)
            {
                return Task.CompletedTask;
            }

            public Task DeleteMachineAsync(Guid machineId, CancellationToken cancellationToken = default)
            {
                return Task.CompletedTask;
            }
        }
    }
}
