using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Automata.Application.Auth.Models;
using Automata.Application.Auth.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Automata.Desktop.ViewModels
{
    /// <summary>
    /// ViewModel окна регистрации (email + пароль + captcha + мок-код подтверждения).
    /// </summary>
    public partial class RegistrationViewModel : ViewModelBase
    {
        private int _captchaExpectedResult;

        private readonly IAuthService _authService;
        private readonly Action _closeWindow;

        [ObservableProperty]
        private string email = string.Empty;

        [ObservableProperty]
        private string password = string.Empty;

        [ObservableProperty]
        private string confirmationCode = string.Empty;

        [ObservableProperty]
        private string captchaChallenge = string.Empty;

        [ObservableProperty]
        private string captchaAnswer = string.Empty;

        [ObservableProperty]
        private bool isCodeModalOpen;

        [ObservableProperty]
        private string generatedCode = string.Empty;

        [ObservableProperty]
        private string stateMessage = "Заполните поля регистрации.";

        [ObservableProperty]
        private string stateColor = "#334155";

        public RegistrationViewModel(IAuthService authService, Action closeWindow)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _closeWindow = closeWindow ?? throw new ArgumentNullException(nameof(closeWindow));
            GenerateCaptcha();
        }

        [RelayCommand]
        private async Task RegisterAsync()
        {
            try
            {
                // Перед регистрацией проверяем все обязательные поля и captcha.
                ValidateRegistration();

                var request = new RegisterUserRequest
                {
                    Email = Email.Trim(),
                    Password = Password,
                    FirstName = BuildFirstNameFromEmail(Email),
                    LastName = "Пользователь",
                };

                await _authService.RegisterAsync(request);

                StateMessage = "Регистрация успешна. Можно выполнить вход.";
                StateColor = "#166534";
                _closeWindow();
            }
            catch (Exception ex)
            {
                StateMessage = ex.Message;
                StateColor = "#B91C1C";
            }
        }

        [RelayCommand]
        private void OpenCodeModal()
        {
            // Мок-код генерируется один раз за сессию формы.
            if (string.IsNullOrWhiteSpace(GeneratedCode))
            {
                GeneratedCode = Random.Shared.Next(100000, 1000000).ToString(CultureInfo.InvariantCulture);
                StateMessage = "Код подтверждения сгенерирован.";
                StateColor = "#1D4ED8";
            }
            else
            {
                StateMessage = "Код подтверждения уже сгенерирован для этой сессии.";
                StateColor = "#334155";
            }

            IsCodeModalOpen = true;
        }

        [RelayCommand]
        private void CloseCodeModal()
        {
            IsCodeModalOpen = false;
        }

        [RelayCommand]
        private void RegenerateCaptcha()
        {
            GenerateCaptcha();
            CaptchaAnswer = string.Empty;
        }

        [RelayCommand]
        private void GoToLogin()
        {
            _closeWindow();
        }

        private void ValidateRegistration()
        {
            // Минимальные правила для текущего UX регистрации.
            if (string.IsNullOrWhiteSpace(Email) || !Email.Contains('@'))
            {
                throw new InvalidOperationException("Укажите корректный email.");
            }

            if (string.IsNullOrWhiteSpace(Password) || Password.Length < 8)
            {
                throw new InvalidOperationException("Пароль должен быть не короче 8 символов.");
            }

            if (!Password.Any(char.IsDigit) || !Password.Any(ch => !char.IsLetterOrDigit(ch)))
            {
                throw new InvalidOperationException("Пароль должен содержать цифру и специальный символ.");
            }

            if (!int.TryParse(CaptchaAnswer.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var captchaValue) ||
                captchaValue != _captchaExpectedResult)
            {
                throw new InvalidOperationException("Неверная CAPTCHA.");
            }

            if (string.IsNullOrWhiteSpace(GeneratedCode))
            {
                throw new InvalidOperationException("Сначала сгенерируйте код подтверждения.");
            }

            if (!string.Equals(ConfirmationCode.Trim(), GeneratedCode, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Неверный код подтверждения.");
            }
        }

        private void GenerateCaptcha()
        {
            // Генерируем выражение из 4 чисел и 3 операций.
            var a = Random.Shared.Next(2, 10);
            var b = Random.Shared.Next(2, 10);
            var c = Random.Shared.Next(2, 10);
            var d = Random.Shared.Next(2, 10);

            var operation1 = Random.Shared.Next(0, 2) == 0 ? '+' : '-';
            var operation2 = Random.Shared.Next(0, 2) == 0 ? '+' : '-';
            var operation3 = Random.Shared.Next(0, 2) == 0 ? '+' : '-';

            _captchaExpectedResult = Calculate(a, b, c, d, operation1, operation2, operation3);
            CaptchaChallenge = $"{a} {operation1} {b} {operation2} {c} {operation3} {d} = ?";
        }

        private static int Calculate(int a, int b, int c, int d, char op1, char op2, char op3)
        {
            var result = ApplyOperation(a, b, op1);
            result = ApplyOperation(result, c, op2);
            result = ApplyOperation(result, d, op3);
            return result;
        }

        private static int ApplyOperation(int left, int right, char operation)
        {
            return operation == '+' ? left + right : left - right;
        }

        private static string BuildFirstNameFromEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return "Новый";
            }

            var localPart = email.Split('@')[0].Trim();
            if (string.IsNullOrWhiteSpace(localPart))
            {
                return "Новый";
            }

            return char.ToUpperInvariant(localPart[0]) + localPart[1..];
        }
    }
}
