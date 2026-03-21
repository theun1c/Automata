using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Automata.Application.Common;
using Automata.Application.Users.Models;
using Automata.Application.Users.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Automata.Desktop.ViewModels
{
    /// <summary>
    /// Модель полей формы создания/редактирования пользователя.
    /// </summary>
    public partial class UserEditFormModel : ObservableObject
    {
        [ObservableProperty]
        private Guid? id;

        [ObservableProperty]
        private string lastName = string.Empty;

        [ObservableProperty]
        private string firstName = string.Empty;

        [ObservableProperty]
        private string? middleName;

        [ObservableProperty]
        private string email = string.Empty;

        [ObservableProperty]
        private string? phone;

        [ObservableProperty]
        private LookupItem? selectedRole;
    }

    /// <summary>
    /// ViewModel экрана администрирования пользователей.
    /// Поддерживает поиск, фильтр, CRUD и смену пароля только для администратора.
    /// </summary>
    public partial class UsersViewModel : ViewModelBase
    {
        private readonly IUserAdministrationService _userAdministrationService;
        private readonly LookupItem _allRolesItem = new() { Id = 0, Name = "Все роли" };
        private readonly Guid _currentUserId;
        private readonly bool _canManage;
        private CancellationTokenSource? _autoFilterDelayTokenSource;
        private bool _suppressAutoFiltering;
        private int _reloadRequestVersion;

        private static readonly TimeSpan AutoFilterDelay = TimeSpan.FromMilliseconds(250);

        [ObservableProperty]
        private string? searchText;

        [ObservableProperty]
        private LookupItem? selectedRoleFilter;

        [ObservableProperty]
        private UserAdministrationListItem? selectedUser;

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private string? errorMessage;

        [ObservableProperty]
        private string? actionMessage;

        [ObservableProperty]
        private string recordsCounterText = "0";

        [ObservableProperty]
        private bool isCreateMode;

        [ObservableProperty]
        private bool isCreateDialogOpen;

        [ObservableProperty]
        private string editorTitle = "Данные пользователя";

        [ObservableProperty]
        private UserEditFormModel editForm = new();

        [ObservableProperty]
        private string createPassword = string.Empty;

        [ObservableProperty]
        private string passwordForReset = string.Empty;

        public UsersViewModel()
            : this(new DesignUserAdministrationService(), Guid.Empty, true)
        {
        }

        public UsersViewModel(
            IUserAdministrationService userAdministrationService,
            Guid currentUserId,
            bool canManage)
        {
            _userAdministrationService = userAdministrationService;
            _currentUserId = currentUserId;
            _canManage = canManage;

            Users = new ObservableCollection<UserAdministrationListItem>();
            RoleFilters = new ObservableCollection<LookupItem> { _allRolesItem };
            Roles = new ObservableCollection<LookupItem>();

            _suppressAutoFiltering = true;
            SelectedRoleFilter = _allRolesItem;
            _suppressAutoFiltering = false;

            if (!_canManage)
            {
                ErrorMessage = "Раздел доступен только администратору.";
                return;
            }

            _ = LoadAsync();
        }

        public ObservableCollection<UserAdministrationListItem> Users { get; }
        public ObservableCollection<LookupItem> RoleFilters { get; }
        public ObservableCollection<LookupItem> Roles { get; }

        public bool CanManage => _canManage;
        public bool HasUsers => Users.Count > 0;
        public bool HasNoUsers => !IsLoading && string.IsNullOrWhiteSpace(ErrorMessage) && Users.Count == 0;
        public bool CanEditSelectedUser => CanManage && SelectedUser is not null;
        public bool HasNoSelectedUser => !CanEditSelectedUser;
        public bool CanDeleteSelectedUser => CanManage && SelectedUser is not null;
        public bool CanChangePassword => CanManage && SelectedUser is not null;
        public string ModeHint => "Создание пользователя выполняется в модальном окне. " +
                                  "Редактирование выполняется по выбранной записи.";
        public string SelectedUserHint => SelectedUser is null
            ? "Пользователь не выбран."
            : $"Выбран: {SelectedUser.DisplayName}";

        partial void OnSearchTextChanged(string? value)
        {
            TriggerAutoFiltering();
        }

        partial void OnSelectedRoleFilterChanged(LookupItem? value)
        {
            TriggerAutoFiltering();
        }

        partial void OnSelectedUserChanged(UserAdministrationListItem? value)
        {
            DeleteUserCommand.NotifyCanExecuteChanged();
            ChangePasswordCommand.NotifyCanExecuteChanged();
            OnPropertyChanged(nameof(SelectedUserHint));
            OnPropertyChanged(nameof(CanEditSelectedUser));
            OnPropertyChanged(nameof(HasNoSelectedUser));
            OnPropertyChanged(nameof(CanDeleteSelectedUser));
            OnPropertyChanged(nameof(CanChangePassword));

            if (!CanManage || value is null)
            {
                return;
            }

            if (IsCreateDialogOpen)
            {
                return;
            }

            IsCreateMode = false;
            EditorTitle = $"Редактирование «{value.DisplayName}»";
            CreatePassword = string.Empty;
            PasswordForReset = string.Empty;
            FillEditForm(value);
        }

        partial void OnIsCreateModeChanged(bool value)
        {
            OnPropertyChanged(nameof(ModeHint));
            OnPropertyChanged(nameof(CanChangePassword));
        }

        partial void OnIsCreateDialogOpenChanged(bool value)
        {
            OnPropertyChanged(nameof(ModeHint));
        }

        [RelayCommand]
        private async Task LoadAsync()
        {
            if (!CanManage || IsLoading)
            {
                return;
            }

            // Инициализация справочников и первой загрузки списка пользователей.
            IsLoading = true;
            ErrorMessage = null;
            ActionMessage = null;
            CancelPendingAutoFiltering();

            try
            {
                var roles = await _userAdministrationService.GetRolesAsync();

                _suppressAutoFiltering = true;
                try
                {
                    RoleFilters.Clear();
                    RoleFilters.Add(_allRolesItem);

                    Roles.Clear();

                    foreach (var role in roles)
                    {
                        RoleFilters.Add(role);
                        Roles.Add(role);
                    }

                    if (SelectedRoleFilter is null || !RoleFilters.Any(role => role.Id == SelectedRoleFilter.Id))
                    {
                        SelectedRoleFilter = _allRolesItem;
                    }
                }
                finally
                {
                    _suppressAutoFiltering = false;
                }

                ResetEditForm();

                var requestVersion = Interlocked.Increment(ref _reloadRequestVersion);
                await ReloadUsersAsync(requestVersion);
            }
            catch (Exception ex)
            {
                Users.Clear();
                RoleFilters.Clear();
                Roles.Clear();
                RecordsCounterText = "0";
                ErrorMessage = $"Не удалось загрузить пользователей: {ex.Message}";
                OnPropertyChanged(nameof(HasUsers));
                OnPropertyChanged(nameof(HasNoUsers));
            }
            finally
            {
                IsLoading = false;
                OnPropertyChanged(nameof(HasNoUsers));
            }
        }

        [RelayCommand]
        private async Task RefreshUsersAsync()
        {
            await ReloadUsersSafeAsync(SelectedUser?.Id);
        }

        [RelayCommand]
        private async Task ResetFiltersAsync()
        {
            _suppressAutoFiltering = true;
            SearchText = null;
            SelectedRoleFilter = _allRolesItem;
            _suppressAutoFiltering = false;

            ErrorMessage = null;
            ActionMessage = null;

            await ReloadUsersSafeAsync();
        }

        [RelayCommand]
        private void StartCreateUser()
        {
            if (!CanManage)
            {
                return;
            }

            // Сценарий создания пользователя в модальном окне.
            IsCreateMode = true;
            EditorTitle = "Данные пользователя";
            SelectedUser = null;
            PasswordForReset = string.Empty;
            CreatePassword = string.Empty;
            ErrorMessage = null;

            EditForm = new UserEditFormModel
            {
                SelectedRole = Roles.FirstOrDefault(),
            };

            IsCreateDialogOpen = true;
        }

        [RelayCommand]
        private void CloseCreateDialog()
        {
            // Закрываем модалку и возвращаем форму в контекст текущего выбранного пользователя.
            IsCreateDialogOpen = false;
            IsCreateMode = false;

            if (SelectedUser is not null)
            {
                EditorTitle = $"Редактирование «{SelectedUser.DisplayName}»";
                FillEditForm(SelectedUser);
            }
            else
            {
                ResetEditForm();
            }
        }

        [RelayCommand]
        private void ResetEditor()
        {
            if (!CanManage)
            {
                return;
            }

            ErrorMessage = null;

            if (IsCreateMode)
            {
                EditForm = new UserEditFormModel
                {
                    SelectedRole = Roles.FirstOrDefault(),
                };
                CreatePassword = string.Empty;
                return;
            }

            if (SelectedUser is null)
            {
                ResetEditForm();
                return;
            }

            FillEditForm(SelectedUser);
            PasswordForReset = string.Empty;
            CreatePassword = string.Empty;
        }

        [RelayCommand]
        private async Task SaveUserAsync()
        {
            if (!CanManage)
            {
                return;
            }

            // Общий обработчик сохранения для create/edit.
            var validationError = ValidateEditForm();
            if (validationError is not null)
            {
                ErrorMessage = validationError;
                return;
            }

            var model = new UserEditModel
            {
                Id = IsCreateMode ? null : EditForm.Id,
                LastName = EditForm.LastName.Trim(),
                FirstName = EditForm.FirstName.Trim(),
                MiddleName = string.IsNullOrWhiteSpace(EditForm.MiddleName) ? null : EditForm.MiddleName.Trim(),
                Email = EditForm.Email.Trim(),
                Phone = string.IsNullOrWhiteSpace(EditForm.Phone) ? null : EditForm.Phone.Trim(),
                RoleId = EditForm.SelectedRole!.Id,
            };

            try
            {
                Guid targetId;
                if (IsCreateMode)
                {
                    if (string.IsNullOrWhiteSpace(CreatePassword))
                    {
                        ErrorMessage = "Для нового пользователя укажите стартовый пароль.";
                        return;
                    }

                    targetId = await _userAdministrationService.CreateUserAsync(model, CreatePassword);
                    ActionMessage = "Пользователь успешно создан.";
                    IsCreateMode = false;
                    IsCreateDialogOpen = false;
                    CreatePassword = string.Empty;
                }
                else
                {
                    if (!model.Id.HasValue)
                    {
                        ErrorMessage = "Выберите пользователя для редактирования.";
                        return;
                    }

                    await _userAdministrationService.UpdateUserAsync(model, _currentUserId);
                    targetId = model.Id.Value;
                    ActionMessage = "Данные пользователя обновлены.";
                }

                ErrorMessage = null;
                await ReloadUsersSafeAsync(targetId);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Не удалось сохранить пользователя: {ex.Message}";
            }
        }

        [RelayCommand(CanExecute = nameof(CanDeleteSelectedUser))]
        private async Task DeleteUserAsync()
        {
            if (!CanManage || SelectedUser is null)
            {
                return;
            }

            try
            {
                // Фактические защитные ограничения удаления выполняет сервис.
                var deletedName = SelectedUser.DisplayName;
                await _userAdministrationService.DeleteUserAsync(SelectedUser.Id, _currentUserId);
                ActionMessage = $"Пользователь «{deletedName}» удален.";
                ErrorMessage = null;
                ResetEditForm();
                await ReloadUsersSafeAsync();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Не удалось удалить пользователя: {ex.Message}";
            }
        }

        [RelayCommand(CanExecute = nameof(CanChangePassword))]
        private async Task ChangePasswordAsync()
        {
            if (!CanManage || SelectedUser is null)
            {
                return;
            }

            // Смена пароля намеренно отделена от редактирования профиля.
            if (string.IsNullOrWhiteSpace(PasswordForReset))
            {
                ErrorMessage = "Введите новый пароль.";
                return;
            }

            try
            {
                await _userAdministrationService.ChangePasswordAsync(SelectedUser.Id, PasswordForReset);
                PasswordForReset = string.Empty;
                ErrorMessage = null;
                ActionMessage = $"Пароль пользователя «{SelectedUser.DisplayName}» обновлен.";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Не удалось обновить пароль: {ex.Message}";
            }
        }

        private async Task ReloadUsersSafeAsync(Guid? preferredSelectedUserId = null)
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
                await ReloadUsersAsync(requestVersion, preferredSelectedUserId);
            }
            catch (Exception ex)
            {
                if (requestVersion == _reloadRequestVersion)
                {
                    Users.Clear();
                    RecordsCounterText = "0";
                    ErrorMessage = $"Не удалось загрузить пользователей: {ex.Message}";
                    OnPropertyChanged(nameof(HasUsers));
                    OnPropertyChanged(nameof(HasNoUsers));
                }
            }
            finally
            {
                if (requestVersion == _reloadRequestVersion)
                {
                    IsLoading = false;
                    OnPropertyChanged(nameof(HasNoUsers));
                }
            }
        }

        private async Task ReloadUsersAsync(
            int requestVersion,
            Guid? preferredSelectedUserId = null,
            CancellationToken cancellationToken = default)
        {
            // Сбор параметров фильтра из UI-состояния.
            int? roleId = SelectedRoleFilter is { Id: > 0 } ? SelectedRoleFilter.Id : null;

            var items = await _userAdministrationService.GetUsersAsync(SearchText, roleId, cancellationToken);

            if (requestVersion != _reloadRequestVersion)
            {
                return;
            }

            var selectedUserId = preferredSelectedUserId ?? SelectedUser?.Id;

            Users.Clear();
            foreach (var item in items)
            {
                Users.Add(item);
            }

            RecordsCounterText = Users.Count.ToString();

            if (selectedUserId.HasValue)
            {
                SelectedUser = Users.FirstOrDefault(user => user.Id == selectedUserId.Value);
            }
            else
            {
                SelectedUser = null;
            }

            OnPropertyChanged(nameof(HasUsers));
            OnPropertyChanged(nameof(HasNoUsers));
        }

        private void FillEditForm(UserAdministrationListItem user)
        {
            var selectedRole = Roles.FirstOrDefault(role => role.Id == user.RoleId);

            EditForm = new UserEditFormModel
            {
                Id = user.Id,
                LastName = user.LastName,
                FirstName = user.FirstName,
                MiddleName = user.MiddleName,
                Email = user.Email,
                Phone = user.Phone,
                SelectedRole = selectedRole,
            };
        }

        private void ResetEditForm()
        {
            IsCreateMode = false;
            IsCreateDialogOpen = false;
            EditorTitle = "Данные пользователя";
            PasswordForReset = string.Empty;
            CreatePassword = string.Empty;

            EditForm = new UserEditFormModel
            {
                SelectedRole = Roles.FirstOrDefault(),
            };
        }

        private void TriggerAutoFiltering()
        {
            if (_suppressAutoFiltering || !CanManage)
            {
                return;
            }

            // Debounce фильтрации при наборе текста.
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
                await RefreshUsersAsync();
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

        private string? ValidateEditForm()
        {
            if (string.IsNullOrWhiteSpace(EditForm.LastName))
            {
                return "Фамилия обязательна.";
            }

            if (string.IsNullOrWhiteSpace(EditForm.FirstName))
            {
                return "Имя обязательно.";
            }

            if (string.IsNullOrWhiteSpace(EditForm.Email) || !EditForm.Email.Contains('@'))
            {
                return "Укажите корректный email.";
            }

            if (EditForm.SelectedRole is null || EditForm.SelectedRole.Id <= 0)
            {
                return "Выберите роль пользователя.";
            }

            return null;
        }

        private sealed class DesignUserAdministrationService : IUserAdministrationService
        {
            public Task<IReadOnlyList<UserAdministrationListItem>> GetUsersAsync(
                string? search,
                int? roleId,
                CancellationToken cancellationToken = default)
            {
                IReadOnlyList<UserAdministrationListItem> items = Array.Empty<UserAdministrationListItem>();
                return Task.FromResult(items);
            }

            public Task<IReadOnlyList<LookupItem>> GetRolesAsync(CancellationToken cancellationToken = default)
            {
                IReadOnlyList<LookupItem> roles = Array.Empty<LookupItem>();
                return Task.FromResult(roles);
            }

            public Task<Guid> CreateUserAsync(
                UserEditModel model,
                string password,
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult(Guid.NewGuid());
            }

            public Task UpdateUserAsync(
                UserEditModel model,
                Guid actingUserId,
                CancellationToken cancellationToken = default)
            {
                return Task.CompletedTask;
            }

            public Task ChangePasswordAsync(
                Guid userId,
                string newPassword,
                CancellationToken cancellationToken = default)
            {
                return Task.CompletedTask;
            }

            public Task DeleteUserAsync(
                Guid userId,
                Guid actingUserId,
                CancellationToken cancellationToken = default)
            {
                return Task.CompletedTask;
            }
        }
    }
}
