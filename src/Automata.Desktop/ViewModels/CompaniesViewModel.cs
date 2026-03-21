using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Automata.Application.Companies.Models;
using Automata.Application.Companies.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Automata.Desktop.ViewModels
{
    /// <summary>
    /// Модель данных модального окна создания/редактирования компании.
    /// Хранит только поля, которые реально используются формой.
    /// </summary>
    public partial class CompanyDialogViewModel : ObservableObject
    {
        [ObservableProperty]
        private Guid? id;

        [ObservableProperty]
        private CompanyLookupItem? selectedParentCompany;

        [ObservableProperty]
        private string name = string.Empty;

        [ObservableProperty]
        private string contacts = string.Empty;

        [ObservableProperty]
        private string address = string.Empty;

        [ObservableProperty]
        private string? notes;

        // Поля не выводятся отдельными контролами, но сохраняются при редактировании существующей компании.
        [ObservableProperty]
        private string? phone;

        [ObservableProperty]
        private string? email;
    }

    /// <summary>
    /// ViewModel экрана «Компании».
    /// Отвечает за список, поиск, CRUD и модальное окно редактирования.
    /// </summary>
    public partial class CompaniesViewModel : ViewModelBase
    {
        private static readonly TimeSpan AutoFilterDelay = TimeSpan.FromMilliseconds(250);

        private readonly ICompanyService _companyService;
        private readonly bool _canManage;
        private readonly CompanyLookupItem _noParentItem = new()
        {
            Id = Guid.Empty,
            Name = "— Без вышестоящей компании —",
        };

        private List<CompanyLookupItem> _parentLookupCache = new();
        private CancellationTokenSource? _autoFilterDelayTokenSource;
        private int _reloadRequestVersion;
        private bool _suppressAutoFiltering;

        [ObservableProperty]
        private string? searchText;

        [ObservableProperty]
        private CompanyListItem? selectedCompany;

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private string? errorMessage;

        [ObservableProperty]
        private string? actionMessage;

        [ObservableProperty]
        private string recordsCounterText = "0";

        [ObservableProperty]
        private bool isDialogOpen;

        [ObservableProperty]
        private bool isCreateMode;

        [ObservableProperty]
        private string dialogTitle = "Компания";

        [ObservableProperty]
        private string? dialogErrorMessage;

        [ObservableProperty]
        private CompanyDialogViewModel dialog = new();

        [ObservableProperty]
        private int selectedPageSize = 50;

        public CompaniesViewModel()
            : this(new DesignCompanyService(), true)
        {
        }

        public CompaniesViewModel(ICompanyService companyService, bool canManage)
        {
            _companyService = companyService ?? throw new ArgumentNullException(nameof(companyService));
            _canManage = canManage;

            Companies = new ObservableCollection<CompanyListItem>();
            ParentCompanies = new ObservableCollection<CompanyLookupItem>();
            PageSizes = new ObservableCollection<int> { 50 };

            if (!_canManage)
            {
                ErrorMessage = "Раздел доступен только администратору.";
                return;
            }

            _ = LoadAsync();
        }

        /// <summary>
        /// Текущий список компаний (с учетом активного поиска).
        /// </summary>
        public ObservableCollection<CompanyListItem> Companies { get; }

        /// <summary>
        /// Набор значений для выпадающего списка «Вышестоящая компания» в модальном окне.
        /// </summary>
        public ObservableCollection<CompanyLookupItem> ParentCompanies { get; }

        /// <summary>
        /// Визуальный селектор количества строк (в текущей версии фиксирован на 50).
        /// </summary>
        public ObservableCollection<int> PageSizes { get; }

        public bool CanManage => _canManage;
        public bool HasCompanies => Companies.Count > 0;
        public bool HasNoCompanies => !IsLoading && string.IsNullOrWhiteSpace(ErrorMessage) && Companies.Count == 0;
        public bool CanEditSelectedCompany => CanManage && SelectedCompany is not null;
        public bool CanDeleteSelectedCompany => CanManage && SelectedCompany is not null;
        public string DialogSubmitButtonText => IsCreateMode ? "Добавить" : "Сохранить";

        partial void OnSearchTextChanged(string? value)
        {
            TriggerAutoFiltering();
        }

        partial void OnSelectedCompanyChanged(CompanyListItem? value)
        {
            EditSelectedCompanyCommand.NotifyCanExecuteChanged();
            DeleteSelectedCompanyCommand.NotifyCanExecuteChanged();
            OnPropertyChanged(nameof(CanEditSelectedCompany));
            OnPropertyChanged(nameof(CanDeleteSelectedCompany));
        }

        partial void OnIsCreateModeChanged(bool value)
        {
            OnPropertyChanged(nameof(DialogSubmitButtonText));
        }

        [RelayCommand]
        private async Task LoadAsync()
        {
            if (!CanManage || IsLoading)
            {
                return;
            }

            IsLoading = true;
            ErrorMessage = null;
            ActionMessage = null;
            DialogErrorMessage = null;
            CancelPendingAutoFiltering();

            try
            {
                await RefreshParentLookupCacheAsync();
                var requestVersion = Interlocked.Increment(ref _reloadRequestVersion);
                await ReloadCompaniesAsync(requestVersion);
            }
            catch (Exception ex)
            {
                Companies.Clear();
                RecordsCounterText = "0";
                ErrorMessage = $"Не удалось загрузить компании: {ex.Message}";
                OnPropertyChanged(nameof(HasCompanies));
                OnPropertyChanged(nameof(HasNoCompanies));
            }
            finally
            {
                IsLoading = false;
                OnPropertyChanged(nameof(HasNoCompanies));
            }
        }

        [RelayCommand]
        private async Task RefreshAsync()
        {
            await ReloadCompaniesSafeAsync(SelectedCompany?.Id);
        }

        [RelayCommand]
        private async Task ResetFilterAsync()
        {
            _suppressAutoFiltering = true;
            SearchText = null;
            _suppressAutoFiltering = false;

            ErrorMessage = null;
            ActionMessage = null;

            await ReloadCompaniesSafeAsync();
        }

        [RelayCommand]
        private void OpenCreateDialog()
        {
            if (!CanManage)
            {
                return;
            }

            ErrorMessage = null;
            DialogErrorMessage = null;
            IsCreateMode = true;
            DialogTitle = "Добавление компании";

            RebuildParentCompanies(excludeCompanyId: null, selectedParentId: null, preferFirstParent: true);

            Dialog = new CompanyDialogViewModel
            {
                SelectedParentCompany = ParentCompanies.FirstOrDefault(company => company.Id != Guid.Empty) ?? _noParentItem,
            };

            IsDialogOpen = true;
        }

        [RelayCommand(CanExecute = nameof(CanEditSelectedCompany))]
        private async Task EditSelectedCompanyAsync()
        {
            if (SelectedCompany is null)
            {
                return;
            }

            await OpenEditDialogForAsync(SelectedCompany);
        }

        [RelayCommand]
        private async Task EditCompanyFromRowAsync(CompanyListItem? company)
        {
            if (company is null)
            {
                return;
            }

            SelectedCompany = company;
            await OpenEditDialogForAsync(company);
        }

        [RelayCommand(CanExecute = nameof(CanDeleteSelectedCompany))]
        private async Task DeleteSelectedCompanyAsync()
        {
            if (!CanManage || SelectedCompany is null)
            {
                return;
            }

            try
            {
                var deletedName = SelectedCompany.Name;
                await _companyService.DeleteAsync(SelectedCompany.Id);

                if (Dialog.Id == SelectedCompany.Id)
                {
                    IsDialogOpen = false;
                }

                ActionMessage = $"Компания «{deletedName}» удалена.";
                ErrorMessage = null;
                DialogErrorMessage = null;

                await RefreshParentLookupCacheAsync();
                await ReloadCompaniesSafeAsync();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Не удалось удалить компанию: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task DeleteCompanyFromRowAsync(CompanyListItem? company)
        {
            if (company is null)
            {
                return;
            }

            SelectedCompany = company;
            await DeleteSelectedCompanyAsync();
        }

        [RelayCommand]
        private void CloseDialog()
        {
            // Закрываем окно редактирования и очищаем локальные сообщения в форме.
            IsDialogOpen = false;
            DialogErrorMessage = null;
            DialogTitle = "Компания";
            IsCreateMode = false;
        }

        [RelayCommand]
        private async Task SaveDialogAsync()
        {
            if (!CanManage)
            {
                return;
            }

            var validationError = ValidateDialog();
            if (validationError is not null)
            {
                DialogErrorMessage = validationError;
                return;
            }

            var model = new CompanyEditModel
            {
                Id = IsCreateMode ? null : Dialog.Id,
                ParentCompanyId = Dialog.SelectedParentCompany?.Id == Guid.Empty
                    ? null
                    : Dialog.SelectedParentCompany?.Id,
                Name = Dialog.Name.Trim(),
                ContactPerson = Normalize(Dialog.Contacts),
                Phone = Normalize(Dialog.Phone),
                Email = Normalize(Dialog.Email),
                Address = Normalize(Dialog.Address),
                Notes = Normalize(Dialog.Notes),
            };

            try
            {
                Guid targetCompanyId;
                if (IsCreateMode)
                {
                    targetCompanyId = await _companyService.CreateAsync(model);
                    ActionMessage = "Компания успешно создана.";
                }
                else
                {
                    if (!model.Id.HasValue)
                    {
                        DialogErrorMessage = "Не удалось определить компанию для сохранения.";
                        return;
                    }

                    await _companyService.UpdateAsync(model);
                    targetCompanyId = model.Id.Value;
                    ActionMessage = "Данные компании обновлены.";
                }

                ErrorMessage = null;
                DialogErrorMessage = null;
                IsDialogOpen = false;
                IsCreateMode = false;

                await RefreshParentLookupCacheAsync();
                await ReloadCompaniesSafeAsync(targetCompanyId);
            }
            catch (Exception ex)
            {
                DialogErrorMessage = $"Не удалось сохранить компанию: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task ExportCsvAsync()
        {
            if (!CanManage)
            {
                return;
            }

            try
            {
                var csv = BuildCsv(Companies);
                var exportPath = ResolveExportPath();
                await File.WriteAllTextAsync(exportPath, csv, new UTF8Encoding(true));

                ErrorMessage = null;
                ActionMessage = $"CSV экспортирован: {exportPath}";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Не удалось выполнить экспорт: {ex.Message}";
            }
        }

        private async Task OpenEditDialogForAsync(CompanyListItem company)
        {
            if (!CanManage)
            {
                return;
            }

            try
            {
                var model = await _companyService.GetByIdAsync(company.Id);
                if (model is null)
                {
                    ErrorMessage = "Компания не найдена.";
                    return;
                }

                ErrorMessage = null;
                DialogErrorMessage = null;
                IsCreateMode = false;
                DialogTitle = $"Редактирование «{model.Name}»";

                RebuildParentCompanies(excludeCompanyId: model.Id, selectedParentId: model.ParentCompanyId, preferFirstParent: false);

                Dialog = new CompanyDialogViewModel
                {
                    Id = model.Id,
                    SelectedParentCompany = ParentCompanies.FirstOrDefault(parent => parent.Id == (model.ParentCompanyId ?? Guid.Empty))
                                            ?? _noParentItem,
                    Name = model.Name,
                    Contacts = model.ContactPerson ?? model.Phone ?? model.Email ?? string.Empty,
                    Address = model.Address ?? string.Empty,
                    Notes = model.Notes,
                    Phone = model.Phone,
                    Email = model.Email,
                };

                IsDialogOpen = true;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Не удалось открыть карточку компании: {ex.Message}";
            }
        }

        private async Task ReloadCompaniesSafeAsync(Guid? preferredSelectedCompanyId = null)
        {
            if (!CanManage)
            {
                return;
            }

            var requestVersion = Interlocked.Increment(ref _reloadRequestVersion);
            IsLoading = true;
            ErrorMessage = null;

            try
            {
                await ReloadCompaniesAsync(requestVersion, preferredSelectedCompanyId);
            }
            catch (Exception ex)
            {
                if (requestVersion == _reloadRequestVersion)
                {
                    Companies.Clear();
                    RecordsCounterText = "0";
                    ErrorMessage = $"Не удалось загрузить компании: {ex.Message}";
                    OnPropertyChanged(nameof(HasCompanies));
                    OnPropertyChanged(nameof(HasNoCompanies));
                }
            }
            finally
            {
                if (requestVersion == _reloadRequestVersion)
                {
                    IsLoading = false;
                    OnPropertyChanged(nameof(HasNoCompanies));
                }
            }
        }

        private async Task ReloadCompaniesAsync(
            int requestVersion,
            Guid? preferredSelectedCompanyId = null,
            CancellationToken cancellationToken = default)
        {
            var items = await _companyService.GetListAsync(SearchText, cancellationToken);

            if (requestVersion != _reloadRequestVersion)
            {
                return;
            }

            var selectedCompanyId = preferredSelectedCompanyId ?? SelectedCompany?.Id;

            Companies.Clear();
            foreach (var company in items)
            {
                Companies.Add(company);
            }

            RecordsCounterText = Companies.Count.ToString();

            if (selectedCompanyId.HasValue)
            {
                SelectedCompany = Companies.FirstOrDefault(company => company.Id == selectedCompanyId.Value);
            }
            else
            {
                SelectedCompany = null;
            }

            OnPropertyChanged(nameof(HasCompanies));
            OnPropertyChanged(nameof(HasNoCompanies));
        }

        private async Task RefreshParentLookupCacheAsync(CancellationToken cancellationToken = default)
        {
            var parents = await _companyService.GetParentLookupAsync(cancellationToken);
            _parentLookupCache = parents.ToList();
        }

        private void RebuildParentCompanies(Guid? excludeCompanyId, Guid? selectedParentId, bool preferFirstParent)
        {
            ParentCompanies.Clear();
            ParentCompanies.Add(_noParentItem);

            foreach (var parent in _parentLookupCache.Where(company => company.Id != excludeCompanyId))
            {
                ParentCompanies.Add(parent);
            }

            CompanyLookupItem selectedParent;

            if (selectedParentId.HasValue)
            {
                selectedParent = ParentCompanies.FirstOrDefault(company => company.Id == selectedParentId.Value)
                                 ?? _noParentItem;
            }
            else if (preferFirstParent)
            {
                selectedParent = ParentCompanies.FirstOrDefault(company => company.Id != Guid.Empty)
                                 ?? _noParentItem;
            }
            else
            {
                selectedParent = _noParentItem;
            }

            Dialog.SelectedParentCompany = selectedParent;
        }

        private void TriggerAutoFiltering()
        {
            if (_suppressAutoFiltering || !CanManage)
            {
                return;
            }

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
                await ReloadCompaniesSafeAsync();
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

        private string? ValidateDialog()
        {
            if (string.IsNullOrWhiteSpace(Dialog.Name))
            {
                return "Название компании обязательно.";
            }

            if (string.IsNullOrWhiteSpace(Dialog.Address))
            {
                return "Адрес компании обязателен.";
            }

            if (string.IsNullOrWhiteSpace(Dialog.Contacts))
            {
                return "Контакты компании обязательны.";
            }

            if (Dialog.Id.HasValue && Dialog.SelectedParentCompany?.Id == Dialog.Id.Value)
            {
                return "Компания не может быть вышестоящей сама для себя.";
            }

            if (!string.IsNullOrWhiteSpace(Dialog.Email) && !Dialog.Email.Contains('@'))
            {
                return "Укажите корректный email компании.";
            }

            return null;
        }

        private static string BuildCsv(IEnumerable<CompanyListItem> companies)
        {
            var builder = new StringBuilder();
            builder.AppendLine("Название;Вышестоящая компания;Адрес;Контакты;В работе с");

            foreach (var company in companies)
            {
                builder.Append(EscapeCsv(company.Name)).Append(';')
                    .Append(EscapeCsv(company.ParentCompanyName)).Append(';')
                    .Append(EscapeCsv(company.Address)).Append(';')
                    .Append(EscapeCsv(company.ContactsDisplay)).Append(';')
                    .Append(EscapeCsv(company.CreatedAt.ToLocalTime().ToString("dd.MM.yyyy")))
                    .AppendLine();
            }

            return builder.ToString();
        }

        private static string EscapeCsv(string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            var escaped = value.Replace("\"", "\"\"");
            return $"\"{escaped}\"";
        }

        private static string ResolveExportPath()
        {
            var fileName = $"companies-{DateTime.Now:yyyyMMdd-HHmmss}.csv";
            var desktopDirectory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);

            if (!string.IsNullOrWhiteSpace(desktopDirectory) && Directory.Exists(desktopDirectory))
            {
                return Path.Combine(desktopDirectory, fileName);
            }

            return Path.Combine(AppContext.BaseDirectory, fileName);
        }

        private static string? Normalize(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        /// <summary>
        /// Минимальный design-time сервис для конструктора без реальной БД.
        /// </summary>
        private sealed class DesignCompanyService : ICompanyService
        {
            public Task<IReadOnlyList<CompanyListItem>> GetListAsync(
                string? search,
                CancellationToken cancellationToken = default)
            {
                IReadOnlyList<CompanyListItem> items = Array.Empty<CompanyListItem>();
                return Task.FromResult(items);
            }

            public Task<IReadOnlyList<CompanyLookupItem>> GetParentLookupAsync(
                CancellationToken cancellationToken = default)
            {
                IReadOnlyList<CompanyLookupItem> items = Array.Empty<CompanyLookupItem>();
                return Task.FromResult(items);
            }

            public Task<CompanyEditModel?> GetByIdAsync(Guid companyId, CancellationToken cancellationToken = default)
            {
                CompanyEditModel? model = null;
                return Task.FromResult(model);
            }

            public Task<Guid> CreateAsync(CompanyEditModel model, CancellationToken cancellationToken = default)
            {
                return Task.FromResult(Guid.NewGuid());
            }

            public Task UpdateAsync(CompanyEditModel model, CancellationToken cancellationToken = default)
            {
                return Task.CompletedTask;
            }

            public Task DeleteAsync(Guid companyId, CancellationToken cancellationToken = default)
            {
                return Task.CompletedTask;
            }
        }
    }
}
