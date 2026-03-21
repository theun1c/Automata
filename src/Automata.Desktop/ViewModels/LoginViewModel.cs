using System;
using System.Threading.Tasks;
using Automata.Application.Auth.Models;
using Automata.Application.Auth.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Automata.Desktop.ViewModels
{
    /// <summary>
    /// ViewModel окна входа: валидация полей и запуск сценария авторизации.
    /// </summary>
    public partial class LoginViewModel : ViewModelBase
    {
        private readonly IAuthService _authService;
        private readonly Func<Task> _openRegistrationWindow;
        private readonly Action<AuthenticatedUser> _completeSignIn;

        [ObservableProperty]
        private string email = string.Empty;

        [ObservableProperty]
        private string password = string.Empty;

        [ObservableProperty]
        private bool showPassword;

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private string errorText = string.Empty;

        public bool HasError => !string.IsNullOrWhiteSpace(ErrorText);

        public LoginViewModel(
            IAuthService authService,
            Func<Task> openRegistrationWindow,
            Action<AuthenticatedUser> completeSignIn)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _openRegistrationWindow = openRegistrationWindow ?? throw new ArgumentNullException(nameof(openRegistrationWindow));
            _completeSignIn = completeSignIn ?? throw new ArgumentNullException(nameof(completeSignIn));
        }

        public bool IsPasswordMasked => !ShowPassword;

        partial void OnShowPasswordChanged(bool value)
        {
            OnPropertyChanged(nameof(IsPasswordMasked));
        }

        partial void OnErrorTextChanged(string value)
        {
            OnPropertyChanged(nameof(HasError));
        }

        [RelayCommand]
        private async Task SignInAsync()
        {
            if (IsLoading)
            {
                return;
            }

            // Минимальная pre-validation до обращения к сервису.
            ErrorText = string.Empty;

            if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
            {
                ErrorText = "Укажите email и пароль.";
                return;
            }

            IsLoading = true;
            try
            {
                var user = await _authService.SignInAsync(Email.Trim(), Password);
                if (user is null)
                {
                    ErrorText = "Неверный email или пароль.";
                    return;
                }

                _completeSignIn(user);
            }
            catch (Exception ex)
            {
                ErrorText = $"Ошибка входа: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task GoToRegistrationAsync()
        {
            try
            {
                // Регистрация открывается в отдельном окне, не смешиваем контексты.
                ErrorText = string.Empty;
                await _openRegistrationWindow();
            }
            catch (Exception ex)
            {
                ErrorText = $"Ошибка открытия регистрации: {ex.Message}";
            }
        }
    }
}
