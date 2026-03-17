using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Automata.Desktop.ViewModels
{
    public enum RegistrationMockState
    {
        EmptyForm,
        InvalidEmail,
        InvalidPassword,
        InvalidCaptcha,
        CodeGenerated,
        InvalidConfirmationCode,
        InvalidFranchiseCode,
        Success,
    }

    public sealed class RegistrationStateOption
    {
        public RegistrationStateOption(RegistrationMockState state, string title)
        {
            State = state;
            Title = title;
        }

        public RegistrationMockState State { get; }
        public string Title { get; }
    }

    public partial class RegistrationViewModel : ViewModelBase
    {
        private readonly Action _openLogin;

        [ObservableProperty]
        private string email = string.Empty;

        [ObservableProperty]
        private string password = string.Empty;

        [ObservableProperty]
        private string confirmationCode = string.Empty;

        [ObservableProperty]
        private string franchiseCode = string.Empty;

        [ObservableProperty]
        private string captchaChallenge = "12 - 4 + 3 * 2 = ?";

        [ObservableProperty]
        private string captchaAnswer = string.Empty;

        [ObservableProperty]
        private bool isCodeModalOpen;

        [ObservableProperty]
        private string generatedCode = "834271";

        [ObservableProperty]
        private string stateMessage = "Пустая форма";

        [ObservableProperty]
        private string stateColor = "#334155";

        [ObservableProperty]
        private RegistrationStateOption selectedStateOption = null!;

        public RegistrationViewModel(Action openLogin)
        {
            _openLogin = openLogin;

            StateOptions = new ObservableCollection<RegistrationStateOption>
            {
                new(RegistrationMockState.EmptyForm, "Пустая форма"),
                new(RegistrationMockState.InvalidEmail, "Ошибка email"),
                new(RegistrationMockState.InvalidPassword, "Ошибка пароля"),
                new(RegistrationMockState.InvalidCaptcha, "Неверная CAPTCHA"),
                new(RegistrationMockState.CodeGenerated, "Код сгенерирован"),
                new(RegistrationMockState.InvalidConfirmationCode, "Неверный код подтверждения"),
                new(RegistrationMockState.InvalidFranchiseCode, "Неверный код франчайзи"),
                new(RegistrationMockState.Success, "Успешная регистрация"),
            };

            SelectedStateOption = StateOptions[0];
            ApplySelectedState(SelectedStateOption.State);
        }

        public ObservableCollection<RegistrationStateOption> StateOptions { get; }

        partial void OnSelectedStateOptionChanged(RegistrationStateOption value)
        {
            ApplySelectedState(value.State);
        }

        [RelayCommand]
        private void Register()
        {
            StateMessage = "Демо-режим: регистрация отображается только визуально.";
            StateColor = "#1D4ED8";
        }

        [RelayCommand]
        private void OpenCodeModal()
        {
            IsCodeModalOpen = true;
            SelectedStateOption = StateOptions[4];
        }

        [RelayCommand]
        private void CloseCodeModal()
        {
            IsCodeModalOpen = false;
        }

        [RelayCommand]
        private void GoToLogin()
        {
            _openLogin();
        }

        private void ApplySelectedState(RegistrationMockState state)
        {
            switch (state)
            {
                case RegistrationMockState.EmptyForm:
                    StateMessage = "Пустая форма: заполните поля регистрации.";
                    StateColor = "#334155";
                    break;
                case RegistrationMockState.InvalidEmail:
                    StateMessage = "Ошибка email: укажите корректный адрес.";
                    StateColor = "#B91C1C";
                    break;
                case RegistrationMockState.InvalidPassword:
                    StateMessage = "Ошибка пароля: минимум 8 символов, цифра и спецсимвол.";
                    StateColor = "#B91C1C";
                    break;
                case RegistrationMockState.InvalidCaptcha:
                    StateMessage = "Неверная CAPTCHA: проверьте арифметическое выражение.";
                    StateColor = "#B91C1C";
                    break;
                case RegistrationMockState.CodeGenerated:
                    StateMessage = "Код подтверждения сгенерирован (эмуляция).";
                    StateColor = "#1D4ED8";
                    break;
                case RegistrationMockState.InvalidConfirmationCode:
                    StateMessage = "Неверный код подтверждения.";
                    StateColor = "#B91C1C";
                    break;
                case RegistrationMockState.InvalidFranchiseCode:
                    StateMessage = "Неверный код франчайзи.";
                    StateColor = "#B91C1C";
                    break;
                case RegistrationMockState.Success:
                    StateMessage = "Успешная регистрация.";
                    StateColor = "#166534";
                    break;
                default:
                    StateMessage = "Демо-режим.";
                    StateColor = "#334155";
                    break;
            }
        }
    }
}
